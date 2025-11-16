using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleEnemyAI : MonoBehaviour
{
    public Transform player;
    public float damage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    private NavMeshAgent agent;
    private float lastAttackTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.Find("Player")?.transform;
        if (player == null) UnityEngine.Debug.LogError("Player not found!");
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
            UnityEngine.Debug.Log($"Enemy attacked player! Damage: {damage}");
        }
        lastAttackTime = Time.time + attackCooldown;
    }
}