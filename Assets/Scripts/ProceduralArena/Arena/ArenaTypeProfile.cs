using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Arena
{
    [CreateAssetMenu(fileName = "ArenaTypeProfile", menuName = "VoidSurvivor/Arena/Type Profile")]
    public class ArenaTypeProfile : ScriptableObject
    {
        [Header("Identity")]
        public ArenaCategory category = ArenaCategory.Combat;
        public ArenaSizePreset size = ArenaSizePreset.M;

        [Header("Vertical space")]
        [Tooltip("Per-arena ceiling height range in meters. 10–25 is the TZ r4 budget.")]
        public Vector2 ceilingRange = new Vector2(10f, 14f);

        [Header("Shape distribution")]
        [Tooltip("Weighted list of shapes. First non-zero entry wins if all weights are zero.")]
        public ShapeWeight[] shapes = new ShapeWeight[]
        {
            new ShapeWeight { shape = ArenaShape.Rect,    weight = 0.6f },
            new ShapeWeight { shape = ArenaShape.LShape,  weight = 0.2f },
            new ShapeWeight { shape = ArenaShape.TShape,  weight = 0.1f },
            new ShapeWeight { shape = ArenaShape.Octagon, weight = 0.1f }
        };

        [Header("Cover")]
        [Tooltip("Cover objects per 100 m² of floor area.")]
        [Range(0f, 3f)] public float coverDensity = 1.2f;
        [Tooltip("PR 2.H1 — number of hand-authored structures (bunker/sandbag line/pillar cluster/sniper nest) placed alongside the box-cover. 0 = none, 1-2 typical for M, 2-3 for L. Reserves cells before cover so box-cover fills around them.")]
        [Range(0, 5)] public int structureBudget = 2;

        [Header("Exits")]
        [Tooltip("Number of exit doors. Start/Boss usually 1; combat/elite/etc usually 2.")]
        [Range(1, 2)] public int exitCount = 2;

        [Header("Verticality")]
        public bool enableVerticality = false;
        [Tooltip("Deterministic platform count range when verticality is enabled.")]
        public Vector2Int platformCountRange = new Vector2Int(2, 4);
        [Tooltip("Platform top height range in meters above the arena floor.")]
        public Vector2 platformHeightRange = new Vector2(2.5f, 5f);
        [Tooltip("Platform footprint width/length range in macro cells.")]
        public Vector2Int platformFootprintRange = new Vector2Int(2, 3);

        [Header("Spawning")]
        [Min(0)] public int enemySpawnCount = 8;
        [Tooltip("PR 3.D — optional. When assigned, EncounterController runs EnemySpawnComposer using this profile + arenaIndex instead of spawning the legacy single GameManager.enemyPrefab. Null = legacy path.")]
        public EnemySpawnProfile spawnProfile;

        [Header("Biome")]
        public BiomeDefinition biome;

        [Header("Clear condition")]
        public ClearCondition clearCondition = ClearCondition.KillAll;
    }
}
