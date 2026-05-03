using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Phase 5+ / UI HUD Polish — sci-fi combat HUD that owns the active in-game UI.
// Auto-builds its own Canvas (ScreenSpaceOverlay) and procedurally generated
// sprites so no Editor wiring or art assets are required. An optional
// hudPrefab can be assigned later for authored visuals.
//
// Bootstrapped automatically by GameManager.ResolveReferences when one is not
// already present on the player.
public class CombatHUDController : MonoBehaviour
{
    [Header("Optional override")]
    [Tooltip("If assigned, this prefab is instantiated under the auto-built canvas instead of using procedural visuals. Leave null for runtime defaults.")]
    [SerializeField] private GameObject hudPrefab;

    [Header("References (auto-resolved)")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private PlayerController playerController;

    // Built at runtime
    Canvas canvas;
    RectTransform rootRect;

    // Blocks
    HpBlock hpBlock;
    AmmoBlock ammoBlock;
    DashChargesBlock dashBlock;
    CrosshairBlock crosshairBlock;
    EnemyCounterBlock enemyCounterBlock;
    HealFloatBlock healFloat;
    GloryKillPromptBlock gloryPrompt;
    KillPointsBlock kpBlock;
    StyleMeterBlock styleBlock;

    // Cached state
    float lastDisplayedHp;

    void Awake()
    {
        ResolveRefs();
        BuildCanvas();
        BuildBlocks();
    }

    void Start()
    {
        // Suppress the legacy slider/text HUD so they don't fight the new one.
        var legacy = FindAnyObjectByType<UIManager>();
        if (legacy != null) legacy.SuppressLegacyHud();

        if (playerHealth != null)
        {
            lastDisplayedHp = playerHealth.currentHealth;
            hpBlock?.SetHp(playerHealth.currentHealth, playerHealth.maxHealth, instant: true);
        }
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged.AddListener(OnHealthChanged);
            playerHealth.onTakeDamage.AddListener(OnTakeDamage);
            if (playerHealth.onHeal != null) playerHealth.onHeal.AddListener(OnHeal);
            playerHealth.onDeath.AddListener(OnPlayerDied);
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged.RemoveListener(OnHealthChanged);
            playerHealth.onTakeDamage.RemoveListener(OnTakeDamage);
            if (playerHealth.onHeal != null) playerHealth.onHeal.RemoveListener(OnHeal);
            playerHealth.onDeath.RemoveListener(OnPlayerDied);
        }
    }

    void Update()
    {
        // Use unscaled time so HUD pulses don't freeze during glory-kill slow-mo.
        float dt = Time.unscaledDeltaTime;
        hpBlock?.Tick(dt);
        ammoBlock?.Tick();
        dashBlock?.Tick();
        crosshairBlock?.Tick(dt);
        enemyCounterBlock?.Tick(dt);
        healFloat?.Tick(dt);
        gloryPrompt?.Tick(dt);
        kpBlock?.Tick(dt);
        styleBlock?.Tick(dt);
    }

    // --- Resolution / building ---

    void ResolveRefs()
    {
        if (playerHealth == null) playerHealth = GetComponent<Health>();
        if (playerHealth == null) playerHealth = GetComponentInParent<Health>();
        if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
        if (weaponManager == null) weaponManager = GetComponentInParent<WeaponManager>();
        if (weaponManager == null) weaponManager = GetComponentInChildren<WeaponManager>(true);
        // Last resort: scene scan. Cheap because we only do it during HUD bring-up.
        if (weaponManager == null) weaponManager = FindAnyObjectByType<WeaponManager>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerController == null) playerController = GetComponentInParent<PlayerController>();
        if (playerController == null) playerController = GetComponentInChildren<PlayerController>(true);
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
    }

    void BuildCanvas()
    {
        var canvasGo = new GameObject("CombatHUDCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4000; // below DamageDirectionHUD (4500)

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        rootRect = canvas.GetComponent<RectTransform>();

        if (hudPrefab != null)
        {
            // Authored override: instantiate and skip procedural blocks.
            Instantiate(hudPrefab, canvasGo.transform);
        }
    }

    void BuildBlocks()
    {
        if (hudPrefab != null) return; // authored prefab handles its own blocks
        hpBlock = HpBlock.Build(rootRect);
        ammoBlock = AmmoBlock.Build(rootRect, weaponManager);
        dashBlock = DashChargesBlock.Build(rootRect, playerController);
        crosshairBlock = CrosshairBlock.Build(rootRect, playerHealth);
        enemyCounterBlock = EnemyCounterBlock.Build(rootRect);
        healFloat = HealFloatBlock.Build(rootRect);

        var executor = playerHealth != null
            ? playerHealth.GetComponent<GloryKillExecutor>()
            : UnityEngine.Object.FindAnyObjectByType<GloryKillExecutor>();
        gloryPrompt = GloryKillPromptBlock.Build(rootRect, executor);
        kpBlock = KillPointsBlock.Build(rootRect);
        styleBlock = StyleMeterBlock.Build(rootRect);
    }

    void OnDestroy()
    {
        ammoBlock?.Unbind();
        crosshairBlock?.Unsubscribe();
        enemyCounterBlock?.Unsubscribe();
        kpBlock?.Unsubscribe();
        styleBlock?.Unsubscribe();
    }

    // --- Health event handlers ---

    void OnHealthChanged(float current, float max)
    {
        hpBlock?.SetHp(current, max, instant: false);
        lastDisplayedHp = current;
    }

    void OnTakeDamage(float damage)
    {
        hpBlock?.PulseDamage();
    }

    void OnHeal(float amount)
    {
        hpBlock?.PulseHeal(amount);
        healFloat?.Show(amount);
    }

    void OnPlayerDied()
    {
        if (canvas != null) canvas.enabled = false;
    }
}
