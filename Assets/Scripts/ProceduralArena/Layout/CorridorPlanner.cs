// ============================================================================
// [DEPRECATED] Phase 2 r4 pivot (2026-04-20). Corridors between rooms are no
// longer generated — each encounter is a single procedural arena. See
// ARENA_GENERATION_TZ.md (r4). Kept for reference only.
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;

namespace VoidSurvivor.ProceduralArena.Layout
{
    public static class CorridorPlanner
    {
        struct Edge
        {
            public int a, b;
            public int distSq;
        }

        public static List<ArenaCorridorData> Connect(
            List<ArenaRoomData> rooms, ArenaRunConfig cfg, System.Random rng)
        {
            var corridors = new List<ArenaCorridorData>();
            if (rooms.Count < 2) return corridors;

            var allEdges = new List<Edge>();
            for (int i = 0; i < rooms.Count; i++)
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var ca = rooms[i].CenterCell;
                var cb = rooms[j].CenterCell;
                int dx = ca.x - cb.x, dy = ca.y - cb.y;
                allEdges.Add(new Edge { a = i, b = j, distSq = dx * dx + dy * dy });
            }
            allEdges.Sort((x, y) => x.distSq.CompareTo(y.distSq));

            var parent = new int[rooms.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
            bool Union(int a, int b) { int ra = Find(a), rb = Find(b); if (ra == rb) return false; parent[ra] = rb; return true; }

            var mstEdges = new List<Edge>();
            foreach (var e in allEdges)
            {
                if (Union(e.a, e.b)) mstEdges.Add(e);
                if (mstEdges.Count == rooms.Count - 1) break;
            }

            var used = new HashSet<long>();
            foreach (var e in mstEdges)
            {
                BuildCorridor(rooms[e.a], rooms[e.b], cfg, rng, corridors);
                used.Add(PairKey(e.a, e.b));
            }

            foreach (var e in allEdges)
            {
                if (used.Contains(PairKey(e.a, e.b))) continue;
                if (rng.NextDouble() < cfg.extraCorridorChance)
                {
                    BuildCorridor(rooms[e.a], rooms[e.b], cfg, rng, corridors);
                    used.Add(PairKey(e.a, e.b));
                }
            }

            return corridors;
        }

        static long PairKey(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

        static void BuildCorridor(
            ArenaRoomData ra, ArenaRoomData rb, ArenaRunConfig cfg, System.Random rng,
            List<ArenaCorridorData> output)
        {
            var ca = ra.CenterCell;
            var cb = rb.CenterCell;
            bool horizontalFirst = rng.Next(2) == 0;

            var corridor = new ArenaCorridorData
            {
                roomAId = ra.id,
                roomBId = rb.id,
                widthCells = cfg.corridorWidthCells
            };

            Vector2Int corner = horizontalFirst
                ? new Vector2Int(cb.x, ca.y)
                : new Vector2Int(ca.x, cb.y);

            AddLine(corridor.pathCells, ca, corner);
            AddLine(corridor.pathCells, corner, cb);

            Vector2Int doorA = FindEdgeEntry(ra.boundsCells, corridor.pathCells);
            Vector2Int doorB = FindEdgeEntry(rb.boundsCells, corridor.pathCells);
            if (!ra.doorAnchorsCells.Contains(doorA)) ra.doorAnchorsCells.Add(doorA);
            if (!rb.doorAnchorsCells.Contains(doorB)) rb.doorAnchorsCells.Add(doorB);

            if (!ra.connectedRoomIds.Contains(rb.id)) ra.connectedRoomIds.Add(rb.id);
            if (!rb.connectedRoomIds.Contains(ra.id)) rb.connectedRoomIds.Add(ra.id);

            output.Add(corridor);
        }

        static void AddLine(List<Vector2Int> cells, Vector2Int a, Vector2Int b)
        {
            int x = a.x, y = a.y;
            int dx = Math.Sign(b.x - a.x);
            int dy = Math.Sign(b.y - a.y);
            if (dx != 0)
            {
                while (x != b.x) { if (cells.Count == 0 || cells[cells.Count - 1] != new Vector2Int(x, y)) cells.Add(new Vector2Int(x, y)); x += dx; }
            }
            if (dy != 0)
            {
                while (y != b.y) { if (cells.Count == 0 || cells[cells.Count - 1] != new Vector2Int(x, y)) cells.Add(new Vector2Int(x, y)); y += dy; }
            }
            if (cells.Count == 0 || cells[cells.Count - 1] != b) cells.Add(b);
        }

        static Vector2Int FindEdgeEntry(RectInt room, List<Vector2Int> path)
        {
            foreach (var p in path)
            {
                bool inside = p.x >= room.xMin && p.x < room.xMax && p.y >= room.yMin && p.y < room.yMax;
                bool onEdge = p.x == room.xMin || p.x == room.xMax - 1 || p.y == room.yMin || p.y == room.yMax - 1;
                if (inside && onEdge) return p;
            }
            int cx = Mathf.Clamp(path[0].x, room.xMin, room.xMax - 1);
            int cy = Mathf.Clamp(path[0].y, room.yMin, room.yMax - 1);
            return new Vector2Int(cx, cy);
        }
    }
}
