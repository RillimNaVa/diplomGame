using System.Collections.Generic;

namespace VoidSurvivor.ProceduralArena.Run
{
    public class RunGraph
    {
        public int runSeed;
        public readonly List<RunGraphNode> nodes = new List<RunGraphNode>();
        public RunGraphNode startNode;
    }
}
