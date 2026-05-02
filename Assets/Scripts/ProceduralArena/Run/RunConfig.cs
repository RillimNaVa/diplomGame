using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.ProceduralArena.Run
{
    [CreateAssetMenu(fileName = "RunConfig", menuName = "VoidSurvivor/Arena/Run Config (r4)")]
    public class RunConfig : ScriptableObject
    {
        [Header("Seed")]
        [Tooltip("0 = time-based; non-zero = deterministic graph + arenas.")]
        public int runSeed = 0;

        [Header("Anchor profiles")]
        public ArenaTypeProfile startProfile;
        public ArenaTypeProfile bossProfile;

        [Header("PR 4.PD category pools (10-stage run)")]
        [Tooltip("Combat-only profiles. Used for stages 1, 2, 5 and as fallback elsewhere.")]
        public ArenaTypeProfile[] combatPool;
        [Tooltip("Elite profiles. Used for stages 3, 6 (offered alongside Combat).")]
        public ArenaTypeProfile[] elitePool;
        [Tooltip("Shop profiles. Used for wide tier (stages 4, 7, 8).")]
        public ArenaTypeProfile[] shopPool;
        [Tooltip("Rest profiles. Used for wide tier (stages 4, 7, 8).")]
        public ArenaTypeProfile[] restPool;

        [Header("[Legacy] Mid-stage pools (5-room generator, deprecated)")]
        [Tooltip("[DEPRECATED — replaced by combatPool] kept for migration only.")]
        public ArenaTypeProfile[] mid1Pool;
        [Tooltip("[DEPRECATED — replaced by combat/shop/rest pools]")]
        public ArenaTypeProfile[] mid2Pool;
        [Tooltip("[DEPRECATED — replaced by combat/elite pools]")]
        public ArenaTypeProfile[] mid3Pool;

        [Header("Transitions")]
        [Min(0f)] public float fadeInSeconds = 0.35f;
        [Min(0f)] public float fadeHoldSeconds = 0.15f;
        [Min(0f)] public float fadeOutSeconds = 0.35f;

        [Header("Lifecycle")]
        public bool autoStartOnPlay = true;
        public bool skipClearCondition = true; // PR 2.C replaces this with real encounter gating

        [Header("PR 2.D scaling")]
        [Tooltip("Additional enemy-count multiplier per arenaIndex. 0.15 = +15% each arena.")]
        [Min(0f)] public float enemyCountScalePerArena = 0.15f;
        [Tooltip("Additional enemy max-health multiplier per arenaIndex. 0.05 = +5% each arena.")]
        [Min(0f)] public float enemyHealthScalePerArena = 0.05f;
    }
}
