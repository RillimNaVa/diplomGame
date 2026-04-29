using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;

namespace VoidSurvivor.ProceduralArena.Arena
{
    // Phase 2 / PR 2.H1 — picks N structures from the eligible pool, finds
    // valid cell positions on the macro grid, places them, and writes their
    // footprint into the reserved mask so cover/spawn don't overlap.
    //
    // Determinism: takes a System.Random sub-stream (structureRng) — zero
    // UnityEngine.Random. Same constraints as ArenaCoverPlanner: stays inside
    // the shape mask, leaves a 1-cell margin from walls and exits, doesn't
    // overlap reserved cells.
    public static class ArenaStructurePlanner
    {
        public static List<StructurePlacement> Plan(
            ArenaRunConfig cfg,
            ArenaCategory category,
            bool[,] mask,
            RectInt bounds,
            Vector2Int entryCellLocal,
            IList<ExitDoorAnchor> exits,
            bool[,] reserved,
            int budget,
            System.Random rng,
            StructureDefinition[] pool = null)
        {
            var result = new List<StructurePlacement>();
            if (budget <= 0) return result;
            if (mask == null || rng == null) return result;

            if (pool == null || pool.Length == 0) pool = BuiltInStructures.All();

            // Filter pool to category + accumulate weights for weighted pick.
            var eligible = new List<StructureDefinition>();
            float totalWeight = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] == null) continue;
                if (!pool[i].IsEligible(category)) continue;
                eligible.Add(pool[i]);
                totalWeight += Mathf.Max(0.01f, pool[i].weight);
            }
            if (eligible.Count == 0) return result;

            int w = mask.GetLength(0);
            int h = mask.GetLength(1);
            float m = cfg.macroCellMeters;

            int maxAttempts = budget * 40;
            int attempts = 0;
            while (result.Count < budget && attempts < maxAttempts)
            {
                attempts++;
                StructureDefinition def = WeightedPick(eligible, totalWeight, rng);
                if (def == null) break;

                // Pick a yaw that swaps footprint orientation 50/50.
                int yawIdx = rng.Next(0, 4);
                float yaw = yawIdx * 90f;
                Vector2Int fp = def.footprintCells;
                if (yawIdx == 1 || yawIdx == 3) fp = new Vector2Int(fp.y, fp.x);

                int cx = rng.Next(1, w - 1);
                int cy = rng.Next(1, h - 1);
                if (!FootprintFits(mask, reserved, w, h, cx, cy, fp)) continue;
                if (TooCloseToEntry(cx, cy, fp, entryCellLocal)) continue;
                if (TooCloseToExits(cx, cy, fp, bounds, exits, m)) continue;

                // Reserve the footprint so cover / spawn / further structures don't overlap.
                ReserveFootprint(reserved, cx, cy, fp);

                int wx = cx + bounds.xMin;
                int wy = cy + bounds.yMin;
                Vector3 worldPivot = new Vector3(
                    (wx + fp.x * 0.5f) * m,
                    0f,
                    (wy + fp.y * 0.5f) * m);

                result.Add(new StructurePlacement
                {
                    structureId = def.structureId,
                    position = worldPivot,
                    yawDeg = yaw,
                    footprintCells = fp,
                });
            }
            return result;
        }

        static StructureDefinition WeightedPick(
            List<StructureDefinition> eligible, float totalWeight, System.Random rng)
        {
            if (eligible.Count == 1) return eligible[0];
            float pick = (float)rng.NextDouble() * totalWeight;
            float acc = 0f;
            for (int i = 0; i < eligible.Count; i++)
            {
                acc += Mathf.Max(0.01f, eligible[i].weight);
                if (pick <= acc) return eligible[i];
            }
            return eligible[eligible.Count - 1];
        }

        static bool FootprintFits(bool[,] mask, bool[,] reserved, int w, int h,
            int cx, int cy, Vector2Int fp)
        {
            // Origin = bottom-left of the footprint. Center the placement so
            // (cx, cy) is treated as the lower-left of the footprint rectangle.
            for (int dx = 0; dx < fp.x; dx++)
            for (int dy = 0; dy < fp.y; dy++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (x < 1 || x >= w - 1 || y < 1 || y >= h - 1) return false;
                if (!mask[x, y]) return false;
                if (reserved != null && reserved[x, y]) return false;
            }
            return true;
        }

        static void ReserveFootprint(bool[,] reserved, int cx, int cy, Vector2Int fp)
        {
            if (reserved == null) return;
            for (int dx = 0; dx < fp.x; dx++)
            for (int dy = 0; dy < fp.y; dy++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (x >= 0 && x < reserved.GetLength(0) && y >= 0 && y < reserved.GetLength(1))
                    reserved[x, y] = true;
            }
        }

        static bool TooCloseToEntry(int cx, int cy, Vector2Int fp, Vector2Int entryCellLocal)
        {
            // Manhattan distance from any footprint corner to entry must be > 3.
            for (int dx = 0; dx <= fp.x; dx += fp.x)
            for (int dy = 0; dy <= fp.y; dy += fp.y)
            {
                int dist = Mathf.Abs((cx + dx) - entryCellLocal.x) + Mathf.Abs((cy + dy) - entryCellLocal.y);
                if (dist <= 3) return true;
            }
            return false;
        }

        static bool TooCloseToExits(int cx, int cy, Vector2Int fp, RectInt bounds,
            IList<ExitDoorAnchor> exits, float m)
        {
            if (exits == null || exits.Count == 0) return false;
            // Footprint center in world units.
            float fcx = (cx + bounds.xMin + fp.x * 0.5f) * m;
            float fcz = (cy + bounds.yMin + fp.y * 0.5f) * m;
            float minDist = m * 3.5f;
            for (int i = 0; i < exits.Count; i++)
            {
                Vector3 e = exits[i].worldCenter;
                float d = Mathf.Sqrt((e.x - fcx) * (e.x - fcx) + (e.z - fcz) * (e.z - fcz));
                if (d < minDist) return true;
            }
            return false;
        }
    }
}
