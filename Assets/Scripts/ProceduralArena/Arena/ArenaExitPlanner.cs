using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;

namespace VoidSurvivor.ProceduralArena.Arena
{
    public static class ArenaExitPlanner
    {
        public enum WallSide { West = 0, East = 1, South = 2, North = 3 }

        public static WallSide PickEntryWall(System.Random rng)
        {
            int i = rng.Next(0, 4);
            return (WallSide)i;
        }

        public static void Plan(
            ArenaRunConfig cfg,
            bool[,] shapeMask,
            RectInt bounds,
            int exitCount,
            WallSide entryWall,
            System.Random rng,
            List<ExitDoorAnchor> exits,
            out Vector3 startSpawnWorld,
            out Vector2Int entryCellLocal)
        {
            exits.Clear();
            float m = cfg.macroCellMeters;
            int w = shapeMask.GetLength(0);
            int h = shapeMask.GetLength(1);

            entryCellLocal = PickCellOnWall(shapeMask, entryWall, w, h, rng);
            Vector2Int entryWorldCell = new Vector2Int(entryCellLocal.x + bounds.xMin, entryCellLocal.y + bounds.yMin);
            startSpawnWorld = new Vector3(
                entryWorldCell.x * m + m * 0.5f + WallInwardX(entryWall) * m * 1.5f,
                0f,
                entryWorldCell.y * m + m * 0.5f + WallInwardY(entryWall) * m * 1.5f);

            WallSide primary, secondary;
            PickExitWalls(entryWall, rng, out primary, out secondary);

            var primaryCell = PickCellOnWall(shapeMask, primary, w, h, rng);
            exits.Add(MakeAnchor(primaryCell, primary, bounds, m));

            if (exitCount >= 2)
            {
                var secondaryCell = PickCellOnWall(shapeMask, secondary, w, h, rng);
                exits.Add(MakeAnchor(secondaryCell, secondary, bounds, m));
            }
        }

        static void PickExitWalls(WallSide entry, System.Random rng, out WallSide primary, out WallSide secondary)
        {
            // TZ: 2 exits on opposite walls, not on entry wall.
            // Pick a pair of opposite walls different from entry; if entry is on one of them, use the perpendicular pair.
            bool entryHorizontal = entry == WallSide.West || entry == WallSide.East;
            if (entryHorizontal)
            {
                primary = WallSide.South;
                secondary = WallSide.North;
            }
            else
            {
                primary = WallSide.West;
                secondary = WallSide.East;
            }
            if (rng.NextDouble() < 0.5)
            {
                var tmp = primary; primary = secondary; secondary = tmp;
            }
        }

        static Vector2Int PickCellOnWall(bool[,] mask, WallSide wall, int w, int h, System.Random rng)
        {
            // Scan candidate cells along the wall that are interior in mask and have exterior neighbour outward.
            var candidates = new List<Vector2Int>();
            switch (wall)
            {
                case WallSide.West:
                    for (int y = 1; y < h - 1; y++)
                        for (int x = 0; x < w; x++)
                            if (mask[x, y] && (x == 0 || !mask[x - 1, y])) { candidates.Add(new Vector2Int(x, y)); break; }
                    break;
                case WallSide.East:
                    for (int y = 1; y < h - 1; y++)
                        for (int x = w - 1; x >= 0; x--)
                            if (mask[x, y] && (x == w - 1 || !mask[x + 1, y])) { candidates.Add(new Vector2Int(x, y)); break; }
                    break;
                case WallSide.South:
                    for (int x = 1; x < w - 1; x++)
                        for (int y = 0; y < h; y++)
                            if (mask[x, y] && (y == 0 || !mask[x, y - 1])) { candidates.Add(new Vector2Int(x, y)); break; }
                    break;
                case WallSide.North:
                    for (int x = 1; x < w - 1; x++)
                        for (int y = h - 1; y >= 0; y--)
                            if (mask[x, y] && (y == h - 1 || !mask[x, y + 1])) { candidates.Add(new Vector2Int(x, y)); break; }
                    break;
            }
            if (candidates.Count == 0) return new Vector2Int(w / 2, h / 2);
            // prefer middle-ish (centered) candidate with some jitter
            int mid = candidates.Count / 2;
            int jitter = rng.Next(-candidates.Count / 4, candidates.Count / 4 + 1);
            int idx = Mathf.Clamp(mid + jitter, 0, candidates.Count - 1);
            return candidates[idx];
        }

        static ExitDoorAnchor MakeAnchor(Vector2Int local, WallSide wall, RectInt bounds, float m)
        {
            Vector2Int worldCell = new Vector2Int(local.x + bounds.xMin, local.y + bounds.yMin);
            Vector2Int outward = OutwardDir(wall);
            Vector3 center = new Vector3(worldCell.x * m + m * 0.5f, 0f, worldCell.y * m + m * 0.5f);
            // shift to wall face
            center += new Vector3(outward.x * m * 0.5f, 0f, outward.y * m * 0.5f);
            float yaw = OutwardYaw(wall);
            return new ExitDoorAnchor
            {
                cell = worldCell,
                outwardDir = outward,
                worldCenter = center,
                yawDeg = yaw
            };
        }

        static Vector2Int OutwardDir(WallSide w)
        {
            switch (w)
            {
                case WallSide.West:  return new Vector2Int(-1, 0);
                case WallSide.East:  return new Vector2Int( 1, 0);
                case WallSide.South: return new Vector2Int( 0, -1);
                case WallSide.North: return new Vector2Int( 0,  1);
            }
            return Vector2Int.zero;
        }

        static float OutwardYaw(WallSide w)
        {
            switch (w)
            {
                case WallSide.North: return 0f;
                case WallSide.East:  return 90f;
                case WallSide.South: return 180f;
                case WallSide.West:  return 270f;
            }
            return 0f;
        }

        static int WallInwardX(WallSide w) => -OutwardDir(w).x;
        static int WallInwardY(WallSide w) => -OutwardDir(w).y;
    }
}
