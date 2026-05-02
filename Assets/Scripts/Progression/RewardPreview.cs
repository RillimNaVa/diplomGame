using UnityEngine;

// Phase 4 / PR 4.PC — derives a short "before → after" string for a reward
// card given the current run state. Best-effort: only effect types that map
// cleanly to a player-visible number get a preview. Others return "".
public static class RewardPreview
{
    public static string Build(UpgradeData data, UpgradeSystem sys, GameObject playerRoot)
    {
        if (data == null) return string.Empty;
        Health hp = playerRoot != null ? playerRoot.GetComponentInParent<Health>() : null;
        PlayerStats stats = playerRoot != null ? playerRoot.GetComponentInParent<PlayerStats>() : null;
        PlayerController pc = playerRoot != null ? playerRoot.GetComponentInParent<PlayerController>() : null;

        switch (data.effectType)
        {
            case UpgradeEffectType.MaxHpFlat:
            {
                if (hp == null || stats == null) break;
                float baseHp = stats.BaselineMaxHp;
                float current = hp.maxHealth;
                float next = current + data.valueA;
                return $"HP: {current:F0} → {next:F0}";
            }

            case UpgradeEffectType.WeaponDamageMultiplier:
            {
                float mul = sys != null ? sys.GetMultiplier(UpgradeEffectType.WeaponDamageMultiplier) : 1f;
                float pct = (mul - 1f) * 100f;
                float nextPct = pct + data.valueA * 100f;
                return $"Damage: +{pct:F0}% → +{nextPct:F0}%";
            }

            case UpgradeEffectType.FireRateMultiplier:
            {
                float mul = sys != null ? sys.GetMultiplier(UpgradeEffectType.FireRateMultiplier) : 1f;
                float pct = (mul - 1f) * 100f;
                float nextPct = pct + data.valueA * 100f;
                return $"Fire rate: +{pct:F0}% → +{nextPct:F0}%";
            }

            case UpgradeEffectType.GloryKillHealFlat:
            {
                if (stats == null) break;
                float current = stats.GetGloryHealAmount();
                float next = current + data.valueA;
                return $"Glory heal: {current:F0} → {next:F0}";
            }

            case UpgradeEffectType.HpOrbHealMultiplier:
            {
                if (stats == null) break;
                float current = stats.GetOrbHealAmount();
                float baseOrb = stats.orbHealAmount;
                float currentMul = sys != null ? sys.GetMultiplier(UpgradeEffectType.HpOrbHealMultiplier) : 1f;
                float nextMul = currentMul + data.valueA;
                float next = baseOrb * nextMul;
                return $"Orb heal: {current:F0} → {next:F0}";
            }

            case UpgradeEffectType.HpOrbMagnetRadius:
            {
                if (stats == null) break;
                float current = stats.GetOrbMagnetRadius();
                float next = current + data.valueA;
                return $"Magnet: {current:F0}m → {next:F0}m";
            }

            case UpgradeEffectType.DashChargeFlat:
            {
                if (pc == null) break;
                int current = pc.MaxDashCharges;
                int next = current + Mathf.RoundToInt(data.valueA);
                return $"Dash charges: {current} → {next}";
            }

            case UpgradeEffectType.DashCooldownMultiplier:
            {
                if (pc == null) break;
                float current = pc.EffectiveDashCooldown;
                float currentMul = sys != null ? sys.GetMultiplier(UpgradeEffectType.DashCooldownMultiplier) : 1f;
                float nextMul = Mathf.Max(0.05f, currentMul + data.valueA);
                float next = current * (nextMul / Mathf.Max(0.01f, currentMul));
                return $"Dash CD: {current:F2}s → {next:F2}s";
            }
        }
        return string.Empty;
    }
}
