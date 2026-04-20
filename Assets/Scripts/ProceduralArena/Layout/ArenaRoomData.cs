using System.Collections.Generic;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Layout
{
    public enum RoomType
    {
        Start,
        CombatSmall,
        CombatMedium,
        CombatLarge,
        Transition,
        Exit
    }

    public class ArenaRoomData
    {
        public int id;
        public RectInt boundsCells;
        public RoomType type = RoomType.CombatMedium;
        public readonly List<Vector2Int> doorAnchorsCells = new List<Vector2Int>();
        public readonly List<int> connectedRoomIds = new List<int>();

        public Vector2Int CenterCell =>
            new Vector2Int(
                boundsCells.xMin + boundsCells.width / 2,
                boundsCells.yMin + boundsCells.height / 2);

        public int AreaCells => boundsCells.width * boundsCells.height;
    }
}
