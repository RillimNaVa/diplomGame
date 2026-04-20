using System.Collections.Generic;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Layout
{
    public static class RoomTypeAssigner
    {
        public static void Assign(ArenaLayout layout)
        {
            if (layout.rooms.Count == 0) return;

            ArenaRoomData start = null;
            int bestKey = int.MaxValue;
            foreach (var r in layout.rooms)
            {
                var c = r.CenterCell;
                int key = c.x + c.y;
                if (key < bestKey) { bestKey = key; start = r; }
            }
            start.type = RoomType.Start;
            layout.startRoomId = start.id;

            var exit = FindFarthestRoom(layout, start.id);
            exit.type = RoomType.Exit;
            layout.exitRoomId = exit.id;

            foreach (var r in layout.rooms)
            {
                if (r.id == layout.startRoomId || r.id == layout.exitRoomId) continue;
                int area = r.AreaCells;
                if (area <= 30) r.type = RoomType.CombatSmall;
                else if (area <= 70) r.type = RoomType.CombatMedium;
                else r.type = RoomType.CombatLarge;
            }
        }

        static ArenaRoomData FindFarthestRoom(ArenaLayout layout, int startId)
        {
            var byId = new Dictionary<int, ArenaRoomData>();
            foreach (var r in layout.rooms) byId[r.id] = r;

            var dist = new Dictionary<int, int> { [startId] = 0 };
            var queue = new Queue<int>();
            queue.Enqueue(startId);
            int farthestId = startId;
            int farthestDist = 0;

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                int d = dist[cur];
                if (d > farthestDist) { farthestDist = d; farthestId = cur; }
                foreach (var nb in byId[cur].connectedRoomIds)
                {
                    if (dist.ContainsKey(nb)) continue;
                    dist[nb] = d + 1;
                    queue.Enqueue(nb);
                }
            }
            return byId[farthestId];
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
