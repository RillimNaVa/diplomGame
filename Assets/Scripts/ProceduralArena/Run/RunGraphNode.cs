using System.Collections.Generic;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// One node in the procedural run graph. Holds the per-arena seed and
    /// type-profile. Children is the list of next nodes (0..N) offered as a
    /// door-choice after this arena is cleared. Boss nodes have 0 children.
    ///
    /// Phase 4 / PR 4.PD: <see cref="stageIndex"/> (0..9) is the new primary
    /// depth metric. <see cref="arenaIndex"/> mirrors it for backwards-compat
    /// callers. <see cref="stage"/> is kept as a coarse legacy enum.
    /// </summary>
    public class RunGraphNode
    {
        public int id;

        /// <summary>Legacy 5-state enum. Prefer <see cref="stageIndex"/>.</summary>
        public RunStage stage;

        /// <summary>0..9 — depth in the 10-room run.</summary>
        public int stageIndex;

        /// <summary>Visited-path index used by reward rarity / KP / scaling. Equals
        /// <see cref="stageIndex"/> while the graph has shared-subtree wiring.</summary>
        public int arenaIndex;

        public int arenaSeed;
        public ArenaTypeProfile typeProfile;
        public readonly List<RunGraphNode> children = new List<RunGraphNode>();

        public ArenaCategory Category => typeProfile != null ? typeProfile.category : ArenaCategory.Combat;
    }
}
