using UnityEngine;

// Phase 3 / PR 3.A — tuning ScriptableObject for one enemy archetype.
// Stores role + numbers, no behavior code. Behavior lives in *Brain classes.
// See docs/ENEMY_AI_TZ.md §5.5.
[CreateAssetMenu(menuName = "Void Survivor/Enemies/Enemy Data", fileName = "EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyId;
    public string displayName;
    public EnemyRole role;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3.5f;
    public float damage = 10f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float telegraphTime = 0.25f;
    public float recoveryTime = 0.35f;

    [Header("Ranged (PR 3.B)")]
    [Tooltip("Null for melee. Required by RangedEnemyBrain.")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    [Tooltip("Distance the brain tries to hold from the target.")]
    public float preferredDistance = 12f;
    public float lineOfSightCheckInterval = 0.2f;

    [Header("Heavy / Brute (PR 3.C)")]
    [Tooltip("0 disables area attack. BruteEnemyBrain uses Physics.OverlapSphere at impact.")]
    public float slamRadius = 0f;
    public LayerMask slamHitMask;

    [Header("Spawn (defaults — EnemySpawnEntry may override per profile)")]
    public int spawnCost = 1;
    public int minArenaIndex = 0;
    public int maxAlive = 20;
    public float spawnWeight = 1f;
}
