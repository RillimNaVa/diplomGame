using UnityEngine;

// Phase 3 / PR 3.D — encounter spawn composition profile.
// One asset per arena type / encounter difficulty bucket. Each profile lists
// candidate enemy entries, the budget curve, the per-encounter role cap, and
// bad-combination guards from ENEMY_AI_TZ §7.6.
//
// Suggested authoring: one Combat profile, one Elite profile, one Boss profile.
// Each ArenaTypeProfile points at a profile (optional — null falls back to the
// legacy single-prefab spawn path in GameManager).
[CreateAssetMenu(menuName = "Void Survivor/Enemies/Enemy Spawn Profile", fileName = "EnemySpawnProfile")]
public class EnemySpawnProfile : ScriptableObject
{
    [Header("Roster")]
    public EnemySpawnEntry[] entries;

    [Header("Budget curve")]
    [Tooltip("budget = baseBudget + arenaIndex * budgetPerArenaIndex (TZ §7.4).")]
    public int baseBudget = 8;
    [Tooltip("Linear budget growth per arena index. arenaIndex 0..4 typically.")]
    public int budgetPerArenaIndex = 3;

    [Header("Variety caps")]
    [Tooltip("Maximum number of distinct enemy types in one encounter (TZ §7.4 — keep low in early arenas, raise later).")]
    [Range(1, 5)] public int maxEnemyTypesPerEncounter = 3;

    [Header("Bad-combination guards (TZ §7.6)")]
    [Tooltip("Hard cap on Tank-role enemies (Brute) per encounter. 1 is the spec default until PR 3.E slot manager.")]
    [Min(0)] public int maxTanks = 1;
    [Tooltip("Hard cap on Ranged enemies (Spitter) per encounter.")]
    [Min(0)] public int maxRanged = 3;
}
