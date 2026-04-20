// ============================================================================
// [DEPRECATED] Phase 2 r4 pivot (2026-04-20). BSP multi-room layout is not the
// Phase 2 target anymore — see ARENA_GENERATION_TZ.md (r4). Kept for diploma
// reference only; do not extend or wire into new features.
// ============================================================================
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
