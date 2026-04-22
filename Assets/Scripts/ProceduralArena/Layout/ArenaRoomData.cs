using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;

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

        // ---- r4 single-arena extensions (unused by legacy BSP path) ----
        public ArenaCategory category = ArenaCategory.Combat;
        public ArenaShape shape = ArenaShape.Rect;
        /// <summary>Interior mask in bounds-local coordinates [0..width, 0..height). null = full rect.</summary>
        public bool[,] shapeMask;
        /// <summary>Per-arena ceiling height in meters. 0 = fall back to ArenaRunConfig.wallHeightMeters.</summary>
        public float wallHeightMeters;
        public readonly List<CoverPlacement> coverPlacements = new List<CoverPlacement>();
        public readonly List<ExitDoorAnchor> exitDoorAnchors = new List<ExitDoorAnchor>();
        public Vector3 startSpawnPoint;
        public readonly List<Vector3> combatSpawnPoints = new List<Vector3>();
        public readonly List<PlatformPlacement> platformPlacements = new List<PlatformPlacement>();
        public readonly List<RampPlacement> rampPlacements = new List<RampPlacement>();
        public string biomeId;
        public Color biomeDebugTint = Color.white;
        public int arenaIndex;
        public int scaledEnemyCount;
        public float enemyHealthMultiplier = 1f;
        // ----------------------------------------------------------------

        public Vector2Int CenterCell =>
            new Vector2Int(
                boundsCells.xMin + boundsCells.width / 2,
                boundsCells.yMin + boundsCells.height / 2);

        public int AreaCells => boundsCells.width * boundsCells.height;
    }
}
