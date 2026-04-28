using System;
using UnityEngine;

// Phase 3 / PR 3.D — one row of an EnemySpawnProfile. Each entry pairs a prefab
// with the EnemyData SO that drives its brain, plus optional per-profile
// overrides for spawn-related fields.
//
// Override rule (ENEMY_AI_TZ §7.2 Revision):
//   value > 0 on the entry overrides EnemyData; value 0 inherits the SO default.
[Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public EnemyData data;

    [Tooltip("0 = inherit from EnemyData.minArenaIndex.")]
    public int minArenaIndex;
    [Tooltip("0 = inherit from EnemyData.spawnCost.")]
    public int spawnCost;
    [Tooltip("0 = inherit from EnemyData.maxAlive.")]
    public int maxAlive;
    [Tooltip("0 = inherit from EnemyData.spawnWeight.")]
    public float weight;

    public int ResolvedMinArenaIndex => minArenaIndex > 0 ? minArenaIndex : (data != null ? data.minArenaIndex : 0);
    public int ResolvedSpawnCost => spawnCost > 0 ? spawnCost : (data != null ? data.spawnCost : 1);
    public int ResolvedMaxAlive => maxAlive > 0 ? maxAlive : (data != null ? data.maxAlive : 1);
    public float ResolvedWeight => weight > 0f ? weight : (data != null ? data.spawnWeight : 1f);
    public EnemyRole Role => data != null ? data.role : EnemyRole.Fodder;

    public bool IsValid => prefab != null && data != null;
}
