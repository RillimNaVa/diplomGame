using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleEnemyAI : MonoBehaviour
{
    [SerializeField] private Transform player;

    public float damage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    private NavMeshAgent agent;
    private float lastAttackTime;

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

        agent.SetDestination(player.position);

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
            Debug.Log($"Enemy attacked player! Damage: {damage}");
        }

        lastAttackTime = Time.time + attackCooldown;
    }

    public void SetTarget(Transform target)
    {
        player = target;
    }
}
