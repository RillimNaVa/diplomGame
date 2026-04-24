using System;
using System.Diagnostics;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.Arena
{
    /// <summary>
    /// r4 micro-layer entry point. Produces a single-room ArenaLayout
    /// fully populated with shape mask, cover, exits, start spawn, and
    /// combat spawn points. Pure C# — no UnityEngine.Random.
    /// </summary>
    public static class SingleArenaGenerator
    {
        const int SizeJitterCells = 2;

        public static ArenaRuntimeContext Generate(int arenaSeed, ArenaTypeProfile profile, ArenaRunConfig cfg)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            int seed = arenaSeed != 0 ? arenaSeed : MakeTimeSeed();
            var ctx = ArenaRuntimeContext.Create(seed);
            var sw = Stopwatch.StartNew();

            var sizeRng    = new System.Random(unchecked(seed ^ 0x11CE));
            var shapeRng   = new System.Random(unchecked(seed ^ 0x22BF));
            var exitRng    = new System.Random(unchecked(seed ^ 0x33A0));
            var verticalRng = new System.Random(unchecked(seed ^ 0x4491));
            var coverRng   = new System.Random(unchecked(seed ^ 0x5582));
            var ceilingRng = new System.Random(unchecked(seed ^ 0x6673));
            var spawnRng   = new System.Random(unchecked(seed ^ 0x7764));

            // 1. Size with jitter
            Vector2Int baseSize = profile.size.BaseCells();
            int jw = sizeRng.Next(-SizeJitterCells, SizeJitterCells + 1);
            int jh = sizeRng.Next(-SizeJitterCells, SizeJitterCells + 1);
            int w = Mathf.Max(6, baseSize.x + jw);
            int h = Mathf.Max(6, baseSize.y + jh);
            var bounds = new RectInt(0, 0, w, h);

            // 2. Shape
            var shape = ArenaShapeGenerator.PickShape(profile.shapes, shapeRng);
            var mask = ArenaShapeGenerator.BuildMask(shape, w, h, shapeRng);

            // 3. Ceiling height
            float minC = Mathf.Min(profile.ceilingRange.x, profile.ceilingRange.y);
            float maxC = Mathf.Max(profile.ceilingRange.x, profile.ceilingRange.y);
            float ceiling = minC + (float)ceilingRng.NextDouble() * (maxC - minC);

            // 4. Build room data
            var room = new ArenaRoomData
            {
                id = 0,
                boundsCells = bounds,
                type = MapRoomType(profile.category),
                category = profile.category,
                shape = shape,
                shapeMask = mask,
                wallHeightMeters = ceiling,
                biomeId = profile.biome != null ? profile.biome.biomeId : string.Empty,
                biomeDebugTint = profile.biome != null ? profile.biome.debugTint : Color.white
            };

            // 5. Exits + start spawn
            int exitCount = Mathf.Clamp(profile.exitCount, 1, 2);
            var entryWall = ArenaExitPlanner.PickEntryWall(exitRng);
            Vector2Int entryCellLocal;
            Vector3 startSpawn;
            ArenaExitPlanner.Plan(cfg, mask, bounds, exitCount, entryWall, exitRng,
                room.exitDoorAnchors, out startSpawn, out entryCellLocal);
            room.startSpawnPoint = startSpawn;

            // 6. Verticality reserves cells against cover/spawn placement.
            // Calm utility arenas stay flat even if a profile is later misconfigured.
            bool[,] reserved = AllowsGeneratedVerticality(profile.category)
                ? ArenaVerticalityPlanner.Plan(room, profile, cfg, entryCellLocal, verticalRng)
                : new bool[mask.GetLength(0), mask.GetLength(1)];

            // 7. Cover
            var cover = ArenaCoverPlanner.Plan(cfg, mask, bounds, entryCellLocal,
                room.exitDoorAnchors, profile.coverDensity, reserved, coverRng);
            room.coverPlacements.AddRange(cover);

            // 8. Combat spawn points (simple interior grid sampling)
            PlanCombatSpawns(room, mask, bounds, cfg, profile.enemySpawnCount, entryCellLocal, reserved, spawnRng);

            // 9. Wrap into ArenaLayout (single room, no corridors)
            var layout = new ArenaLayout
            {
                seed = seed,
                outerBoundsCells = bounds,
                startRoomId = 0,
                exitRoomId = 0
            };
            layout.rooms.Add(room);
            ctx.layout = layout;
            ctx.spawnRng = spawnRng;
            ctx.attemptsUsed = 1;

            sw.Stop();
            ctx.generationTimeMs = sw.ElapsedMilliseconds;
            return ctx;
        }

        static void PlanCombatSpawns(
            ArenaRoomData room, bool[,] mask, RectInt bounds, ArenaRunConfig cfg,
            int count, Vector2Int entryCellLocal, bool[,] reservedMask, System.Random rng)
        {
            if (count <= 0) return;
            float m = cfg.macroCellMeters;
            int w = mask.GetLength(0);
            int h = mask.GetLength(1);
            int tries = 0;
            int maxTries = count * 30;
            while (room.combatSpawnPoints.Count < count && tries < maxTries)
            {
                tries++;
                int cx = rng.Next(1, w - 1);
                int cy = rng.Next(1, h - 1);
                if (!mask[cx, cy]) continue;
                if (reservedMask != null && reservedMask[cx, cy]) continue;
                if (Mathf.Abs(cx - entryCellLocal.x) + Mathf.Abs(cy - entryCellLocal.y) < 4) continue;
                int wx = cx + bounds.xMin, wy = cy + bounds.yMin;
                Vector3 p = new Vector3(wx * m + m * 0.5f, 0f, wy * m + m * 0.5f);
                bool tooClose = false;
                for (int i = 0; i < room.combatSpawnPoints.Count; i++)
                    if ((room.combatSpawnPoints[i] - p).sqrMagnitude < (m * 1.5f) * (m * 1.5f)) { tooClose = true; break; }
                if (tooClose) continue;
                room.combatSpawnPoints.Add(p);
            }
        }

        static RoomType MapRoomType(ArenaCategory c)
        {
            switch (c)
            {
                case ArenaCategory.Start: return RoomType.Start;
                case ArenaCategory.Boss:  return RoomType.Exit;
                case ArenaCategory.Shop:
                case ArenaCategory.Rest:
                case ArenaCategory.Parkour: return RoomType.Transition;
                default: return RoomType.CombatMedium;
            }
        }

        static bool AllowsGeneratedVerticality(ArenaCategory category)
        {
            switch (category)
            {
                case ArenaCategory.Combat:
                case ArenaCategory.Elite:
                case ArenaCategory.Parkour:
                case ArenaCategory.Boss:
                    return true;
                default:
                    return false;
            }
        }

        static int MakeTimeSeed()
        {
            long t = DateTime.UtcNow.Ticks;
            return unchecked((int)(t ^ (t >> 32)));
        }
    }
}
