using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;

namespace VoidSurvivor.ProceduralArena.Layout
{
    public static class BspLayoutGenerator
    {
        public static BspNode Generate(RectInt bounds, ArenaRunConfig cfg, int maxDepth, System.Random rng)
        {
            var root = new BspNode(bounds, 0);
            Split(root, cfg, maxDepth, rng);
            return root;
        }

        static void Split(BspNode node, ArenaRunConfig cfg, int maxDepth, System.Random rng)
        {
            if (node.depth >= maxDepth) return;

            int w = node.bounds.width;
            int h = node.bounds.height;
            int min = cfg.bspMinLeafCells;

            bool canSplitH = h >= 2 * min;
            bool canSplitV = w >= 2 * min;
            if (!canSplitH && !canSplitV) return;

            bool splitHorizontally;
            if (canSplitH && !canSplitV) splitHorizontally = true;
            else if (canSplitV && !canSplitH) splitHorizontally = false;
            else splitHorizontally = (h > w) ? true : (w > h ? false : rng.Next(2) == 0);

            int dim = splitHorizontally ? h : w;
            int center = dim / 2;
            int jitter = Mathf.RoundToInt(dim * cfg.bspSplitJitter);
            int minPos = Mathf.Max(min, center - jitter);
            int maxPos = Mathf.Min(dim - min, center + jitter);
            if (maxPos <= minPos) { minPos = min; maxPos = dim - min; }
            int cut = rng.Next(minPos, maxPos + 1);

            RectInt a, b;
            if (splitHorizontally)
            {
                a = new RectInt(node.bounds.xMin, node.bounds.yMin, w, cut);
                b = new RectInt(node.bounds.xMin, node.bounds.yMin + cut, w, h - cut);
            }
            else
            {
                a = new RectInt(node.bounds.xMin, node.bounds.yMin, cut, h);
                b = new RectInt(node.bounds.xMin + cut, node.bounds.yMin, w - cut, h);
            }

            node.left = new BspNode(a, node.depth + 1);
            node.right = new BspNode(b, node.depth + 1);
            Split(node.left, cfg, maxDepth, rng);
            Split(node.right, cfg, maxDepth, rng);
        }
    }
}
