using System.Collections.Generic;

// Phase 4 / PR 4.PF — deterministic Shop inventory builder.
// The caller owns seed derivation; this class only consumes the System.Random.
public static class ShopInventoryGenerator
{
    public static ShopOffer[] Generate(
        System.Random rng,
        IList<UpgradeData> pool,
        UpgradeSystem upgradeSystem,
        int visitedArenaIndex)
    {
        if (rng == null) rng = new System.Random(0);

        var result = new List<ShopOffer>(3);
        result.Add(GenerateHeal(rng));

        var upgrades = PickUpgrades(rng, pool, upgradeSystem, visitedArenaIndex, 2);
        for (int i = 0; i < upgrades.Count; i++)
            result.Add(ShopOffer.Upgrade(upgrades[i]));

        return result.ToArray();
    }

    static ShopOffer GenerateHeal(System.Random rng)
    {
        // One heal offer per shop. The roll is deterministic and keeps the
        // player from seeing all three heal sizes at once.
        int roll = rng.Next(0, 100);
        if (roll < 45) return ShopOffer.Heal("Small Heal", 0.25f, 10);
        if (roll < 82) return ShopOffer.Heal("Medium Heal", 0.50f, 18);
        return ShopOffer.Heal("Full Heal", 1.00f, 32);
    }

    static List<UpgradeData> PickUpgrades(
        System.Random rng,
        IList<UpgradeData> pool,
        UpgradeSystem upgradeSystem,
        int visitedArenaIndex,
        int count)
    {
        var candidates = new List<UpgradeData>();
        if (pool != null)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                var u = pool[i];
                if (u == null) continue;
                if (!u.canAppearInShop) continue;
                if (visitedArenaIndex < u.minArenaIndex) continue;
                if (upgradeSystem != null && upgradeSystem.IsMaxed(u)) continue;
                candidates.Add(u);
            }
        }

        var result = new List<UpgradeData>(count);
        while (result.Count < count && candidates.Count > 0)
        {
            int pick = PickWeightedByRarity(rng, candidates);
            result.Add(candidates[pick]);
            candidates.RemoveAt(pick);
        }
        return result;
    }

    static int PickWeightedByRarity(System.Random rng, List<UpgradeData> candidates)
    {
        float total = 0f;
        for (int i = 0; i < candidates.Count; i++) total += Weight(candidates[i].rarity);

        float roll = (float)rng.NextDouble() * total;
        float acc = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            acc += Weight(candidates[i].rarity);
            if (roll <= acc) return i;
        }
        return candidates.Count - 1;
    }

    static float Weight(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Rare: return 0.32f;
            case UpgradeRarity.Epic: return 0.10f;
            case UpgradeRarity.Legendary: return 0.02f;
            default: return 0.56f;
        }
    }

    public static int RerollPrice(int rerollCount)
    {
        if (rerollCount <= 0) return 8;
        if (rerollCount == 1) return 14;
        if (rerollCount == 2) return 22;
        return 22 + (rerollCount - 2) * 10;
    }
}
