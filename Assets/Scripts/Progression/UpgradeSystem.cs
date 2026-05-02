using System;
using System.Collections.Generic;
using UnityEngine;

// Phase 4 / PR 4.PA — central runtime store for the active run's upgrades.
//
// Owns:
//   * the list of active upgrade stacks for the current run
//   * stack-limit enforcement
//   * passive modifier resolution (additive + multiplier with caps, §9.4.1)
//   * weapon-scoped modifier resolution (§9.5.1)
//   * event hooks for triggered upgrades (§9.4.2). Triggered effects themselves
//     live in dedicated components added in PR 4.PB onward; this class just
//     fans out the Notify* calls so they can subscribe.
//   * run-reset contract (§9.6).
//
// Auto-creating scene singleton mirrors EnemyPool / ActiveAttackSlotManager.
// See docs/PHASE_4_ROGUELIKE_PROGRESSION_TZ.md §9.
public class UpgradeSystem : MonoBehaviour
{
    static UpgradeSystem s_instance;

    readonly List<ActiveUpgradeStack> activeUpgrades = new List<ActiveUpgradeStack>();
    readonly Dictionary<string, ActiveUpgradeStack> byId =
        new Dictionary<string, ActiveUpgradeStack>(StringComparer.Ordinal);

    public IReadOnlyList<ActiveUpgradeStack> ActiveUpgrades => activeUpgrades;

    // Fired after AddUpgrade or ResetForNewRun so consumers (PlayerStats,
    // weapon resolvers, HUD) can refresh cached values lazily on next query.
    public event Action OnUpgradesChanged;

    // Triggered-effect entry points. Subscribers added in PR 4.PB+.
    public event Action<GameObject> OnEnemyKilled;
    public event Action<GameObject> OnGloryKill;
    public event Action<MonoBehaviour> OnWeaponFired;
    public event Action<MonoBehaviour, float> OnDamageDealt;
    public event Action<float> OnPlayerDamaged;
    public event Action<int> OnArenaCleared;

    public static UpgradeSystem Instance
    {
        get
        {
            if (s_instance != null) return s_instance;
            UpgradeSystem found =
#if UNITY_2023_1_OR_NEWER
                UnityEngine.Object.FindFirstObjectByType<UpgradeSystem>();
#else
                UnityEngine.Object.FindObjectOfType<UpgradeSystem>();
#endif
            if (found != null) { s_instance = found; return s_instance; }

            GameObject go = new GameObject("UpgradeSystem");
            s_instance = go.AddComponent<UpgradeSystem>();
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
    }

    // ------------------------------------------------------------------
    // Stack management
    // ------------------------------------------------------------------

    public bool CanAdd(UpgradeData data)
    {
        if (data == null) return false;
        if (string.IsNullOrEmpty(data.id))
        {
            Debug.LogWarning($"UpgradeSystem.CanAdd: '{data.name}' has empty id, rejecting.");
            return false;
        }
        if (byId.TryGetValue(data.id, out ActiveUpgradeStack existing))
        {
            return existing.stacks < data.maxStacks;
        }
        return true;
    }

    public void AddUpgrade(UpgradeData data)
    {
        if (!CanAdd(data)) return;

        if (byId.TryGetValue(data.id, out ActiveUpgradeStack existing))
        {
            existing.stacks++;
        }
        else
        {
            ActiveUpgradeStack stack = new ActiveUpgradeStack(data, 1);
            activeUpgrades.Add(stack);
            byId[data.id] = stack;
        }

        OnUpgradesChanged?.Invoke();
    }

    public int GetStackCount(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return 0;
        return byId.TryGetValue(upgradeId, out ActiveUpgradeStack s) ? s.stacks : 0;
    }

    public bool IsMaxed(UpgradeData data)
    {
        if (data == null) return false;
        return GetStackCount(data.id) >= data.maxStacks;
    }

    // ------------------------------------------------------------------
    // Modifier API (§9.4.1)
    // ------------------------------------------------------------------

    /// <summary>Sum of valueA × stacks for the given effect type (Flat / Triggered).
    /// Includes only stacks with empty targetWeaponId (global).</summary>
    public float GetAdditive(UpgradeEffectType type)
    {
        float sum = 0f;
        for (int i = 0; i < activeUpgrades.Count; i++)
        {
            ActiveUpgradeStack s = activeUpgrades[i];
            if (s.data.effectType != type) continue;
            if (!string.IsNullOrEmpty(s.data.targetWeaponId)) continue;
            sum += s.data.valueA * s.stacks;
        }
        float cap = type.BonusCap();
        if (cap > 0f && sum > cap) sum = cap;
        return sum;
    }

    /// <summary>Multiplier for global effects: 1 + capped Σ(valueA × stacks).</summary>
    public float GetMultiplier(UpgradeEffectType type)
    {
        return 1f + GetAdditive(type);
    }

    /// <summary>Weapon-scoped multiplier — 1 + Σ for stacks whose targetWeaponId
    /// matches. Global stacks are NOT included here (callers combine via
    /// GetMultiplier(type) × GetWeaponMultiplier(id, type), §9.5.1).</summary>
    public float GetWeaponMultiplier(string weaponId, UpgradeEffectType type)
    {
        if (string.IsNullOrEmpty(weaponId)) return 1f;
        float sum = 0f;
        for (int i = 0; i < activeUpgrades.Count; i++)
        {
            ActiveUpgradeStack s = activeUpgrades[i];
            if (s.data.effectType != type) continue;
            if (!string.Equals(s.data.targetWeaponId, weaponId, StringComparison.Ordinal)) continue;
            sum += s.data.valueA * s.stacks;
        }
        float cap = type.BonusCap();
        if (cap > 0f && sum > cap) sum = cap;
        return 1f + sum;
    }

    // ------------------------------------------------------------------
    // Event hook fan-out (§9.3, §9.4.2)
    // ------------------------------------------------------------------

    public void NotifyEnemyKilled(GameObject enemy) => OnEnemyKilled?.Invoke(enemy);
    public void NotifyGloryKill(GameObject enemy) => OnGloryKill?.Invoke(enemy);
    public void NotifyWeaponFired(MonoBehaviour weapon) => OnWeaponFired?.Invoke(weapon);
    public void NotifyDamageDealt(MonoBehaviour target, float damage) =>
        OnDamageDealt?.Invoke(target, damage);
    public void NotifyPlayerDamaged(float damage) => OnPlayerDamaged?.Invoke(damage);
    public void NotifyArenaCleared(int visitedArenaIndex) =>
        OnArenaCleared?.Invoke(visitedArenaIndex);

    // ------------------------------------------------------------------
    // Reset contract (§9.6)
    // ------------------------------------------------------------------

    /// <summary>Wipes all active upgrades. Triggered-effect components clean up
    /// their own runtime state by listening to OnUpgradesChanged. Called by
    /// RunController.StartRun, player death, debug restart.</summary>
    public void ResetForNewRun()
    {
        if (activeUpgrades.Count == 0)
        {
            OnUpgradesChanged?.Invoke();
            return;
        }
        activeUpgrades.Clear();
        byId.Clear();
        OnUpgradesChanged?.Invoke();
    }
}
