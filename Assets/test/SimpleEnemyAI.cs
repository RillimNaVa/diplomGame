//using UnityEngine;
//using UnityEngine.AI;

//[RequireComponent(typeof(NavMeshAgent))]
//public class SimpleEnemyAI : MonoBehaviour
//{
//    public Transform player;
//    public float damage = 10f;
//    public float attackCooldown = 1f;

//    private NavMeshAgent agent;
//    private float lastAttackTime;

//    void Start()
//    {
//        agent = GetComponent<NavMeshAgent>();
//        player = GameObject.Find("Player").transform;
//    }

//    void Update()
//    {
//        if (player != null)
//        {
//            agent.SetDestination(player.position);
//        }

//        // Атака при близости
//        if (Vector3.Distance(transform.position, player.position) < 1.5f && Time.time > lastAttackTime)
//        {
//            Attack();
//        }
//    }

//    void Attack()
//    {
//        Health playerHealth = player.GetComponent<Health>();
//        if (playerHealth != null)
//        {
//            playerHealth.TakeDamage(damage);
//        }
//        lastAttackTime = Time.time + attackCooldown;
//    }
//}