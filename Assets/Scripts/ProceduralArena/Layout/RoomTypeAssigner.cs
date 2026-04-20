// ============================================================================
// [DEPRECATED] Phase 2 r4 pivot (2026-04-20). Arena type is now carried by
// ArenaTypeProfile (ScriptableObject) on the run graph node, not derived from
// a BFS pass. Kept for reference only — see ARENA_GENERATION_TZ.md (r4).
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Layout
{
    public static class RoomTypeAssigner
    {
        public static void Assign(ArenaLayout layout)
        {
            if (layout.rooms.Count == 0) return;

            var smallCandidates = PickSmallerHalf(layout.rooms);

            ArenaRoomData start = null;
            int bestKey = int.MaxValue;
            foreach (var r in smallCandidates)
            {
                var c = r.CenterCell;
                int key = c.x + c.y;
                if (key < bestKey) { bestKey = key; start = r; }
            }
            if (start == null) start = layout.rooms[0];
            start.type = RoomType.Start;
            layout.startRoomId = start.id;

            var exit = FindFarthestRoom(layout, start.id, smallCandidates);
            exit.type = RoomType.Exit;
            layout.exitRoomId = exit.id;

            foreach (var r in layout.rooms)
            {
                if (r.id == layout.startRoomId || r.id == layout.exitRoomId) continue;
                int area = r.AreaCells;
                if (area <= 36) r.type = RoomType.CombatSmall;
                else if (area <= 80) r.type = RoomType.CombatMedium;
                else r.type = RoomType.CombatLarge;
            }
        }

        static List<ArenaRoomData> PickSmallerHalf(List<ArenaRoomData> rooms)
        {
            var sorted = new List<ArenaRoomData>(rooms);
            sorted.Sort((a, b) => a.AreaCells.CompareTo(b.AreaCells));
            int takeCount = Mathf.Max(1, (sorted.Count + 1) / 2);
            return sorted.GetRange(0, takeCount);
        }

        static ArenaRoomData FindFarthestRoom(ArenaLayout layout, int startId, List<ArenaRoomData> preferred)
        {
            var byId = new Dictionary<int, ArenaRoomData>();
            foreach (var r in layout.rooms) byId[r.id] = r;

            var dist = new Dictionary<int, int> { [startId] = 0 };
            var queue = new Queue<int>();
            queue.Enqueue(startId);

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                int d = dist[cur];
                foreach (var nb in byId[cur].connectedRoomIds)
                {
                    if (dist.ContainsKey(nb)) continue;
                    dist[nb] = d + 1;
                    queue.Enqueue(nb);
                }
            }

            var preferredIds = new HashSet<int>();
            foreach (var r in preferred) preferredIds.Add(r.id);

            int bestId = startId;
            int bestDist = -1;
            foreach (var kv in dist)
            {
                if (kv.Key == startId) continue;
                if (!preferredIds.Contains(kv.Key)) continue;
                if (kv.Value > bestDist) { bestDist = kv.Value; bestId = kv.Key; }
            }
            if (bestDist < 0)
            {
                foreach (var kv in dist)
                    if (kv.Key != startId && kv.Value > bestDist) { bestDist = kv.Value; bestId = kv.Key; }
            }
            return byId[bestId];
        }

        public static bool AllRoomsConnected(ArenaLayout layout)
        {
            if (layout.rooms.Count == 0) return true;
            var byId = new Dictionary<int, ArenaRoomData>();
            foreach (var r in layout.rooms) byId[r.id] = r;
            var visited = new HashSet<int> { layout.rooms[0].id };
            var queue = new Queue<int>();
            queue.Enqueue(layout.rooms[0].id);
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                foreach (var nb in byId[cur].connectedRoomIds)
                    if (visited.Add(nb)) queue.Enqueue(nb);
            }
            return visited.Count == layout.rooms.Count;
        }
    }
}
