using System.Collections.Generic;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// One node in the procedural run graph. Holds the per-arena seed and
    /// type-profile. Children is the list of next nodes (0..2) offered as a
    /// door-choice after this arena is cleared. Boss nodes have 0 children.
    /// </summary>
    public class RunGraphNode
    {
        public int id;
        public RunStage stage;
        public int arenaIndex;        // 0..4 — used for difficulty scaling
        public int arenaSeed;
        public ArenaTypeProfile typeProfile;
        public readonly List<RunGraphNode> children = new List<RunGraphNode>();
    }
}
