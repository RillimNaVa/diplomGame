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
        [Tooltip("Total arena width in macro cells. 1 cell = macroCellMeters. 30 cells × 4m = 120m.")]
        [Min(10)] public int mapWidthCells = 30;
        [Tooltip("Total arena height in macro cells.")]
        [Min(10)] public int mapHeightCells = 30;

        [Header("BSP")]
        [Tooltip("Deeper splits → more leaves → more rooms. Main knob for room count.")]
        [Min(1)] public int bspMaxDepth = 5;
        [Tooltip("Minimum leaf side in cells. If leaf shrinks below this, split is rejected.")]
        [Min(4)] public int bspMinLeafCells = 8;
        [Range(0f, 0.5f)] public float bspSplitJitter = 0.25f;

        [Header("Rooms (macro cells)")]
        [Min(3)] public int roomMinCells = 5;
        [Min(3)] public int roomMaxCells = 13;
        [Min(0)] public int roomPaddingCells = 1;
        [Tooltip("Informational only — actual count is driven by BSP depth / leaf size.")]
        [Range(2, 12)] public int targetRoomCount = 6;

        [Header("Corridors")]
        [Min(1)] public int corridorWidthCells = 2;
        [Range(0f, 1f)] public float extraCorridorChance = 0.25f;

        [Header("Fallback")]
        [Min(1)] public int maxGenerationAttempts = 3;

        [Header("Build — geometry")]
        [Tooltip("Ceiling height in meters. DOOM-style double-jump reaches ~5m, set 6-8m for clearance.")]
        [Min(1f)] public float wallHeightMeters = 7f;
        [Min(0.05f)] public float wallThicknessMeters = 0.4f;
        [Min(0.05f)] public float floorThicknessMeters = 0.3f;
        [Min(0.05f)] public float ceilingThicknessMeters = 0.3f;

        [Header("Build — cover")]
        [Tooltip("Cover pillars per 10x10 macro cells of combat room floor area.")]
        [Range(0f, 4f)] public float coverDensity = 1.2f;
        [Min(0f)] public float coverMinSpacingMeters = 3f;
        [Min(0.2f)] public float coverHeightMeters = 2.2f;
        [Min(0.2f)] public float coverWidthMeters = 1.2f;

        [Header("Build — anchors")]
        public bool generateAnchors = true;
        [Min(1f)] public float ceilingAnchorSpacingMeters = 4f;
    }
}
