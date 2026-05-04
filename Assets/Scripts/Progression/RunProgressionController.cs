using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;
using VoidSurvivor.ProceduralArena.Encounter;
using VoidSurvivor.Progression;

// Phase 4 / PR 4.PC — orchestrator that gates exit barriers behind a 3-card
// reward selection (TZ §6.4). Owns:
//   * subscription to EncounterController.Cleared
//   * RewardCardCanvas lifecycle
//   * player freeze (PlayerController.enabled = false during selection)
//   * cursor unlock for clicking cards
//   * deterministic seeding from runSeed + visitedArenaIndex + rewardCounter
//
// Auto-attached as a scene singleton (mirrors EnemyPool pattern). Wired by
// RunController.OnArenaBuilt which calls WatchEncounter for every new arena.
public class RunProgressionController : MonoBehaviour
{
    static RunProgressionController s_instance;

    [Tooltip("Resource path under Assets/Resources/. Default = 'Progression/Upgrades'.")]
    public string resourcePath = "Progression/Upgrades";

    [Tooltip("Run seed used for reward determinism. Set by the run controller; defaults to system time.")]
    public int runSeed;

    [Tooltip("Phase 4 / PR 4.PH — log per-arena reward seeding + picks for tuning.")]
    public bool debugLog;

    UpgradeData[] pool;
    int rewardCounter;
    bool pendingRareBoost;
    EncounterController watchedEncounter;
    int watchedArenaIndex;
    bool watchedIsElite;
    ArenaCategory watchedCategory = ArenaCategory.Combat;
    StyleBreakdown lastStyleSnapshot;
    bool hasStyleSnapshot;

    PlayerController playerController;
    WeaponManager weaponManager;
    RewardCardCanvas activeCanvas;

    CursorLockMode prevCursorLock;
    bool prevCursorVisible;
    bool cursorOverridden;

