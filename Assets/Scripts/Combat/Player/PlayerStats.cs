using System;
using UnityEngine;

/// <summary>
/// Central numeric-parameter provider for player-facing systems (heal amounts,
/// streak thresholds, speed multipliers).
///
/// Phase 4 / PR 4.PB: PlayerStats is the resolver between authored baseline
/// values and the active <see cref="UpgradeSystem"/>. Consumers call the Get*
/// methods and receive the final post-upgrade value lazily — see TZ §9.5.2
/// "PlayerStats reads from UpgradeSystem at query time".
///
/// The legacy public fields (<see cref="orbHealAmount"/>, <see cref="gloryHealAmount"/>)
/// remain accessible as the *baseline* — Inspector authoring still works.
/// External callers should prefer <see cref="GetOrbHealAmount"/> /
/// <see cref="GetGloryHealAmount"/> so upgrades take effect.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerStats : MonoBehaviour
{
    [Header("Healing (baseline)")]
    public float orbHealAmount = 5f;
    public float gloryHealAmount = 25f;

    [Header("HP Orb Magnet (baseline)")]
    [Tooltip("Default magnet pickup radius applied to spawned HP orbs. Upgrades add to this.")]
    public float orbMagnetBaseRadius = 0f;

    [Header("Glory Kill")]
    public float gloryBonusDamage = 999f;

    [Header("Kill Streak")]
    public int streakThreshold = 5;
    public float streakWindowSeconds = 10f;
    public float streakBoostMultiplier = 1.2f;
    public float streakBoostDuration = 5f;

    public event Action OnStatsChanged;

    Health health;
    float baselineMaxHp;
    float appliedMaxHpBonus;
    UpgradeSystem boundSystem;

    public float BaselineMaxHp => baselineMaxHp;

    void Awake()
    {
        health = GetComponent<Health>();
        baselineMaxHp = health != null ? health.maxHealth : 100f;
    }

    void Start()
    {
        // Subscribe to UpgradeSystem only after first frame so the singleton has
        // had a chance to be created by the run controller / debug probe.
        BindUpgradeSystem();
        ApplyMaxHpFromUpgrades();
    }

    void OnDestroy()
    {
        if (boundSystem != null)
        {
            boundSystem.OnUpgradesChanged -= HandleUpgradesChanged;
            boundSystem = null;
        }
    }

    void BindUpgradeSystem()
    {
        UpgradeSystem sys = UpgradeSystem.Instance;
        if (sys == boundSystem) return;
        if (boundSystem != null) boundSystem.OnUpgradesChanged -= HandleUpgradesChanged;
        boundSystem = sys;
        if (boundSystem != null) boundSystem.OnUpgradesChanged += HandleUpgradesChanged;
    }

    void HandleUpgradesChanged()
    {
        ApplyMaxHpFromUpgrades();
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Push <c>MaxHpFlat</c> total bonus into the Health component. The delta
    /// also heals the player up by the same amount so a fresh upgrade feels
    /// like an immediate reward (TZ §15.1).
    /// </summary>
    void ApplyMaxHpFromUpgrades()
    {
        if (health == null) return;
        UpgradeSystem sys = boundSystem ?? UpgradeSystem.Instance;
        float bonus = sys != null ? sys.GetAdditive(UpgradeEffectType.MaxHpFlat) : 0f;
        float delta = bonus - appliedMaxHpBonus;
        if (Mathf.Approximately(delta, 0f) && Mathf.Approximately(health.maxHealth, baselineMaxHp + bonus))
        {
            return;
        }
        health.maxHealth = baselineMaxHp + bonus;
        if (delta > 0f)
        {
            health.currentHealth = Mathf.Min(health.maxHealth, health.currentHealth + delta);
        }
        else if (delta < 0f)
        {
            // Bonus removed (run reset). Clamp current HP, do not restore.
            health.currentHealth = Mathf.Min(health.currentHealth, health.maxHealth);
        }
        appliedMaxHpBonus = bonus;
    }

    public float GetOrbHealAmount()
    {
        UpgradeSystem sys = boundSystem ?? UpgradeSystem.Instance;
        if (sys == null) return orbHealAmount;
        return orbHealAmount * sys.GetMultiplier(UpgradeEffectType.HpOrbHealMultiplier);
    }

    public float GetGloryHealAmount()
    {
        UpgradeSystem sys = boundSystem ?? UpgradeSystem.Instance;
        if (sys == null) return gloryHealAmount;
        return gloryHealAmount + sys.GetAdditive(UpgradeEffectType.GloryKillHealFlat);
    }

    public float GetOrbMagnetRadius()
    {
        UpgradeSystem sys = boundSystem ?? UpgradeSystem.Instance;
        if (sys == null) return orbMagnetBaseRadius;
        return orbMagnetBaseRadius + sys.GetAdditive(UpgradeEffectType.HpOrbMagnetRadius);
    }

    public void NotifyStatsChanged() => OnStatsChanged?.Invoke();
}
