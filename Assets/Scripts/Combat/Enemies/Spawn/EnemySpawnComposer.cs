using System.Collections.Generic;
using UnityEngine;

// Phase 3 / PR 3.D — pure-logic composer.
// Given a profile + arenaIndex, produces a roster (one EnemySpawnEntry per
// enemy that should be instantiated this encounter), respecting:
//   * budget = baseBudget + arenaIndex * budgetPerArenaIndex (TZ §7.4)
//   * minArenaIndex gating (entries with min > arenaIndex are skipped)
//   * maxAlive per entry (cap on copies of the same enemy)
//   * maxEnemyTypesPerEncounter (variety cap)
//   * maxTanks / maxRanged role guards (TZ §7.6)
//   * weighted random selection within the picked role set
//
// Determinism: callers may pass their own System.Random for seeded runs.
// Without one, falls back to UnityEngine.Random (acceptable for runtime
// encounters that are not part of seeded run reproduction).
public static class EnemySpawnComposer
{
    public class Result
    {
        public List<EnemySpawnEntry> Roster = new List<EnemySpawnEntry>();
        public int Budget;
        public int Spent;
        public bool UsedFallback;
        public string FallbackReason;
    }

    public static Result Compose(EnemySpawnProfile profile, int arenaIndex, System.Random rng = null, float budgetMultiplier = 1f)
    {
        var result = new Result();

        if (profile == null || profile.entries == null || profile.entries.Length == 0)
        {
            result.UsedFallback = true;
            result.FallbackReason = "spawn profile is null or has no entries";
            Debug.LogWarning($"[EnemySpawnComposer] Fallback to caller's default — {result.FallbackReason}.");
            return result;
        }

        result.Budget = Mathf.RoundToInt((profile.baseBudget + arenaIndex * profile.budgetPerArenaIndex) * Mathf.Max(0.1f, budgetMultiplier));
        if (result.Budget <= 0)
        {
            result.UsedFallback = true;
            result.FallbackReason = $"budget <= 0 (base {profile.baseBudget} + index {arenaIndex} * per {profile.budgetPerArenaIndex})";
            Debug.LogWarning($"[EnemySpawnComposer] Fallback to caller's default — {result.FallbackReason}.");
            return result;
        }

        // 1. Eligible entries: valid + arenaIndex gate satisfied.
        var eligible = new List<EnemySpawnEntry>();
        for (int i = 0; i < profile.entries.Length; i++)
        {
            var e = profile.entries[i];
            if (!e.IsValid) continue;
            if (arenaIndex < e.ResolvedMinArenaIndex) continue;
            eligible.Add(e);
        }

        if (eligible.Count == 0)
        {
            result.UsedFallback = true;
            result.FallbackReason = $"no entries pass arenaIndex {arenaIndex} gate";
            Debug.LogWarning($"[EnemySpawnComposer] Fallback to caller's default — {result.FallbackReason}.");
            return result;
        }

        // 2. Pick role set: how many distinct entries we allow this encounter.
        //    1-2 in early arenas, 2-3 later — TZ §7.4. Capped by profile and by
        //    how many eligible we actually have.
        int targetVariety = arenaIndex <= 0 ? 1 : (arenaIndex >= 2 ? 3 : 2);
        int variety = Mathf.Min(targetVariety, profile.maxEnemyTypesPerEncounter, eligible.Count);

        // Weighted shuffle to pick `variety` distinct entries — heavier-weighted
        // entries are more likely to be picked.
        var picked = WeightedPickDistinct(eligible, variety, rng);

        // 3. Bookkeeping for caps.
        var alive = new Dictionary<EnemySpawnEntry, int>();
        int tankCount = 0;
        int rangedCount = 0;

        // 4. Spend budget. Each iteration picks one weighted entry from `picked`,
        //    respecting maxAlive + role caps. Bail when nothing fits.
        int safetyMax = 256;
        while (result.Budget - result.Spent > 0 && safetyMax-- > 0)
        {
            var pool = new List<EnemySpawnEntry>(picked.Count);
            for (int i = 0; i < picked.Count; i++)
            {
                var e = picked[i];
                int already = alive.TryGetValue(e, out var n) ? n : 0;
                if (already >= e.ResolvedMaxAlive) continue;
                if (e.ResolvedSpawnCost > result.Budget - result.Spent) continue;
                if (e.Role == EnemyRole.Tank && tankCount >= profile.maxTanks) continue;
                if (e.Role == EnemyRole.Ranged && rangedCount >= profile.maxRanged) continue;
                pool.Add(e);
            }

            if (pool.Count == 0) break;

            var chosen = WeightedPickOne(pool, rng);
            result.Roster.Add(chosen);
            result.Spent += chosen.ResolvedSpawnCost;
            alive[chosen] = (alive.TryGetValue(chosen, out var existing) ? existing : 0) + 1;
            if (chosen.Role == EnemyRole.Tank) tankCount++;
            else if (chosen.Role == EnemyRole.Ranged) rangedCount++;
        }

        // 5. If after all that we somehow produced nothing — also a fallback.
        if (result.Roster.Count == 0)
        {
            result.UsedFallback = true;
            result.FallbackReason = "all eligible entries were filtered out by caps";
            Debug.LogWarning($"[EnemySpawnComposer] Fallback to caller's default — {result.FallbackReason}.");
        }

        return result;
    }

    static List<EnemySpawnEntry> WeightedPickDistinct(List<EnemySpawnEntry> pool, int count, System.Random rng)
    {
        var output = new List<EnemySpawnEntry>(count);
        var working = new List<EnemySpawnEntry>(pool);
        for (int i = 0; i < count && working.Count > 0; i++)
        {
            var pick = WeightedPickOne(working, rng);
            output.Add(pick);
            working.Remove(pick);
        }
        return output;
    }

    static EnemySpawnEntry WeightedPickOne(List<EnemySpawnEntry> pool, System.Random rng)
    {
        float total = 0f;
        for (int i = 0; i < pool.Count; i++) total += Mathf.Max(0.0001f, pool[i].ResolvedWeight);

        double roll = rng != null ? rng.NextDouble() * total : UnityEngine.Random.value * total;
        float acc = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += Mathf.Max(0.0001f, pool[i].ResolvedWeight);
            if (roll <= acc) return pool[i];
        }
        return pool[pool.Count - 1];
    }
}
