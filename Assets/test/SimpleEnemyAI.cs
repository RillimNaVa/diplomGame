using UnityEngine;
using UnityEngine.AI;

// Legacy enemy brain. Kept as a Phase 3 compatibility wrapper so the existing
// Enemy.prefab continues to work until it is migrated to MeleeEnemyBrain +
// EnemyData (see docs/ENEMY_AI_TZ.md §5.8 / PR 3.A handoff). Implements
// IEnemyTargetReceiver so GameManager talks to the interface only.
//
// Behavior preserved from prototype, with two PR 3.A acceptance fixes:
// - SetDestination is throttled, not called every frame;
// - per-attack Debug.Log spam removed.
[RequireComponent(typeof(NavMeshAgent))]
public class SimpleEnemyAI : MonoBehaviour, IEnemyTargetReceiver
{
    [SerializeField] private Transform player;

    public float damage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    [Tooltip("Seconds between SetDestination updates. 0 means every frame (legacy behavior — avoid).")]
    public float pathUpdateInterval = 0.2f;

    private NavMeshAgent agent;
    private float lastAttackTime;
    private float nextPathUpdateTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject taggedPlayer = GameObject.FindWithTag("Player");
            player = taggedPlayer != null ? taggedPlayer.transform : null;
        }

        if (player == null)
        {
            Debug.LogError("Player transform is not assigned for enemy AI.");
        }
    }

    void Update()
    {
        if (player == null || !gameObject.activeInHierarchy) return;

        // PR 2.C: when arenas bake their NavMesh async on fade-in, an enemy
        // spawned during the bake frame may not yet be on a NavMesh. Guard
        // against the "SetDestination on inactive agent" warning/exception.
        if (agent != null && agent.isOnNavMesh && Time.time >= nextPathUpdateTime)
        {
            agent.SetDestination(player.position);
            nextPathUpdateTime = Time.time + pathUpdateInterval;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < attackRange && Time.time > lastAttackTime)
        {
            Attack();
        }
    }

    void Attack()
    {
        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        lastAttackTime = Time.time + attackCooldown;
    }

    public void SetTarget(Transform target)
    {
        player = target;
    }
}
