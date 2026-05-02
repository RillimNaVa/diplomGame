using UnityEngine;

/// <summary>
/// Observes the Void Blade weapon: on every swing, does its own OverlapSphere
/// (same math as MeleeArcFireMode) to find the first staggered target, asks
/// the active IGloryKillPolicy for permission, and — if allowed — instakills
/// + heals the player for PlayerStats.gloryHealAmount.
///
/// Deliberately side-channel: WeaponBase / MeleeArcFireMode are never mutated.
/// Bound to Void Blade via weaponId == "void_blade" (WeaponCategory.Melee alone
/// would also match future melee weapons, which shouldn't glory-kill).
///
/// **2026-05-01 — Disabled by default.** Glory kills are now player-driven via
/// `GloryKillExecutor` (F-press near a staggered enemy). Set `legacyAutoKillEnabled`
/// to true to re-enable the old swing-triggered behavior.
/// </summary>
public class GloryKillDetector : MonoBehaviour
{
    [Header("Legacy")]
    [Tooltip("If false, the old auto-glory-kill on swing is disabled. F-press via GloryKillExecutor is the canonical path.")]
    public bool legacyAutoKillEnabled = false;

    private const string VoidBladeId = "void_blade";

    [Header("References")]
    [Tooltip("Auto-resolved from this GameObject if empty.")]
    public WeaponManager weaponManager;
    [Tooltip("Auto-resolved from this GameObject if empty.")]
    public Health playerHealth;
    [Tooltip("Auto-resolved from this GameObject if empty.")]
    public PlayerStats playerStats;
    [Tooltip("Auto-resolved from this GameObject if empty. Optional — defaults to AlwaysAllowPolicy.")]
    public KillStreakTracker streakTracker;
    [Tooltip("Physics mask for glory-kill target search. Same as weapon hitMask.")]
    public LayerMask hitMask = ~0;
    [Tooltip("Camera used to aim the glory-kill search. Auto-resolved from Camera.main if empty.")]
    public Transform cameraTransform;

    [Header("FX (optional)")]
    public AudioClip gloryKillSfx;
    public GameObject gloryKillVfx;

    private IGloryKillPolicy policy;
    private WeaponBase subscribedWeapon;

    void Awake()
    {
        if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
        if (playerHealth == null) playerHealth = GetComponent<Health>();
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (streakTracker == null) streakTracker = GetComponent<KillStreakTracker>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        policy = GetComponent<IGloryKillPolicy>();
    }

    void OnEnable()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponEquipped += OnWeaponEquipped;
            if (weaponManager.CurrentWeapon != null) OnWeaponEquipped(weaponManager.CurrentWeapon);
        }
    }

    void OnDisable()
    {
        if (weaponManager != null) weaponManager.OnWeaponEquipped -= OnWeaponEquipped;
        UnsubscribeFromWeapon();
    }

    void OnWeaponEquipped(WeaponBase weapon)
    {
        UnsubscribeFromWeapon();

        if (weapon == null || weapon.Definition == null) return;
        if (weapon.Definition.weaponCategory != WeaponCategory.Melee) return;
        if (weapon.Definition.weaponId != VoidBladeId) return;

        subscribedWeapon = weapon;
        subscribedWeapon.OnFired += OnVoidBladeFired;
    }

    void UnsubscribeFromWeapon()
    {
        if (subscribedWeapon != null)
        {
            subscribedWeapon.OnFired -= OnVoidBladeFired;
            subscribedWeapon = null;
        }
    }

    void OnVoidBladeFired(WeaponBase weapon)
    {
        if (!legacyAutoKillEnabled) return;
        if (weapon == null || weapon.Definition == null) return;
        if (cameraTransform == null) return;
        if (playerHealth == null || playerStats == null) return;

        float range = weapon.Definition.range;
        float radius = Mathf.Max(0.1f, range * 0.5f);
        Vector3 center = cameraTransform.position + cameraTransform.forward * radius;

        Collider[] hits = Physics.OverlapSphere(center, radius, hitMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col.transform.IsChildOf(transform)) continue;

            EnemyStagger stagger = col.GetComponentInParent<EnemyStagger>();
            if (stagger == null || !stagger.IsStaggered) continue;

            Health target = stagger.GetComponent<Health>();
            if (target == null || target.currentHealth <= 0f) continue;

            GloryKillContext ctx = new GloryKillContext
            {
                target = target,
                weapon = weapon,
                streakTracker = streakTracker,
            };

            IGloryKillPolicy active = policy ?? GetComponent<IGloryKillPolicy>();
            if (active != null && !active.CanGloryKill(ctx)) return;

            PerformGloryKill(ctx, active);
            return; // one glory kill per swing
        }
    }

    void PerformGloryKill(GloryKillContext ctx, IGloryKillPolicy active)
    {
        if (ctx.target == null) return;

        ctx.target.TakeDamage(playerStats.gloryBonusDamage);
        playerHealth.Heal(playerStats.gloryHealAmount);

        if (gloryKillSfx != null) AudioSource.PlayClipAtPoint(gloryKillSfx, ctx.target.transform.position);
        if (gloryKillVfx != null) Instantiate(gloryKillVfx, ctx.target.transform.position, Quaternion.identity);

        active?.NotifyGloryKillPerformed(ctx);
    }
}
