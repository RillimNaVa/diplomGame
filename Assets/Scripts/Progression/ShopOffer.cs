using UnityEngine;

// Phase 4 / PR 4.PF — one runtime shop offer. Generated per Shop arena;
// never serialized as durable run state.
public enum ShopOfferKind
{
    Heal,
    Upgrade
}

public sealed class ShopOffer
{
    public ShopOfferKind kind;
    public string title;
    public string description;
    public int price;
    public float healFraction;
    public UpgradeData upgrade;
    public bool purchased;

    public bool IsValid => price > 0 && (!purchased) && (kind == ShopOfferKind.Heal || upgrade != null);

    public static ShopOffer Heal(string title, float fraction, int price)
    {
        return new ShopOffer
        {
            kind = ShopOfferKind.Heal,
            title = title,
            description = $"Restore {Mathf.RoundToInt(fraction * 100f)}% max HP",
            price = price,
            healFraction = Mathf.Clamp01(fraction),
        };
    }

    public static ShopOffer Upgrade(UpgradeData data)
    {
        return new ShopOffer
        {
            kind = ShopOfferKind.Upgrade,
            title = data != null ? data.displayName : "Upgrade",
            description = data != null ? data.description : "",
            price = ResolveUpgradePrice(data),
            upgrade = data,
        };
    }

    public static int ResolveUpgradePrice(UpgradeData data)
    {
        if (data == null) return 0;
        if (data.baseShopPrice > 0) return data.baseShopPrice;
        switch (data.rarity)
        {
            case UpgradeRarity.Rare: return 44;
            case UpgradeRarity.Epic: return 70;
            case UpgradeRarity.Legendary: return 100;
            default: return 26;
        }
    }
}
