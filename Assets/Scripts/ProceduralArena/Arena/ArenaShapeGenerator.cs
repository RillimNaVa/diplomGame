using System;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Arena
{
    public static class ArenaShapeGenerator
    {
        public static ArenaShape PickShape(ShapeWeight[] weights, System.Random rng)
        {
            if (weights == null || weights.Length == 0) return ArenaShape.Rect;
            float total = 0f;
            for (int i = 0; i < weights.Length; i++) total += Mathf.Max(0f, weights[i].weight);
            if (total <= 0f) return weights[0].shape;
            double roll = rng.NextDouble() * total;
            double acc = 0.0;
            for (int i = 0; i < weights.Length; i++)
            {
                acc += Mathf.Max(0f, weights[i].weight);
                if (roll <= acc) return weights[i].shape;
            }
            return weights[weights.Length - 1].shape;
        }

        public static bool[,] BuildMask(ArenaShape shape, int width, int height, System.Random rng)
        {
            var mask = new bool[width, height];
            switch (shape)
            {
                case ArenaShape.Rect:    FillRect(mask, width, height); break;
                case ArenaShape.LShape:  FillL(mask, width, height, rng); break;
                case ArenaShape.TShape:  FillT(mask, width, height, rng); break;
                case ArenaShape.Octagon: FillOctagon(mask, width, height); break;
                default: FillRect(mask, width, height); break;
            }
            return mask;
        }

        static void FillRect(bool[,] m, int w, int h)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    m[x, y] = true;
        }

        static void FillL(bool[,] m, int w, int h, System.Random rng)
        {
            FillRect(m, w, h);
            int cutW = Mathf.Max(2, w / 3);
            int cutH = Mathf.Max(2, h / 3);
            int corner = rng.Next(0, 4);
            int x0 = (corner & 1) == 0 ? 0 : w - cutW;
            int y0 = (corner & 2) == 0 ? 0 : h - cutH;
            Carve(m, x0, y0, cutW, cutH);
        }

        static void FillT(bool[,] m, int w, int h, System.Random rng)
        {
            FillRect(m, w, h);
            int cutW = Mathf.Max(2, w / 4);
            int cutH = Mathf.Max(2, h / 3);
            bool top = rng.NextDouble() < 0.5;
            int y0 = top ? h - cutH : 0;
            Carve(m, 0, y0, cutW, cutH);
            Carve(m, w - cutW, y0, cutW, cutH);
        }

        static void FillOctagon(bool[,] m, int w, int h)
        {
            FillRect(m, w, h);
            int cut = Mathf.Max(2, Mathf.Min(w, h) / 4);
            for (int y = 0; y < cut; y++)
            {
                int span = cut - y;
                for (int x = 0; x < span; x++)
                {
                    m[x, y] = false;
                    m[w - 1 - x, y] = false;
                    m[x, h - 1 - y] = false;
                    m[w - 1 - x, h - 1 - y] = false;
                }
            }
        }

        static void Carve(bool[,] m, int x0, int y0, int w, int h)
        {
            int xMax = Math.Min(m.GetLength(0), x0 + w);
            int yMax = Math.Min(m.GetLength(1), y0 + h);
            for (int y = Math.Max(0, y0); y < yMax; y++)
                for (int x = Math.Max(0, x0); x < xMax; x++)
                    m[x, y] = false;
        }

        public static int CountCells(bool[,] mask)
        {
            int c = 0;
            int w = mask.GetLength(0), h = mask.GetLength(1);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (mask[x, y]) c++;
            return c;
        }
    }
}
