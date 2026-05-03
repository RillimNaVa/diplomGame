using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;

// Phase 4 / PR 4.PF — Shop Room runtime orchestrator.
// Opens a deterministic shop UI when a Shop arena is built. Purchases spend
// run-scoped Kill Points and apply heals/upgrades immediately.
public class ShopController : MonoBehaviour
{
    static ShopController s_instance;

    [Tooltip("Resource path under Assets/Resources/. Default = 'Progression/Upgrades'.")]
    public string resourcePath = "Progression/Upgrades";

    UpgradeData[] pool;
    ShopOffer[] offers;
    int runSeed;
    int arenaIndex;
    int shopSeed;
    int rerollCount;

    ShopCanvas activeCanvas;
    PlayerController playerController;
    WeaponManager weaponManager;
    Health playerHealth;

    CursorLockMode prevCursorLock;
    bool prevCursorVisible;
    bool cursorOverridden;

    public static ShopController Instance
    {
        get
        {
            if (s_instance != null) return s_instance;
            var found = FindFirstObjectByType<ShopController>();
            if (found != null) { s_instance = found; return s_instance; }
            var go = new GameObject("ShopController");
            s_instance = go.AddComponent<ShopController>();
            return s_instance;
        }
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this) { Destroy(this); return; }
        s_instance = this;
        pool = Resources.LoadAll<UpgradeData>(resourcePath);
    }

    void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
        CloseShop();
    }

    public void PrepareForArena(ArenaCategory category, int visitedArenaIndex, int currentRunSeed)
    {
        if (category != ArenaCategory.Shop) return;

        CloseShop();
        runSeed = currentRunSeed;
        arenaIndex = visitedArenaIndex;
        rerollCount = 0;
        shopSeed = runSeed ^ arenaIndex ^ unchecked((int)0x90BB1234);

        ResolvePlayerRefs();
        RegenerateOffers();
    }

    public void OpenPreparedShop()
    {
        if (offers == null || offers.Length == 0)
        {
            RegenerateOffers();
        }
        if (activeCanvas != null) return;
        ShowCanvas();
    }

    void RegenerateOffers()
    {
        int rerollSeed = shopSeed ^ rerollCount ^ unchecked((int)0x77EEAA33);
        offers = ShopInventoryGenerator.Generate(
            new System.Random(rerollSeed),
            pool,
            UpgradeSystem.Instance,
            Mathf.Max(1, arenaIndex));
    }

    void ShowCanvas()
    {
        FreezePlayer();
        UnlockCursor();
        if (activeCanvas != null) activeCanvas.Hide();
        activeCanvas = ShopCanvas.Show(offers, rerollCount, OnPurchaseRequested, OnRerollRequested, CloseShop);
    }

    void OnPurchaseRequested(int offerIndex)
    {
        if (offers == null || offerIndex < 0 || offerIndex >= offers.Length) return;
        var offer = offers[offerIndex];
        if (offer == null || offer.purchased) return;

        var wallet = KillPointsWallet.Instance;
        if (wallet == null || !wallet.TrySpend(offer.price)) return;

        if (offer.kind == ShopOfferKind.Upgrade && offer.upgrade != null)
        {
            UpgradeSystem.Instance?.AddUpgrade(offer.upgrade);
        }
        else if (offer.kind == ShopOfferKind.Heal && playerHealth != null)
        {
            float amount = playerHealth.maxHealth * offer.healFraction;
            playerHealth.Heal(amount);
        }

        offer.purchased = true;
        activeCanvas?.Refresh(offers, rerollCount);
    }

    void OnRerollRequested()
    {
        int price = ShopInventoryGenerator.RerollPrice(rerollCount);
        var wallet = KillPointsWallet.Instance;
        if (wallet == null || !wallet.TrySpend(price)) return;

        rerollCount++;
        RegenerateOffers();
        activeCanvas?.Refresh(offers, rerollCount);
    }

    public void CloseShop()
    {
        if (activeCanvas != null)
        {
            activeCanvas.Hide();
            activeCanvas = null;
        }
        UnfreezePlayer();
        RestoreCursor();
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
