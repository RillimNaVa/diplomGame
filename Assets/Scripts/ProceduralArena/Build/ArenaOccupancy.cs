using UnityEngine;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.Build
{
    public enum CellKind : byte
    {
        Empty = 0,
        Room = 1,
        Corridor = 2
    }

    public class ArenaOccupancy
    {
        public readonly int width;
        public readonly int height;
        public readonly CellKind[] kind;
        public readonly int[] roomId;

        public ArenaOccupancy(int width, int height)
        {
            this.width = width;
            this.height = height;
            kind = new CellKind[width * height];
            roomId = new int[width * height];
            for (int i = 0; i < roomId.Length; i++) roomId[i] = -1;
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        public CellKind Get(int x, int y) => InBounds(x, y) ? kind[y * width + x] : CellKind.Empty;

        public int GetRoomId(int x, int y) => InBounds(x, y) ? roomId[y * width + x] : -1;

        public bool IsInterior(int x, int y)
        {
            var k = Get(x, y);
            return k == CellKind.Room || k == CellKind.Corridor;
        }

        public void SetRoom(int x, int y, int room)
        {
            if (!InBounds(x, y)) return;
            kind[y * width + x] = CellKind.Room;
            roomId[y * width + x] = room;
        }

        public void SetCorridor(int x, int y)
        {
            if (!InBounds(x, y)) return;
            if (kind[y * width + x] == CellKind.Room) return;
            kind[y * width + x] = CellKind.Corridor;
        }

        public static ArenaOccupancy Build(ArenaLayout layout, int corridorWidthCells)
        {
            var outer = layout.outerBoundsCells;
            var grid = new ArenaOccupancy(outer.width, outer.height);

            foreach (var r in layout.rooms)
            {
                var b = r.boundsCells;
                for (int y = b.yMin; y < b.yMax; y++)
                    for (int x = b.xMin; x < b.xMax; x++)
                        grid.SetRoom(x - outer.xMin, y - outer.yMin, r.id);
            }

            int halfLow = corridorWidthCells / 2;
            int halfHigh = corridorWidthCells - halfLow - 1;

            foreach (var c in layout.corridors)
            {
                var path = c.pathCells;
                for (int i = 1; i < path.Count; i++)
                {
                    var a = path[i - 1];
                    var b = path[i];
                    PaintSegment(grid, a, b, halfLow, halfHigh, outer);
                }
            }
            return grid;
        }

        static void PaintSegment(ArenaOccupancy grid, Vector2Int a, Vector2Int b, int lo, int hi, RectInt outer)
        {
            int dx = b.x - a.x, dy = b.y - a.y;
            bool horizontal = Mathf.Abs(dx) >= Mathf.Abs(dy);
            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            int sx = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
            int sy = dy == 0 ? 0 : (dy > 0 ? 1 : -1);
            int x = a.x, y = a.y;
            for (int s = 0; s <= steps; s++)
            {
                if (horizontal)
                {
                    for (int off = -lo; off <= hi; off++)
                        grid.SetCorridor(x - outer.xMin, y + off - outer.yMin);
                }
                else
                {
                    for (int off = -lo; off <= hi; off++)
                        grid.SetCorridor(x + off - outer.xMin, y - outer.yMin);
                }
                if (s == steps) break;
                x += sx; y += sy;
            }
        }
    }
}
