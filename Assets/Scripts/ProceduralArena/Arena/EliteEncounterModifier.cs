using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Arena
{
    /// <summary>
    /// Phase 4 / PR 4.PD — multiplier block applied on top of an Elite arena's
    /// base spawn profile (TZ §5.3). Stays as a separate SO so designers can
    /// retune Elite difficulty without touching the base composition.
    /// </summary>
    [CreateAssetMenu(menuName = "VoidSurvivor/Arena/Elite Encounter Modifier", fileName = "EliteEncounterModifier")]
    public class EliteEncounterModifier : ScriptableObject
    {
        [Header("Composition")]
        [Tooltip("Multiplier applied to the resolved enemy budget before composer picks roles.")]
        [Min(0.1f)] public float budgetMultiplier = 1.35f;
        [Tooltip("Optional list of enemies forced to spawn first (e.g. one Brute + one Spitter). Each entry pairs a prefab with its EnemyData SO, mirroring EnemySpawnProfile rows. Counted against budget.")]
        public EnemySpawnEntry[] guaranteedEnemies;
        [Tooltip("Reserved for spawn-tempo / wave-pacing tuning when GameManager exposes it. Currently informative.")]
        [Min(0.5f)] public float spawnTempoMultiplier = 1.15f;

        [Header("Stats")]
        [Tooltip("Multiplier applied to per-enemy maxHealth at spawn time (reuses encounter HP scaling path).")]
        [Min(0.1f)] public float enemyHpMultiplier = 1.20f;
        [Tooltip("Multiplier on per-enemy damage. Defaults 1 — bump only when readability allows.")]
        [Min(0.1f)] public float enemyDamageMultiplier = 1.0f;
    }
}
