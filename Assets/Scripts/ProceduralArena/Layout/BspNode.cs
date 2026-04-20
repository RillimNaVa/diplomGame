using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Layout
{
    public class BspNode
    {
        public RectInt bounds;
        public int depth;
        public BspNode left;
        public BspNode right;
        public int roomId = -1;

        public bool IsLeaf => left == null && right == null;

        public BspNode(RectInt bounds, int depth)
        {
            this.bounds = bounds;
            this.depth = depth;
        }

        public void CollectLeaves(System.Collections.Generic.List<BspNode> acc)
        {
            if (IsLeaf) { acc.Add(this); return; }
            left?.CollectLeaves(acc);
            right?.CollectLeaves(acc);
        }
    }
}
