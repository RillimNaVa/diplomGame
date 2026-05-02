using UnityEngine;

// Phase 4 / PR 4.PA — single ScriptableObject describing one upgrade.
// One asset per upgrade authored in the editor, never one C# class per upgrade.
// See docs/PHASE_4_ROGUELIKE_PROGRESSION_TZ.md §8.
[CreateAssetMenu(menuName = "Void Survivor/Progression/Upgrade Data", fileName = "UpgradeData")]
public sealed class UpgradeData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("Classification")]
    public UpgradeRarity rarity;
    public UpgradeCategory category;
    public UpgradeEffectType effectType;

    [Header("Stacking")]
    [Min(1)] public int maxStacks = 1;

    [Header("Values")]
    [Tooltip("Primary numeric payload (damage %, flat HP, duration multiplier, etc.).")]
    public float valueA;
    [Tooltip("Optional secondary payload (e.g. magnitude for triggered effects).")]
    public float valueB;
    [Tooltip("Triggered-effect duration in seconds. 0 = instant / not used.")]
    public float duration;

    [Header("Targeting")]
    [Tooltip("Empty = applies to all weapons. Non-empty = restricted to that weapon id.")]
    public string targetWeaponId;

    [Header("Availability")]
    public bool canAppearInReward = true;
    public bool canAppearInShop = true;
    [Tooltip("Earliest visited arena index where this upgrade may appear.")]
    public int minArenaIndex = 1;

    [Header("Shop")]
    public int baseShopPrice = 25;
}
