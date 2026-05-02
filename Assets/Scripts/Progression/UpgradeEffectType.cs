// Phase 4 / PR 4.PA — typed key for the modifier/effect registry.
// Splits roughly into three groups (see UpgradeEffectKind):
//   * Multiplier  — combined as 1 + Σ(valueA × stacks), capped per §15.
//   * Flat        — combined as Σ(valueA × stacks).
//   * Triggered   — modifier query returns the same Flat sum, but the actual
//                   gameplay effect is fired through UpgradeSystem.Notify*.
// See docs/PHASE_4_ROGUELIKE_PROGRESSION_TZ.md §8.1, §9.4.1, §9.4.2.
public enum UpgradeEffectType
{
    // Weapon (Multiplier)
    WeaponDamageMultiplier,
    FireRateMultiplier,
    ReloadSpeedMultiplier,

    // Weapon (Flat)
    MagazineSizeFlat,
    PiercingFlat,

    // Mobility
    DashChargeFlat,
    DashCooldownMultiplier,

    // Sustain (Flat / Multiplier)
    MaxHpFlat,
    GloryKillHealFlat,
    HpOrbHealMultiplier,
    HpOrbMagnetRadius,

    // Triggered (queried as Flat, fired via Notify*)
    SpeedAfterKill,
    ShieldAfterKill,
    ChainLightningChance,
    SplashOnLastShot,
    StaggerThresholdBonus,
    SlowMoOnExecute,
    EliteHunterBonus
}

public enum UpgradeEffectKind
{
    Flat,
    Multiplier,
    Triggered
}

public static class UpgradeEffectTypeExtensions
{
    public static UpgradeEffectKind Kind(this UpgradeEffectType type)
    {
        switch (type)
        {
            case UpgradeEffectType.WeaponDamageMultiplier:
            case UpgradeEffectType.FireRateMultiplier:
            case UpgradeEffectType.ReloadSpeedMultiplier:
            case UpgradeEffectType.DashCooldownMultiplier:
            case UpgradeEffectType.HpOrbHealMultiplier:
                return UpgradeEffectKind.Multiplier;

            case UpgradeEffectType.SpeedAfterKill:
            case UpgradeEffectType.ShieldAfterKill:
            case UpgradeEffectType.ChainLightningChance:
            case UpgradeEffectType.SplashOnLastShot:
            case UpgradeEffectType.SlowMoOnExecute:
                return UpgradeEffectKind.Triggered;

            default:
                return UpgradeEffectKind.Flat;
        }
    }

    // Caps from §15. 0 means uncapped.
    // Multiplier caps are bonus fraction (e.g. 0.6 = +60%).
    // Flat caps are absolute additive (e.g. 60 = +60 HP).
    public static float BonusCap(this UpgradeEffectType type)
    {
        switch (type)
        {
            case UpgradeEffectType.WeaponDamageMultiplier: return 0.60f;
            case UpgradeEffectType.FireRateMultiplier: return 0.35f;
            case UpgradeEffectType.HpOrbHealMultiplier: return 1.00f; // base × 2
            case UpgradeEffectType.SpeedAfterKill: return 0.25f;
            case UpgradeEffectType.ChainLightningChance: return 0.25f;
            case UpgradeEffectType.MaxHpFlat: return 60f;
            case UpgradeEffectType.GloryKillHealFlat: return 20f;
            case UpgradeEffectType.DashChargeFlat: return 2f; // base 1 + 2 = 3 total per §15
            default: return 0f;
        }
    }
}
