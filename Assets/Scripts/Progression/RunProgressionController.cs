using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Encounter;

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

    UpgradeData[] pool;
    int rewardCounter;
    EncounterController watchedEncounter;
    int watchedArenaIndex;
    bool watchedIsElite;

    PlayerController playerController;
    WeaponManager weaponManager;
    RewardCardCanvas activeCanvas;

    CursorLockMode prevCursorLock;
    bool prevCursorVisible;

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
    }

    /// <summary>Called by RunController.OnArenaBuilt to wire the new encounter.</summary>
    public void WatchEncounter(EncounterController enc, int visitedArenaIndex, bool isElite)
    {
        if (watchedEncounter != null)
        {
            watchedEncounter.Cleared -= OnEncounterCleared;
        }
        watchedEncounter = enc;
        watchedArenaIndex = visitedArenaIndex;
        watchedIsElite = isElite;
        if (enc == null) return;
        if (!ShouldShowRewardForArena(enc, visitedArenaIndex)) return;
        // Hold barriers up-front so FinishCleared() doesn't open them before
        // we wire the UI — Cleared fires synchronously inside FinishCleared.
        enc.HoldBarriers = true;
        enc.Cleared += OnEncounterCleared;
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
        ShowReward();
    }

    void ShowReward()
    {
        ResolvePlayerRefs();

        // Deterministic seed: §16.
        int seed = runSeed ^ watchedArenaIndex ^ rewardCounter ^ unchecked((int)0x44AA7711);
        rewardCounter++;
        var rng = new System.Random(seed);

        UpgradeData[] picks = RewardCardGenerator.Generate(
            rng, pool, UpgradeSystem.Instance, Mathf.Max(1, watchedArenaIndex), watchedIsElite, 3);

        if (picks == null || picks.Length == 0)
        {
            Debug.LogWarning("[RunProgressionController] Generator returned 0 cards — unlocking exits with no reward.");
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
        prevCursorLock = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void RestoreCursor()
    {
        Cursor.lockState = prevCursorLock;
        Cursor.visible = prevCursorVisible;
    }
}
