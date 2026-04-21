using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;

namespace VoidSurvivor.ProceduralArena.Arena
{
    /// <summary>
    /// Poisson-disk-style cover placement with flow constraints:
    ///   * no cover within doorExclusionCells of any exit door anchor;
    ///   * no cover within startExclusionCells of the start spawn cell;
    ///   * no cover in the "axial corridor" — cells that lie on any straight
    ///     line between start spawn cell and an exit door cell (simple AABB
    ///     sweep along major axis).
    /// Density is per 100 m² of walkable floor area (mask-interior cells).
    /// </summary>
    public static class ArenaCoverPlanner
    {
        const int DoorExclusionCells = 2;
        const int StartExclusionCells = 3;

        public static List<CoverPlacement> Plan(
            ArenaRunConfig cfg,
            bool[,] shapeMask,
            RectInt bounds,
            Vector2Int startCellLocal,
            List<ExitDoorAnchor> exits,
            float coverDensity,
            System.Random rng)
        {
            var result = new List<CoverPlacement>();
            float m = cfg.macroCellMeters;
            int w = shapeMask.GetLength(0);
            int h = shapeMask.GetLength(1);

            int interiorCells = ArenaShapeGenerator.CountCells(shapeMask);
            float floorArea = interiorCells * m * m;
            int target = Mathf.RoundToInt((floorArea / 100f) * Mathf.Max(0f, coverDensity));
            if (target <= 0) return result;

            var exitCellsLocal = new List<Vector2Int>(exits.Count);
            foreach (var e in exits)
                exitCellsLocal.Add(new Vector2Int(e.cell.x - bounds.xMin, e.cell.y - bounds.yMin));

            var axialCorridor = BuildAxialCorridorMask(w, h, startCellLocal, exitCellsLocal);

            float minSpacing = Mathf.Max(cfg.coverMinSpacingMeters, cfg.coverWidthMeters * 1.5f);
            float minSpacingSq = minSpacing * minSpacing;
            int maxAttempts = target * 20;
            int attempts = 0;

            while (result.Count < target && attempts < maxAttempts)
            {
                attempts++;
                int cx = rng.Next(0, w);
                int cy = rng.Next(0, h);
                if (!shapeMask[cx, cy]) continue;
                if (axialCorridor[cx, cy]) continue;
                if (ManhattanDist(cx, cy, startCellLocal.x, startCellLocal.y) < StartExclusionCells) continue;
                bool nearDoor = false;
                foreach (var ec in exitCellsLocal)
                    if (ManhattanDist(cx, cy, ec.x, ec.y) < DoorExclusionCells) { nearDoor = true; break; }
                if (nearDoor) continue;

                int wx = cx + bounds.xMin;
                int wy = cy + bounds.yMin;
                float jitterRange = Mathf.Max(0f, m - cfg.coverWidthMeters);
                float mx = (float)rng.NextDouble() * jitterRange;
                float mz = (float)rng.NextDouble() * jitterRange;
                Vector3 pos = new Vector3(
                    wx * m + mx + cfg.coverWidthMeters * 0.5f,
                    cfg.coverHeightMeters * 0.5f,
                    wy * m + mz + cfg.coverWidthMeters * 0.5f);

                bool tooClose = false;
                for (int i = 0; i < result.Count; i++)
                {
                    if ((result[i].position - pos).sqrMagnitude < minSpacingSq)
                    { tooClose = true; break; }
                }
                if (tooClose) continue;

                result.Add(new CoverPlacement
                {
                    position = pos,
                    size = new Vector3(cfg.coverWidthMeters, cfg.coverHeightMeters, cfg.coverWidthMeters),
                    yawDeg = 0f
                });
            }
            return result;
        }

        static int ManhattanDist(int ax, int ay, int bx, int by) => Mathf.Abs(ax - bx) + Mathf.Abs(ay - by);

        static bool[,] BuildAxialCorridorMask(int w, int h, Vector2Int start, List<Vector2Int> exits)
        {
            var m = new bool[w, h];
            foreach (var e in exits) PaintAxialPath(m, start, e, w, h);
            return m;
        }

        static void PaintAxialPath(bool[,] m, Vector2Int a, Vector2Int b, int w, int h)
        {
            int x = a.x, y = a.y;
            int dx = b.x - a.x, dy = b.y - a.y;
            int sx = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
            int sy = dy == 0 ? 0 : (dy > 0 ? 1 : -1);
            while (x != b.x)
            {
                PaintBlob(m, x, y, w, h);
                x += sx;
            }
            while (y != b.y)
            {
                PaintBlob(m, x, y, w, h);
                y += sy;
            }
            PaintBlob(m, b.x, b.y, w, h);
        }

        static void PaintBlob(bool[,] m, int cx, int cy, int w, int h)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    m[x, y] = true;
                }
        }
    }
}
