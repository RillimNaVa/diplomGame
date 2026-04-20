using System;
using System.Diagnostics;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.Core
{
    public static class ArenaGenerator
    {
        public static ArenaRuntimeContext Generate(ArenaRunConfig cfg)
        {
            int seed = cfg.seed != 0 ? cfg.seed : MakeTimeSeed();
            var ctx = ArenaRuntimeContext.Create(seed);
            var sw = Stopwatch.StartNew();

            int maxDepth = cfg.bspMaxDepth;
            for (int attempt = 1; attempt <= cfg.maxGenerationAttempts; attempt++)
            {
                ctx.attemptsUsed = attempt;
                var layout = TryGenerate(cfg, ctx, maxDepth);
                if (layout != null && IsLayoutValid(layout, cfg))
                {
                    ctx.layout = layout;
                    sw.Stop();
                    ctx.generationTimeMs = sw.ElapsedMilliseconds;
                    return ctx;
                }
                ctx.Warn($"Attempt {attempt} failed (maxDepth={maxDepth}), retrying with reduced depth.");
                maxDepth = Math.Max(1, maxDepth - 1);
                ctx.layoutRng   = new System.Random(unchecked(seed ^ 0x1A2B ^ attempt));
                ctx.roomRng     = new System.Random(unchecked(seed ^ 0x3C4D ^ attempt));
                ctx.corridorRng = new System.Random(unchecked(seed ^ 0x5E6F ^ attempt));
            }

            ctx.Warn("All BSP attempts failed — using hand-coded fallback layout.");
            ctx.usedHandCodedFallback = true;
            ctx.layout = BuildHandCodedFallback(cfg, seed);
            sw.Stop();
            ctx.generationTimeMs = sw.ElapsedMilliseconds;
            return ctx;
        }

        static ArenaLayout TryGenerate(ArenaRunConfig cfg, ArenaRuntimeContext ctx, int maxDepth)
        {
            var outer = new RectInt(0, 0, cfg.mapWidthCells, cfg.mapHeightCells);
            var root = BspLayoutGenerator.Generate(outer, cfg, maxDepth, ctx.layoutRng);
            var rooms = RoomPlanner.PlaceRooms(root, cfg, ctx.roomRng);
            if (rooms.Count < 2) return null;

            var layout = new ArenaLayout
            {
                seed = ctx.masterSeed,
                outerBoundsCells = outer,
                bspRoot = root
            };
            layout.rooms.AddRange(rooms);

            var corridors = CorridorPlanner.Connect(rooms, cfg, ctx.corridorRng);
            layout.corridors.AddRange(corridors);

            if (!RoomTypeAssigner.AllRoomsConnected(layout)) return null;
            RoomTypeAssigner.Assign(layout);
            return layout;
        }

        static bool IsLayoutValid(ArenaLayout layout, ArenaRunConfig cfg)
        {
            if (layout.rooms.Count < Mathf.Min(2, cfg.targetRoomCount)) return false;
            if (layout.startRoomId < 0 || layout.exitRoomId < 0) return false;
            if (layout.startRoomId == layout.exitRoomId) return false;

            for (int i = 0; i < layout.rooms.Count; i++)
            for (int j = i + 1; j < layout.rooms.Count; j++)
                if (layout.rooms[i].boundsCells.Overlaps(layout.rooms[j].boundsCells))
                    return false;
            return true;
        }

        static ArenaLayout BuildHandCodedFallback(ArenaRunConfig cfg, int seed)
        {
            int w = cfg.mapWidthCells;
            int h = cfg.mapHeightCells;
            int rs = Mathf.Max(cfg.roomMinCells, 5);
            int gap = 2;

            var layout = new ArenaLayout
            {
                seed = seed,
                outerBoundsCells = new RectInt(0, 0, w, h)
            };

            var start  = new ArenaRoomData { id = 0, boundsCells = new RectInt(1, 1, rs, rs), type = RoomType.Start };
            var combatA = new ArenaRoomData { id = 1, boundsCells = new RectInt(1 + rs + gap, 1, rs, rs), type = RoomType.CombatMedium };
            var combatB = new ArenaRoomData { id = 2, boundsCells = new RectInt(1, 1 + rs + gap, rs, rs), type = RoomType.CombatMedium };
            var exit   = new ArenaRoomData { id = 3, boundsCells = new RectInt(1 + rs + gap, 1 + rs + gap, rs, rs), type = RoomType.Exit };

            layout.rooms.Add(start);
            layout.rooms.Add(combatA);
            layout.rooms.Add(combatB);
            layout.rooms.Add(exit);
            layout.startRoomId = start.id;
            layout.exitRoomId = exit.id;

            ConnectFallback(layout, start, combatA, cfg);
            ConnectFallback(layout, start, combatB, cfg);
            ConnectFallback(layout, combatA, exit, cfg);
            ConnectFallback(layout, combatB, exit, cfg);
            return layout;
        }

        static void ConnectFallback(ArenaLayout layout, ArenaRoomData a, ArenaRoomData b, ArenaRunConfig cfg)
        {
            var corridor = new ArenaCorridorData
            {
                roomAId = a.id,
                roomBId = b.id,
                widthCells = cfg.corridorWidthCells
            };
            var ca = a.CenterCell;
            var cb = b.CenterCell;
            var corner = new Vector2Int(cb.x, ca.y);
            corridor.pathCells.Add(ca);
            corridor.pathCells.Add(corner);
            corridor.pathCells.Add(cb);
            layout.corridors.Add(corridor);
            if (!a.connectedRoomIds.Contains(b.id)) a.connectedRoomIds.Add(b.id);
            if (!b.connectedRoomIds.Contains(a.id)) b.connectedRoomIds.Add(a.id);
        }

        static int MakeTimeSeed()
        {
            long t = DateTime.UtcNow.Ticks;
            return unchecked((int)(t ^ (t >> 32)));
        }
    }
}
