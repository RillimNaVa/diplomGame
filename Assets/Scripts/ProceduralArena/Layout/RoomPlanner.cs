using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;

namespace VoidSurvivor.ProceduralArena.Layout
{
    public static class RoomPlanner
    {
        public static List<ArenaRoomData> PlaceRooms(BspNode root, ArenaRunConfig cfg, System.Random rng)
        {
            var leaves = new List<BspNode>();
            root.CollectLeaves(leaves);

            var rooms = new List<ArenaRoomData>();
            int nextId = 0;

            foreach (var leaf in leaves)
            {
                int pad = cfg.roomPaddingCells;
                int availW = leaf.bounds.width - 2 * pad;
                int availH = leaf.bounds.height - 2 * pad;
                if (availW < cfg.roomMinCells || availH < cfg.roomMinCells) continue;

                int maxW = Mathf.Min(cfg.roomMaxCells, availW);
                int maxH = Mathf.Min(cfg.roomMaxCells, availH);
                int roomW = rng.Next(cfg.roomMinCells, maxW + 1);
                int roomH = rng.Next(cfg.roomMinCells, maxH + 1);

                int xMin = leaf.bounds.xMin + pad;
                int yMin = leaf.bounds.yMin + pad;
                int xOffMax = availW - roomW;
                int yOffMax = availH - roomH;
                int x = xMin + (xOffMax > 0 ? rng.Next(0, xOffMax + 1) : 0);
                int y = yMin + (yOffMax > 0 ? rng.Next(0, yOffMax + 1) : 0);

                var room = new ArenaRoomData
                {
                    id = nextId++,
                    boundsCells = new RectInt(x, y, roomW, roomH)
                };
                leaf.roomId = room.id;
                rooms.Add(room);
            }

            return rooms;
        }
    }
}
