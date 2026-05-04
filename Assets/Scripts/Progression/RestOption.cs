using UnityEngine;

// Phase 4 / PR 4.PG — runtime Rest Room option. Generated per Rest arena;
// not serialized as durable run state.
public enum RestOptionKind
{
    HealPercent,
    MaxHpFlat,
    RareBoost
}

public sealed class RestOption
{
    public RestOptionKind kind;
    public string title;
    public string description;
    public float healFraction;
    public int maxHpFlat;
    public int kpCost;

    public bool affordable;
    public bool selected;

    public static RestOption Heal(float fraction)
    {
        return new RestOption
        {
            kind = RestOptionKind.HealPercent,
            title = "EMERGENCY HEAL",
            description = $"Restore {Mathf.RoundToInt(fraction * 100f)}% max HP",
            healFraction = Mathf.Clamp01(fraction),
        };
    }

    public static RestOption MaxHp(int amount)
    {
        return new RestOption
        {
            kind = RestOptionKind.MaxHpFlat,
            title = "REINFORCE FRAME",
            description = $"+{amount} max HP for the rest of the run",
            maxHpFlat = Mathf.Max(0, amount),
        };
    }

    public static RestOption Rare(int cost)
    {
        return new RestOption
        {
            kind = RestOptionKind.RareBoost,
            title = "PRIME REWARD",
            description = $"Spend {cost} KP to boost rarity of the next reward",
            kpCost = Mathf.Max(0, cost),
        };
    }
}
