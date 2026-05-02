using System.Collections.Generic;

// Phase 4 / PR 4.PC — pure logic that picks N reward cards from a pool.
// Deterministic with the System.Random passed in (TZ §16 seeded determinism).
//
// Algorithm:
//   1. Filter pool by minArenaIndex / canAppearInReward / not maxed.
//   2. Pick N distinct UpgradeData by sampling weighted-by-rarity from §10.3.
//      Weight per candidate = rarityWeights[rarity] (after Elite modifier).
//   3. If pool runs out before N, return whatever we have (caller fallback).
public static class RewardCardGenerator
{
    public static UpgradeData[] Generate(
        System.Random rng,
        IList<UpgradeData> pool,
        UpgradeSystem upgradeSystem,
        int visitedArenaIndex,
        bool eliteBonus,
        int cardCount = 3)
    {
        if (pool == null || pool.Count == 0 || rng == null)
            return new UpgradeData[0];

        // 1) Eligible candidates.
        List<UpgradeData> eligible = new List<UpgradeData>();
        for (int i = 0; i < pool.Count; i++)
        {
            UpgradeData u = pool[i];
            if (u == null) continue;
            if (!u.canAppearInReward) continue;
            if (visitedArenaIndex < u.minArenaIndex) continue;
            if (upgradeSystem != null && upgradeSystem.IsMaxed(u)) continue;
            eligible.Add(u);
        }

        if (eligible.Count == 0) return new UpgradeData[0];

        // 2) Per-rarity weights from TZ §10.3, indexed by visitedArenaIndex
        //    clamped to the table range.
        float[] rarityWeights = ResolveRarityWeights(visitedArenaIndex, eliteBonus);

        // 3) Weighted sample WITHOUT replacement.
        int picks = cardCount < eligible.Count ? cardCount : eligible.Count;
        UpgradeData[] result = new UpgradeData[picks];
        List<UpgradeData> bag = new List<UpgradeData>(eligible);
        for (int p = 0; p < picks; p++)
        {
            float total = 0f;
            for (int i = 0; i < bag.Count; i++) total += rarityWeights[(int)bag[i].rarity];
            if (total <= 0f) total = 1f;
            float roll = (float)rng.NextDouble() * total;
            float acc = 0f;
            int chosen = bag.Count - 1;
            for (int i = 0; i < bag.Count; i++)
            {
                acc += rarityWeights[(int)bag[i].rarity];
                if (roll <= acc) { chosen = i; break; }
            }
            result[p] = bag[chosen];
            bag.RemoveAt(chosen);
        }
        return result;
    }

    static float[] ResolveRarityWeights(int visitedArenaIndex, bool eliteBonus)
    {
        // TZ §10.3 — rows correspond to arena index 1..8.
        // Index 0 (Start room) reuses row 1; index >8 clamps to row 8.
        float[][] table =
        {
            new float[] { 0.80f, 0.20f, 0.00f, 0.00f }, // 1
            new float[] { 0.75f, 0.23f, 0.02f, 0.00f }, // 2
            new float[] { 0.68f, 0.28f, 0.04f, 0.00f }, // 3
            new float[] { 0.62f, 0.32f, 0.06f, 0.00f }, // 4
            new float[] { 0.55f, 0.37f, 0.08f, 0.00f }, // 5
            new float[] { 0.50f, 0.40f, 0.10f, 0.00f }, // 6
            new float[] { 0.45f, 0.42f, 0.12f, 0.01f }, // 7
            new float[] { 0.40f, 0.44f, 0.14f, 0.02f }, // 8
        };

        int row = visitedArenaIndex - 1;
        if (row < 0) row = 0;
        if (row >= table.Length) row = table.Length - 1;
        float[] src = table[row];

        float[] w = new float[4];
        for (int i = 0; i < 4; i++) w[i] = src[i];

        if (eliteBonus)
        {
            // §10.3 Elite reward modifier — Common -15%, Rare +10%, Epic +4%, Legendary +1%.
            w[0] = System.Math.Max(0f, w[0] - 0.15f);
            w[1] += 0.10f;
            w[2] += 0.04f;
            w[3] += 0.01f;
        }

        // Normalize.
        float sum = w[0] + w[1] + w[2] + w[3];
        if (sum > 0f) for (int i = 0; i < 4; i++) w[i] /= sum;
        return w;
    }
}
