using System.Collections.Generic;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Layout
{
    public class ArenaLayout
    {
        public int seed;
        public RectInt outerBoundsCells;
        public BspNode bspRoot;
        public readonly List<ArenaRoomData> rooms = new List<ArenaRoomData>();
        public readonly List<ArenaCorridorData> corridors = new List<ArenaCorridorData>();
        public int startRoomId = -1;
        public int exitRoomId = -1;
    }
}