    public static RunProgressionController Instance
    {
        get
        {
            if (s_instance != null) return s_instance;
            var found = FindFirstObjectByType<RunProgressionController>();
            if (found != null) { s_instance = found; return s_instance; }
            var go = new GameObject("RunProgressionController");
            s_instance = go.AddComponent<RunProgressionController>();
            return s_instance;
        }
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this) { Destroy(this); return; }
        s_instance = this;
        if (runSeed == 0) runSeed = System.Environment.TickCount;
        pool = Resources.LoadAll<UpgradeData>(resourcePath);
        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning($"[RunProgressionController] No UpgradeData under Resources/{resourcePath} — reward cards disabled.");
        }
    }

    void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
        if (watchedEncounter != null) watchedEncounter.Cleared -= OnEncounterCleared;
        var st = StylePointsTracker.Instance;
        if (st != null) st.OnArenaFinalized -= OnStyleFinalized;
    }

    /// <summary>PR 4.PG — Rest Room "Prime Reward" sets a one-shot rarity boost
    /// applied to the next reward card draw, then auto-consumed.</summary>
    public void QueueRareRewardBoost() => pendingRareBoost = true;
    public bool HasPendingRareBoost => pendingRareBoost;

    /// <summary>Wipe per-run state. RunController.StartRun calls this.</summary>
    public void ResetForNewRun()
    {
        pendingRareBoost = false;
        rewardCounter = 0;
    }

    /// <summary>Called by RunController.OnArenaBuilt to wire the new encounter.</summary>
    public void WatchEncounter(EncounterController enc, int visitedArenaIndex, bool isElite,
        ArenaCategory category = ArenaCategory.Combat)
    {
        if (watchedEncounter != null)
        {
            watchedEncounter.Cleared -= OnEncounterCleared;
        }
        // Detach style tracker subscription from any prior arena.
        var st = StylePointsTracker.Instance;
        if (st != null) st.OnArenaFinalized -= OnStyleFinalized;

        watchedEncounter = enc;
        watchedArenaIndex = visitedArenaIndex;
        watchedIsElite = isElite;
        watchedCategory = category;
        hasStyleSnapshot = false;
        lastStyleSnapshot = default;

        if (enc == null) return;

        // PR 4.PE — start style tracking for any encounter that has a clear
        // condition (Start/Shop/Rest auto-clear with None and don't pay out).
        bool payoutEligible = (category == ArenaCategory.Combat || category == ArenaCategory.Elite)
                              && enc.clearCondition != ClearCondition.None;
        if (payoutEligible && st != null)
        {
            st.WatchEncounter(enc);
            st.OnArenaFinalized += OnStyleFinalized;
        }

        if (!ShouldShowRewardForArena(enc, visitedArenaIndex) && !payoutEligible) return;
        // Hold barriers up-front so FinishCleared() doesn't open them before
        // we wire the UI — Cleared fires synchronously inside FinishCleared.
        enc.HoldBarriers = true;
        enc.Cleared += OnEncounterCleared;
    }

    void OnStyleFinalized(StyleBreakdown breakdown)
    {
        lastStyleSnapshot = breakdown;
        hasStyleSnapshot = true;
    }

    bool ShouldShowRewardForArena(EncounterController enc, int visitedArenaIndex)
    {
        // No reward for Start/Shop/Rest (clearCondition=None). Boss reward is
        // out of scope per TZ §5.6. PR 4.PD will refine using node category.
        if (enc.clearCondition == VoidSurvivor.ProceduralArena.Arena.ClearCondition.None) return false;
        if (pool == null || pool.Length == 0) return false;
        return true;
    }

    void OnEncounterCleared()
    {
        if (watchedEncounter == null) return;
        if (activeCanvas != null) return; // already showing

        // PR 4.PE — KP payout flows BEFORE the reward card UI. For Combat and
        // Elite arenas: compute payout, award KP, show payout panel, then chain
        // to ShowReward via the panel's onComplete.
        bool payoutEligible = watchedCategory == ArenaCategory.Combat
                              || watchedCategory == ArenaCategory.Elite;
        if (payoutEligible)
        {
            // StylePointsTracker.OnArenaFinalized is fired synchronously from
            // EncounterController.Cleared; if we got here without a snapshot,
            // it's a defensive default (no kills, nothing tracked).
            var snap = hasStyleSnapshot ? lastStyleSnapshot : default;
            var payout = ArenaPayoutCalculator.Compute(watchedCategory, watchedArenaIndex, snap);
            if (debugLog) Debug.Log($"[RPC] payout arena={watchedArenaIndex} cat={watchedCategory} clear={payout.clearReward} style={payout.cappedStyle}/{payout.styleCap} (raw {payout.rawStyle}) total={payout.totalKp} (fast={snap.fastClear} noHit={snap.noHit})");
            KillPointsWallet.Instance?.Add(payout.totalKp);
            // Block the player so they don't run into the closed barrier or
            // start firing through the panel.
            ResolvePlayerRefs();
            FreezePlayer();
            UnlockCursor();
            PayoutPanel.Show(payout, OnPayoutDismissed);
            return;
        }

        ShowReward();
    }

    void OnPayoutDismissed()
    {
        // Continue to the reward card phase if this arena has a card pool;
        // otherwise release the gate and restore input. Reward UI will re-call
        // FreezePlayer/UnlockCursor itself, but they're idempotent.
        if (watchedEncounter != null && pool != null && pool.Length > 0
            && ShouldShowRewardForArena(watchedEncounter, watchedArenaIndex))
        {
            ShowReward();
            return;
        }
        UnfreezePlayer();
        RestoreCursor();
        ReleaseGate();
    }

    void ShowReward()
    {
        ResolvePlayerRefs();

        // Deterministic seed: §16.
        int seed = runSeed ^ watchedArenaIndex ^ rewardCounter ^ unchecked((int)0x44AA7711);
        rewardCounter++;
        var rng = new System.Random(seed);

        bool consumeRareBoost = pendingRareBoost;
        UpgradeData[] picks = RewardCardGenerator.Generate(
            rng, pool, UpgradeSystem.Instance, Mathf.Max(1, watchedArenaIndex), watchedIsElite, 3,
            consumeRareBoost);
        if (consumeRareBoost) pendingRareBoost = false;

        if (debugLog)
        {
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < picks.Length; i++)
            {
                if (i > 0) names.Append(", ");
                names.Append(picks[i] != null ? picks[i].id : "null");
                if (picks[i] != null) names.Append('(').Append(picks[i].rarity).Append(')');
            }
            Debug.Log($"[RPC] reward arena={watchedArenaIndex} elite={watchedIsElite} rareBoost={consumeRareBoost} seed={seed:X8} picks=[{names}]");
        }

        if (picks == null || picks.Length == 0)
        {
            Debug.LogWarning("[RunProgressionController] Generator returned 0 cards — unlocking exits with no reward.");
            UnfreezePlayer();
            RestoreCursor();
            ReleaseGate();
            return;
        }

        FreezePlayer();
        UnlockCursor();
        activeCanvas = RewardCardCanvas.Show(picks, UpgradeSystem.Instance, playerController != null ? playerController.gameObject : null,
            (idx) => OnCardSelected(picks, idx));
    }

    void OnCardSelected(UpgradeData[] options, int index)
    {
        if (index >= 0 && index < options.Length && options[index] != null)
        {
            UpgradeSystem.Instance?.AddUpgrade(options[index]);
        }
        if (activeCanvas != null) { activeCanvas.Hide(); activeCanvas = null; }
        UnfreezePlayer();
        RestoreCursor();
        ReleaseGate();
    }

    void ReleaseGate()
    {
        if (watchedEncounter == null) return;
        watchedEncounter.HoldBarriers = false;
        watchedEncounter.OpenBarriers();
    }

    // ------------------------------------------------------------------
    // Player input lock (TZ §10.2 reward UI must block movement / shooting)
    // ------------------------------------------------------------------
    void ResolvePlayerRefs()
    {
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (weaponManager == null && playerController != null)
        {
            weaponManager = playerController.weaponManager != null
                ? playerController.weaponManager
                : playerController.GetComponentInChildren<WeaponManager>();
        }
    }

    void FreezePlayer()
    {
        if (playerController == null) return;
        // PR 4.PC bugfix — use SetFrozen flag instead of disabling the script.
        // Disabling stopped controller.Move() so the CharacterController fell
        // through the floor, AND SendMessage-based OnFire callbacks kept firing
        // weapons through the reward UI clicks. SetFrozen handles both cleanly.
        playerController.SetFrozen(true);
        if (weaponManager != null) weaponManager.SetFireHeld(false);
    }

    void UnfreezePlayer()
    {
        if (playerController == null) return;
        playerController.SetFrozen(false);
        // Belt-and-braces: ensure weapon trigger is released even if the
        // mouse-up event was eaten by the canvas during freeze.
        if (weaponManager != null) weaponManager.SetFireHeld(false);
    }

    void UnlockCursor()
    {
        if (!cursorOverridden)
        {
            prevCursorLock = Cursor.lockState;
            prevCursorVisible = Cursor.visible;
            cursorOverridden = true;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void RestoreCursor()
    {
        if (!cursorOverridden) return;
        Cursor.lockState = prevCursorLock;
        Cursor.visible = prevCursorVisible;
        cursorOverridden = false;
    }
}
