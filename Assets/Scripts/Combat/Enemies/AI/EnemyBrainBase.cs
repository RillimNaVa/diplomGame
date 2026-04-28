using UnityEngine;
using UnityEngine.AI;

// Phase 3 / PR 3.A — shared runtime concerns for all enemy brains:
// target resolution, NavMeshAgent driving (throttled, never per frame),
// state transitions, Health/Stagger hooks, agent stop during attack/recover.
// See docs/ENEMY_AI_TZ.md §5.6.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public abstract class EnemyBrainBase : MonoBehaviour, IEnemyTargetReceiver
{
    [Header("Brain")]
    [Tooltip("Tuning data for this enemy. Required.")]
    public EnemyData data;
    [Tooltip("Seconds between SetDestination calls during Move state. Avoid 0 — that re-paths every frame.")]
    public float pathUpdateInterval = 0.2f;
    [Tooltip("Auto-resolve player by tag if SetTarget is never called.")]
    public bool autoResolvePlayer = true;

    public EnemyAIState State { get; protected set; } = EnemyAIState.Spawn;
    public Transform Target { get; private set; }

    protected NavMeshAgent agent;
    protected Health health;
    protected Health targetHealth;
    protected EnemyStagger stagger;

    float nextPathUpdateTime;
    float stateEnterTime;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        stagger = GetComponent<EnemyStagger>();

        if (data != null)
        {
            health.maxHealth = data.maxHealth;
            health.currentHealth = data.maxHealth;
            agent.speed = data.moveSpeed;
        }
    }

    protected virtual void OnEnable()
    {
        health.onDeath.AddListener(HandleDeath);
        if (stagger != null) stagger.OnStaggerChanged += HandleStaggerChanged;
        SetState(EnemyAIState.Spawn);
    }

    protected virtual void OnDisable()
    {
        health.onDeath.RemoveListener(HandleDeath);
        if (stagger != null) stagger.OnStaggerChanged -= HandleStaggerChanged;
    }

    protected virtual void Start()
    {
        if (Target == null && autoResolvePlayer)
        {
            GameObject tagged = GameObject.FindWithTag("Player");
            if (tagged != null) SetTarget(tagged.transform);
        }

        // Leave Spawn immediately on first frame — Spawn is reserved for
        // pooled spawn-in delays and PR 3.E fair-spawn warnings.
        if (State == EnemyAIState.Spawn) SetState(EnemyAIState.Move);
    }

    public virtual void SetTarget(Transform target)
    {
        Target = target;
        if (target != null) targetHealth = target.GetComponent<Health>();
    }

    protected virtual void Update()
    {
        if (State == EnemyAIState.Dead || State == EnemyAIState.Staggered) return;
        if (Target == null || data == null) return;

        TickBrain();
    }

    protected abstract void TickBrain();

    /// <summary>
    /// Throttled SetDestination. Skips the call when the agent is not yet on the
    /// NavMesh (PR 2.C async-bake guard).
    /// </summary>
    protected void RequestPathTo(Vector3 worldPos)
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (Time.time < nextPathUpdateTime) return;
        nextPathUpdateTime = Time.time + pathUpdateInterval;
        agent.SetDestination(worldPos);
    }

    protected void StopAgent()
    {
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
    }

    protected void SetState(EnemyAIState next)
    {
        State = next;
        stateEnterTime = Time.time;
    }

    protected float TimeInState => Time.time - stateEnterTime;

    protected float DistanceToTarget()
    {
        if (Target == null) return float.PositiveInfinity;
        Vector3 d = Target.position - transform.position;
        return d.magnitude;
    }

    protected bool TargetIsAlive()
    {
        if (targetHealth == null) return Target != null;
        return targetHealth.currentHealth > 0f;
    }

    protected virtual void HandleStaggerChanged(bool isStaggered)
    {
        // ENEMY_AI_TZ §5.6: stagger aborts in-progress telegraph/attack,
        // releases attack slot (PR 3.E), stops the agent.
        if (isStaggered && State != EnemyAIState.Dead)
        {
            StopAgent();
            SetState(EnemyAIState.Staggered);
        }
    }

    protected virtual void HandleDeath()
    {
        StopAgent();
        SetState(EnemyAIState.Dead);
    }
}
