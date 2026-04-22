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

        [Header("Mid-stage pools (weighted picks per stage)")]
        [Tooltip("Candidates for Mid1 (after Start).")]
        public ArenaTypeProfile[] mid1Pool;
        [Tooltip("Candidates for Mid2 (after Mid1 choice).")]
        public ArenaTypeProfile[] mid2Pool;
        [Tooltip("Candidates for Mid3 (last before Boss — combat-heavy).")]
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
