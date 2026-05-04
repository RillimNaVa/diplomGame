using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;
using VoidSurvivor.ProceduralArena.Encounter;

// Phase 4 / PR 4.PG — Rest Room runtime orchestrator.
// Mirrors ShopController. Holds soft-lock barriers until the player has
// picked exactly one of three options; selecting unlocks the exits.
public class RestRoomController : MonoBehaviour
{
    static RestRoomController s_instance;

    public float healFraction = 0.35f;
    public int maxHpAmount = 10;
    public int rareBoostCost = 15;

    [Tooltip("Phase 4 / PR 4.PH — log Rest selection for tuning.")]
    public bool debugLog;

    RestOption[] options;
    int arenaIndex;
    EncounterController watchedEncounter;
    bool resolved;

    RestRoomCanvas activeCanvas;
    PlayerController playerController;
    WeaponManager weaponManager;
    Health playerHealth;

    CursorLockMode prevCursorLock;
    bool prevCursorVisible;
    bool cursorOverridden;

    public static RestRoomController Instance
    {
        get
        {
            if (s_instance != null) return s_instance;
            var found = FindFirstObjectByType<RestRoomController>();
            if (found != null) { s_instance = found; return s_instance; }
            var go = new GameObject("RestRoomController");
            s_instance = go.AddComponent<RestRoomController>();
            return s_instance;
        }
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this) { Destroy(this); return; }
        s_instance = this;
    }

    void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
        CloseUI();
    }

    public void PrepareForArena(ArenaCategory category, int visitedArenaIndex, EncounterController encounter)
    {
        if (category != ArenaCategory.Rest) return;

        CloseUI();
        arenaIndex = visitedArenaIndex;
        resolved = false;
        watchedEncounter = encounter;
        options = new[]
        {
            RestOption.Heal(healFraction),
            RestOption.MaxHp(maxHpAmount),
            RestOption.Rare(rareBoostCost),
        };

        // Hold the soft-lock barriers until a choice is made. EncounterController.Start
        // honors HoldBarriers if it's already true at startup (PR 4.PG patch).
        if (encounter != null)
        {
            encounter.HoldBarriers = true;
            for (int i = 0; i < encounter.barriers.Count; i++)
                if (encounter.barriers[i] != null) encounter.barriers[i].Close();
        }

        ResolvePlayerRefs();
    }

    public void OpenPreparedRest()
    {
        if (resolved) return;
        if (options == null || options.Length == 0) return;
        if (activeCanvas != null) return;
        ShowCanvas();
    }

    void ShowCanvas()
    {
        FreezePlayer();
        UnlockCursor();
        if (activeCanvas != null) activeCanvas.Hide();
        activeCanvas = RestRoomCanvas.Show(options, OnSelectRequested, RequestClose);
    }

    void OnSelectRequested(int index)
    {
        if (resolved) return;
        if (options == null || index < 0 || index >= options.Length) return;
        var option = options[index];
        if (option == null || option.selected) return;

        if (!ApplyOption(option)) return;

        option.selected = true;
        resolved = true;
        if (debugLog) Debug.Log($"[Rest] arena={arenaIndex} pick={option.kind} ({option.title})");
        ReleaseGate();
        CloseUI();
    }

    bool ApplyOption(RestOption option)
    {
        switch (option.kind)
        {
            case RestOptionKind.HealPercent:
                if (playerHealth == null) ResolvePlayerRefs();
                if (playerHealth == null) return false;
                playerHealth.Heal(playerHealth.maxHealth * option.healFraction);
                return true;

            case RestOptionKind.MaxHpFlat:
                if (playerHealth == null) ResolvePlayerRefs();
                if (playerHealth == null) return false;
                playerHealth.maxHealth += option.maxHpFlat;
                playerHealth.Heal(option.maxHpFlat);
                return true;

            case RestOptionKind.RareBoost:
                var wallet = KillPointsWallet.Instance;
                if (wallet == null || !wallet.TrySpend(option.kpCost)) return false;
                RunProgressionController.Instance?.QueueRareRewardBoost();
                return true;
        }
        return false;
    }

    void RequestClose()
    {
        // Esc / Close button — closes UI but keeps gate locked. Player must
        // step on the platform again to re-open and pick.
        CloseUI();
    }

    void CloseUI()
    {
        if (activeCanvas != null)
        {
            activeCanvas.Hide();
            activeCanvas = null;
        }
        UnfreezePlayer();
        RestoreCursor();
    }

    void ReleaseGate()
    {
        if (watchedEncounter == null) return;
        watchedEncounter.HoldBarriers = false;
        watchedEncounter.OpenBarriers();
    }

    void ResolvePlayerRefs()
    {
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerHealth == null)
        {
            if (GameManager.instance != null) playerHealth = GameManager.instance.playerHealth;
            if (playerHealth == null && playerController != null) playerHealth = playerController.GetComponent<Health>();
        }
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
        playerController.SetFrozen(true);
        if (weaponManager != null) weaponManager.SetFireHeld(false);
    }

    void UnfreezePlayer()
    {
        if (playerController == null) return;
        playerController.SetFrozen(false);
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
