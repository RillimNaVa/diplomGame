using VoidSurvivor.ProceduralArena.Arena;

// Phase 4 / PR 4.PE — payout data structs (TZ §12).
namespace VoidSurvivor.Progression
{
    /// <summary>Per-arena style accumulator snapshot, finalized at clear time.</summary>
    public struct StyleBreakdown
    {
        public int kills;            // ordinary kills (already counted, +1 each in stylePoints)
        public int bruteKills;       // brute kills (each replaces +1 with +2 → net +1 here is bonus)
        public int gloryKills;       // glory-kill bonuses (+1 each, on top of the kill itself)
        public int streakBonusKills; // kills made while CurrentStreak >= 5 (+2 each)
        public bool noHit;           // no damage taken in this encounter
        public bool fastClear;       // clearTime < 0.7 × expectedClearTime
        public float clearTime;
        public float expectedClearTime;
        /// <summary>Raw style points before per-room cap. Computed from the counters above.</summary>
        public int rawStylePoints;
    }

    /// <summary>Per-arena KP payout breakdown (TZ §12.6 panel).</summary>
    public struct ArenaPayout
    {
        public ArenaCategory category;
        public int arenaIndex;
        public int clearReward;
        public int rawStyle;
        public int styleCap;
        public int cappedStyle;
        public int totalKp;
        public StyleBreakdown style;
    }
}
