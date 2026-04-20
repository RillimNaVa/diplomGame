using System.Collections.Generic;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Layout
{
    public class ArenaCorridorData
    {
        public int roomAId;
        public int roomBId;
        public int widthCells;
        public readonly List<Vector2Int> pathCells = new List<Vector2Int>();
    }
}
