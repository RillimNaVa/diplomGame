using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.Arena
{
    /// <summary>
    /// Deterministic platform + ramp planner for PR 2.D.
    /// Reachability is guaranteed by construction: every platform gets a
    /// dedicated floor-to-platform ramp footprint that stays inside the arena
    /// mask and reserves its cells against cover/spawn placement.
    /// </summary>
    public static class ArenaVerticalityPlanner
    {
        const float PlatformThicknessMeters = 0.8f;
        const float RampThicknessMeters = 0.6f;
        const int MinDistanceFromStart = 3;
        const int MinDistanceFromExit = 2;

        public static bool[,] Plan(
            ArenaRoomData room,
            ArenaTypeProfile profile,
            ArenaRunConfig cfg,
            Vector2Int startCellLocal,
            System.Random rng)
        {
            int width = room.shapeMask.GetLength(0);
            int height = room.shapeMask.GetLength(1);
            var reserved = new bool[width, height];
            if (profile == null || !profile.enableVerticality) return reserved;

            int minPlatforms = Mathf.Max(0, Mathf.Min(profile.platformCountRange.x, profile.platformCountRange.y));
            int maxPlatforms = Mathf.Max(minPlatforms, Mathf.Max(profile.platformCountRange.x, profile.platformCountRange.y));
            if (maxPlatforms <= 0) return reserved;

            int platformTarget = rng.Next(minPlatforms, maxPlatforms + 1);
            if (platformTarget <= 0) return reserved;

            var exitCells = new List<Vector2Int>(room.exitDoorAnchors.Count);
            for (int i = 0; i < room.exitDoorAnchors.Count; i++)
            {
                var exit = room.exitDoorAnchors[i];
                exitCells.Add(new Vector2Int(exit.cell.x - room.boundsCells.xMin, exit.cell.y - room.boundsCells.yMin));
            }

            int attempts = 0;
            int maxAttempts = platformTarget * 32;
            while (room.platformPlacements.Count < platformTarget && attempts < maxAttempts)
            {
                attempts++;

                int footprintMin = Mathf.Max(1, Mathf.Min(profile.platformFootprintRange.x, profile.platformFootprintRange.y));
                int footprintMax = Mathf.Max(footprintMin, Mathf.Max(profile.platformFootprintRange.x, profile.platformFootprintRange.y));
                int footprintW = rng.Next(footprintMin, footprintMax + 1);
                int footprintH = rng.Next(footprintMin, footprintMax + 1);
                if (footprintW >= width || footprintH >= height) continue;

                int px = rng.Next(0, width - footprintW + 1);
                int py = rng.Next(0, height - footprintH + 1);
                if (!RectFits(room.shapeMask, reserved, px, py, footprintW, footprintH)) continue;

                Vector2Int centerCell = new Vector2Int(px + footprintW / 2, py + footprintH / 2);
                if (Manhattan(centerCell, startCellLocal) < MinDistanceFromStart) continue;

                bool nearExit = false;
                for (int i = 0; i < exitCells.Count; i++)
                {
                    if (Manhattan(centerCell, exitCells[i]) < MinDistanceFromExit)
                    {
                        nearExit = true;
                        break;
                    }
                }
                if (nearExit) continue;

                float topHeight = RandomRange(profile.platformHeightRange, rng);
                if (topHeight <= 0.5f) continue;

                if (!TryCreateRamp(room.boundsCells, room.shapeMask, reserved, cfg, px, py, footprintW, footprintH, topHeight, rng, out RampPlacement ramp, out RectInt rampRect))
                    continue;

                room.platformPlacements.Add(new PlatformPlacement
                {
                    center = RectCenterWorld(room.boundsCells, px, py, footprintW, footprintH, cfg.macroCellMeters,
                        topHeight - PlatformThicknessMeters * 0.5f),
                    size = new Vector3(footprintW * cfg.macroCellMeters, PlatformThicknessMeters, footprintH * cfg.macroCellMeters),
                    yawDeg = 0f
                });
                room.rampPlacements.Add(ramp);

                MarkRect(reserved, px, py, footprintW, footprintH);
                MarkRect(reserved, rampRect.xMin, rampRect.yMin, rampRect.width, rampRect.height);
            }

            return reserved;
        }

        static bool TryCreateRamp(
            RectInt bounds,
            bool[,] mask,
            bool[,] reserved,
            ArenaRunConfig cfg,
            int px,
            int py,
            int footprintW,
            int footprintH,
            float topHeight,
            System.Random rng,
            out RampPlacement ramp,
            out RectInt rampRect)
        {
            int runCells = Mathf.Clamp(Mathf.CeilToInt(topHeight / Mathf.Max(0.1f, cfg.macroCellMeters * 0.75f)), 1, 4);
            int[] sides = new int[] { 0, 1, 2, 3 };
            Shuffle(sides, rng);

            for (int i = 0; i < sides.Length; i++)
            {
                int side = sides[i];
                if (!TryGetRampRect(side, px, py, footprintW, footprintH, runCells, out rampRect)) continue;
                if (!RectFits(mask, reserved, rampRect.xMin, rampRect.yMin, rampRect.width, rampRect.height)) continue;

                float runMeters = RampRunMeters(side, footprintW, footprintH, runCells, cfg.macroCellMeters);
                float rampWidthMeters = RampWidthMeters(side, footprintW, footprintH, cfg.macroCellMeters);
                float yawDeg = side switch
                {
                    0 => 0f,
                    1 => 180f,
                    2 => 90f,
                    _ => 270f
                };

                ramp = new RampPlacement
                {
                    center = RectCenterWorld(bounds, rampRect.xMin, rampRect.yMin, rampRect.width, rampRect.height,
                        cfg.macroCellMeters, topHeight * 0.5f),
                    size = IsNorthSouth(side)
                        ? new Vector3(rampWidthMeters, RampThicknessMeters, runMeters)
                        : new Vector3(runMeters, RampThicknessMeters, rampWidthMeters),
                    yawDeg = yawDeg,
                    pitchDeg = Mathf.Rad2Deg * Mathf.Atan2(topHeight, Mathf.Max(0.01f, runMeters))
                };
                return true;
            }

            ramp = default;
            rampRect = default;
            return false;
        }

        static RectInt TryGetRect(int x, int y, int width, int height) => new RectInt(x, y, width, height);

        static bool TryGetRampRect(int side, int px, int py, int footprintW, int footprintH, int runCells, out RectInt rect)
        {
            switch (side)
            {
                case 0: // north: ramp extends outward in +Y
                    rect = TryGetRect(px, py + footprintH, footprintW, runCells);
                    return true;
                case 1: // south
                    rect = TryGetRect(px, py - runCells, footprintW, runCells);
                    return true;
                case 2: // east
                    rect = TryGetRect(px + footprintW, py, runCells, footprintH);
                    return true;
                default: // west
                    rect = TryGetRect(px - runCells, py, runCells, footprintH);
                    return true;
            }
        }

        static bool RectFits(bool[,] mask, bool[,] reserved, int xMin, int yMin, int width, int height)
        {
            if (width <= 0 || height <= 0) return false;
            int maxX = mask.GetLength(0);
            int maxY = mask.GetLength(1);
            if (xMin < 0 || yMin < 0 || xMin + width > maxX || yMin + height > maxY) return false;

            for (int y = yMin; y < yMin + height; y++)
            for (int x = xMin; x < xMin + width; x++)
            {
                if (!mask[x, y] || reserved[x, y]) return false;
            }

            return true;
        }

        static void MarkRect(bool[,] reserved, int xMin, int yMin, int width, int height)
        {
            int maxX = reserved.GetLength(0);
            int maxY = reserved.GetLength(1);
            for (int y = Mathf.Max(0, yMin); y < Mathf.Min(maxY, yMin + height); y++)
            for (int x = Mathf.Max(0, xMin); x < Mathf.Min(maxX, xMin + width); x++)
                reserved[x, y] = true;
        }

        static Vector3 RectCenterWorld(RectInt bounds, int xMin, int yMin, int width, int height, float metersPerCell, float y)
        {
            float worldX = (bounds.xMin + xMin + width * 0.5f) * metersPerCell;
            float worldZ = (bounds.yMin + yMin + height * 0.5f) * metersPerCell;
            return new Vector3(worldX, y, worldZ);
        }

        static float RampRunMeters(int side, int footprintW, int footprintH, int runCells, float metersPerCell)
            => runCells * metersPerCell;

        static float RampWidthMeters(int side, int footprintW, int footprintH, float metersPerCell)
            => (IsNorthSouth(side) ? footprintW : footprintH) * metersPerCell;

        static bool IsNorthSouth(int side) => side == 0 || side == 1;

        static float RandomRange(Vector2 range, System.Random rng)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            return min + (float)rng.NextDouble() * (max - min);
        }

        static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        static void Shuffle(int[] values, System.Random rng)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
