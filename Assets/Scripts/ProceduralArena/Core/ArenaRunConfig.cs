using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Core
{
    [CreateAssetMenu(fileName = "ArenaRunConfig", menuName = "VoidSurvivor/Arena/Run Config")]
    public class ArenaRunConfig : ScriptableObject
    {
        [Header("Seed")]
        [Tooltip("0 = use time-based random seed; non-zero = deterministic.")]
        public int seed = 0;

        [Header("Grid")]
        public float macroCellMeters = 4f;
        public float microCellMeters = 1f;

        [Header("Map bounds (macro cells)")]
        [Min(10)] public int mapWidthCells = 24;
        [Min(10)] public int mapHeightCells = 24;

        [Header("BSP")]
        [Min(1)] public int bspMaxDepth = 4;
        [Min(4)] public int bspMinLeafCells = 7;
        [Range(0f, 0.5f)] public float bspSplitJitter = 0.25f;

        [Header("Rooms (macro cells)")]
        [Min(3)] public int roomMinCells = 4;
        [Min(3)] public int roomMaxCells = 11;
        [Min(0)] public int roomPaddingCells = 1;
        [Range(2, 12)] public int targetRoomCount = 5;

        [Header("Corridors")]
        [Min(1)] public int corridorWidthCells = 2;
        [Range(0f, 1f)] public float extraCorridorChance = 0.25f;

        [Header("Fallback")]
        [Min(1)] public int maxGenerationAttempts = 3;
    }
}
