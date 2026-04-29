using System.Collections.Generic;
using UnityEngine;

// Phase 3 / PR 3.E — global cap on simultaneously-resolving enemy attacks.
// Each EnemyBrainBase requests a slot before transitioning Move -> Telegraph;
// if no slot is available the brain stays in Move/Reposition until one frees up.
// See docs/ENEMY_AI_TZ.md §8.3.
//
// Auto-creates a scene-singleton GameObject on first access so prefabs do not
// need explicit wiring. Caps are tunable in the Inspector when authored manually.
public class ActiveAttackSlotManager : MonoBehaviour
{
    [Header("Slot caps (TZ §8.3)")]
    [Min(0)] public int meleeSlots = 3;
    [Min(0)] public int rangedSlots = 3;
    [Min(0)] public int heavySlots = 1;
    [Min(0)] public int specialSlots = 1;

    static ActiveAttackSlotManager s_instance;

    readonly HashSet<EnemyBrainBase> melee = new HashSet<EnemyBrainBase>();
    readonly HashSet<EnemyBrainBase> ranged = new HashSet<EnemyBrainBase>();
    readonly HashSet<EnemyBrainBase> heavy = new HashSet<EnemyBrainBase>();
    readonly HashSet<EnemyBrainBase> special = new HashSet<EnemyBrainBase>();

    public static ActiveAttackSlotManager Instance
    {
        get
        {
            if (s_instance != null) return s_instance;
            ActiveAttackSlotManager found =
#if UNITY_2023_1_OR_NEWER
                Object.FindFirstObjectByType<ActiveAttackSlotManager>();
#else
                Object.FindObjectOfType<ActiveAttackSlotManager>();
#endif
            if (found != null) { s_instance = found; return s_instance; }

            GameObject go = new GameObject("ActiveAttackSlotManager");
            s_instance = go.AddComponent<ActiveAttackSlotManager>();
            return s_instance;
        }
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(this);
            return;
        }
        s_instance = this;
    }

    void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    public static AttackSlotKind KindForRole(EnemyRole role)
    {
        switch (role)
        {
            case EnemyRole.Ranged:  return AttackSlotKind.Ranged;
            case EnemyRole.Tank:    return AttackSlotKind.Heavy;
            case EnemyRole.Zoner:   return AttackSlotKind.Special;
            case EnemyRole.Boss:    return AttackSlotKind.Heavy;
            // Fodder, Chaser, default
            default:                return AttackSlotKind.Melee;
        }
    }

    public bool TryAcquire(EnemyBrainBase holder, AttackSlotKind kind)
    {
        if (holder == null) return false;
        HashSet<EnemyBrainBase> set = SetFor(kind);
        int cap = CapFor(kind);
        // Idempotent: re-acquire by an already-holding brain succeeds without
        // double-counting — protects against accidental double calls during
        // state transitions.
        if (set.Contains(holder)) return true;
        if (set.Count >= cap) return false;
        set.Add(holder);
        return true;
    }

    public void Release(EnemyBrainBase holder)
    {
        if (holder == null) return;
        // Holder kind isn't tracked — just remove from all sets. Cheap.
        melee.Remove(holder);
        ranged.Remove(holder);
        heavy.Remove(holder);
        special.Remove(holder);
    }

    public bool IsHolding(EnemyBrainBase holder)
    {
        return holder != null && (
            melee.Contains(holder) || ranged.Contains(holder) ||
            heavy.Contains(holder) || special.Contains(holder));
    }

    HashSet<EnemyBrainBase> SetFor(AttackSlotKind kind)
    {
        switch (kind)
        {
            case AttackSlotKind.Ranged:  return ranged;
            case AttackSlotKind.Heavy:   return heavy;
            case AttackSlotKind.Special: return special;
            default:                     return melee;
        }
    }

    int CapFor(AttackSlotKind kind)
    {
        switch (kind)
        {
            case AttackSlotKind.Ranged:  return rangedSlots;
            case AttackSlotKind.Heavy:   return heavySlots;
            case AttackSlotKind.Special: return specialSlots;
            default:                     return meleeSlots;
        }
    }
}
