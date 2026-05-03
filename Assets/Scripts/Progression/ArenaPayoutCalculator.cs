using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.Progression
{
    /// <summary>
    /// Pure static. Implements TZ §12.3 clear reward + §12.4 style cap formulas.
    /// </summary>
    public static class ArenaPayoutCalculator
    {
        /// <summary>TZ §12.3 — base clear reward by room type.</summary>
        public static int ClearReward(ArenaCategory cat, int arenaIndex)
        {
            int idx = Mathf.Max(0, arenaIndex);
            switch (cat)
            {
                case ArenaCategory.Combat:  return 10 + idx * 2;
                case ArenaCategory.Elite:   return 18 + idx * 3;
                default:                    return 0; // Start/Shop/Rest/Boss/Parkour grant no KP.
            }
        }

        /// <summary>TZ §12.4 — style KP cap by room type (0 means no style payout).</summary>
        public static int StyleCap(ArenaCategory cat)
        {
            switch (cat)
            {
                case ArenaCategory.Combat: return 8;
                case ArenaCategory.Elite:  return 12;
                default:                   return 0;
            }
        }

        /// <summary>TZ §12.4 — composes raw style points from per-arena counters.</summary>
        public static int ComputeRawStyle(in StyleBreakdown s)
        {
            // §12.4 table:
            //   Kill +1
            //   Glory kill +1 bonus (in addition to +1 kill)
            //   Brute kill +2 (replaces the +1 kill — so net contribution is the same +1 plus +1 bonus)
            //   Kill streak ≥ 5 +2 bonus per kill while streak active
            //   Fast clear +3 (one-shot)
            //   No-hit clear +5 (one-shot)
            //
            // We store ordinary kills, brute kills, glory bonuses, and streak-bonus kills
            // as independent counters; combine them here so the policy lives in one place.
            int total = 0;
            total += s.kills;                  // +1 per kill
            total += s.bruteKills;             // +1 extra per brute (kill itself already counted in 'kills')
            total += s.gloryKills;             // +1 bonus per glory kill (kill itself already counted in 'kills')
            total += s.streakBonusKills * 2;   // +2 per kill made while streak ≥ 5
            if (s.fastClear) total += 3;
            if (s.noHit)     total += 5;
            return total;
        }

        public static ArenaPayout Compute(ArenaCategory cat, int arenaIndex, in StyleBreakdown style)
        {
            int raw = ComputeRawStyle(style);
            int cap = StyleCap(cat);
            int capped = cap > 0 ? Mathf.Min(raw, cap) : 0;
            int clear = ClearReward(cat, arenaIndex);
            return new ArenaPayout
            {
                category = cat,
                arenaIndex = arenaIndex,
                clearReward = clear,
                rawStyle = raw,
                styleCap = cap,
                cappedStyle = capped,
                totalKp = clear + capped,
                style = style,
            };
        }
    }
}
