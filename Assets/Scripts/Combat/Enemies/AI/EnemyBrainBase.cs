using UnityEngine;
using UnityEngine.AI;

// Phase 3 / PR 3.A — shared runtime concerns for all enemy brains:
// target resolution, NavMeshAgent driving (throttled, never per frame),
// state transitions, Health/Stagger hooks, agent stop during attack/recover.
// PR 3.E adds: ActiveAttackSlotManager integration, TelegraphFlash hook,
// fair-spawn delay when spawned too close to the player.
// See docs/ENEMY_AI_TZ.md §5.6, §8.3, §8.4.
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

    [Header("Group separation (PR 3.G)")]
    [Tooltip("Radius within which other active brains push this brain sideways while moving toward the target. 0 disables.")]
    public float separationRadius = 1.6f;
    [Tooltip("Strength of the lateral push. Multiplied by (1 - dist/radius) so far neighbors have no effect.")]
    public float separationStrength = 1.4f;

    public EnemyAIState State { get; protected set; } = EnemyAIState.Spawn;
    public Transform Target { get; private set; }

    // PR 3.G — static registry of every live brain so neighbors can be queried
    // without going through Physics.OverlapSphere on a dedicated layer. Cheap
    // for the small enemy counts we ship (max 16 melee + 4 ranged + 1 brute).
    static readonly System.Collections.Generic.List<EnemyBrainBase> ActiveBrains
        = new System.Collections.Generic.List<EnemyBrainBase>();

    protected NavMeshAgent agent;
    protected Health health;
    protected Health targetHealth;
    protected EnemyStagger stagger;
    protected TelegraphFlash telegraphFlash;

    float nextPathUpdateTime;
    float stateEnterTime;
    float spawnHoldUntil;
    bool spawnInitialized;   // PR 3.F — re-evaluated per OnEnable cycle so pooled rents redo fair-spawn

    protected AttackSlotKind SlotKind { get; private set; } = AttackSlotKind.Melee;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        stagger = GetComponent<EnemyStagger>();
        telegraphFlash = GetComponentInChildren<TelegraphFlash>(true);
        if (telegraphFlash == null) telegraphFlash = gameObject.AddComponent<TelegraphFlash>();
        // PR 4.A — auto-attach combat-feel components so existing prefabs get
        // hit flash / death burst / spawn warp-in without Editor work.
        if (GetComponent<HitFlash>() == null) gameObject.AddComponent<HitFlash>();
        if (GetComponent<EnemyDeathBurst>() == null) gameObject.AddComponent<EnemyDeathBurst>();
        if (GetComponent<SpawnWarpIn>() == null) gameObject.AddComponent<SpawnWarpIn>();
        // PR 5.A — shader-driven death + stagger visuals.
        if (GetComponent<EnemyDissolve>() == null) gameObject.AddComponent<EnemyDissolve>();
        if (GetComponent<StaggerOutline>() == null) gameObject.AddComponent<StaggerOutline>();
        // PR 3.G — physics shards on death.
        if (GetComponent<EnemyDeathShards>() == null) gameObject.AddComponent<EnemyDeathShards>();

        if (data != null)
        {
            health.maxHealth = data.maxHealth;
            health.currentHealth = data.maxHealth;
            agent.speed = data.moveSpeed;
            SlotKind = ActiveAttackSlotManager.KindForRole(data.role);
        }
    }

    protected virtual void OnEnable()
    {
        health.onDeath.AddListener(HandleDeath);
        if (stagger != null) stagger.OnStaggerChanged += HandleStaggerChanged;
        // PR 3.F: re-arm spawn init so a pooled rent re-evaluates fair-spawn.
        // Start only runs once per instance lifetime; pool reuse re-fires
        // OnEnable but not Start.
        spawnInitialized = false;
        SetState(EnemyAIState.Spawn);
        if (!ActiveBrains.Contains(this)) ActiveBrains.Add(this);
    }

    protected virtual void OnDisable()
    {
        health.onDeath.RemoveListener(HandleDeath);
        if (stagger != null) stagger.OnStaggerChanged -= HandleStaggerChanged;
        ReleaseAttackSlot();
        if (telegraphFlash != null) telegraphFlash.EndPulse();
        ActiveBrains.Remove(this);
    }

    protected virtual void Start()
    {
        if (Target == null && autoResolvePlayer)
        {
            GameObject tagged = GameObject.FindWithTag("Player");
            if (tagged != null) SetTarget(tagged.transform);
        }
        // Fair-spawn evaluation now lives in InitializeSpawn(), invoked from
        // Update() on the first frame after each OnEnable. That way pooled
        // rents (which fire OnEnable but not Start) also pick up the delay.
    }

    /// <summary>
    /// PR 3.E §8.4: fair-spawn delay when too close to player. Hold in Spawn,
    /// pulse telegraph briefly so the player can react before damage starts.
    /// Called once per OnEnable cycle as soon as Target is resolved.
    /// </summary>
    void InitializeSpawn()
    {
        spawnInitialized = true;
        float delay = ResolveFairSpawnDelay();
        if (delay > 0f)
        {
            spawnHoldUntil = Time.time + delay;
            if (telegraphFlash != null && data != null)
            {
                telegraphFlash.BeginPulse(delay, data.telegraphColor);
            }
        }
        else
        {
            SetState(EnemyAIState.Move);
        }
    }

    public virtual void SetTarget(Transform target)
    {
        Target = target;
        if (target != null) targetHealth = target.GetComponent<Health>();
    }

    protected virtual void Update()
    {
        if (State == EnemyAIState.Dead || State == EnemyAIState.Staggered) return;

        if (State == EnemyAIState.Spawn)
        {
            if (!spawnInitialized)
            {
                // Wait for Target to be resolved (autoResolve in Start, or
                // GameManager.SetTarget right after Rent) before evaluating
                // the fair-spawn distance.
                if (Target == null) return;
                InitializeSpawn();
                return;
            }
            if (Time.time >= spawnHoldUntil)
            {
                if (telegraphFlash != null) telegraphFlash.EndPulse();
                SetState(EnemyAIState.Move);
            }
            return;
        }

        if (Target == null || data == null) return;

        TickBrain();
    }

    protected abstract void TickBrain();

    /// <summary>
    /// Throttled SetDestination. Skips the call when the agent is not yet on the
    /// NavMesh (PR 2.C async-bake guard). Applies group-separation offset so
    /// brains do not stack on top of each other when chasing the player.
    /// </summary>
    protected void RequestPathTo(Vector3 worldPos)
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (Time.time < nextPathUpdateTime) return;
        nextPathUpdateTime = Time.time + pathUpdateInterval;
        agent.SetDestination(ApplySeparation(worldPos));
    }

    /// <summary>
    /// PR 3.G — group coordination. Pushes the requested destination sideways
    /// based on nearby active brains so a Crawler swarm fans out around the
    /// player instead of stacking on the same point. Returns the adjusted pos.
    /// </summary>
    protected Vector3 ApplySeparation(Vector3 desired)
    {
        if (separationRadius <= 0.001f || separationStrength <= 0.001f) return desired;
        Vector3 push = Vector3.zero;
        Vector3 self = transform.position;
        for (int i = 0; i < ActiveBrains.Count; i++)
        {
            var other = ActiveBrains[i];
            if (other == null || other == this) continue;
            Vector3 d = self - other.transform.position;
            d.y = 0f;
            float distSqr = d.sqrMagnitude;
            if (distSqr < 0.0001f || distSqr > separationRadius * separationRadius) continue;
            float dist = Mathf.Sqrt(distSqr);
            // Linear falloff: full push at touch distance, zero at radius edge.
            float w = 1f - dist / separationRadius;
            push += d.normalized * w;
        }
        if (push.sqrMagnitude < 0.0001f) return desired;
        return desired + push.normalized * separationStrength;
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

    // ----- PR 3.E: attack-slot helpers (shared by every brain) -----

    /// <summary>
    /// Gate before transitioning Move -> Telegraph. Returns true if the slot was
    /// granted (or already held by this brain). Returning false means the brain
    /// should keep moving/repositioning until a slot frees up. Idempotent.
    /// </summary>
    protected bool TryAcquireAttackSlot()
    {
        return ActiveAttackSlotManager.Instance.TryAcquire(this, SlotKind);
    }

    protected void ReleaseAttackSlot()
    {
        // Avoid auto-creating a manager during teardown if one was never built.
        if (!Application.isPlaying) return;
        var inst =
#if UNITY_2023_1_OR_NEWER
            Object.FindFirstObjectByType<ActiveAttackSlotManager>();
#else
            Object.FindObjectOfType<ActiveAttackSlotManager>();
#endif
        if (inst != null) inst.Release(this);
    }

    /// <summary>
    /// Brains call this when entering Telegraph. Pulses the telegraph emission
    /// for the brain's telegraphTime in the EnemyData-defined color.
    /// </summary>
    protected void BeginTelegraphFlash()
    {
        if (telegraphFlash == null || data == null) return;
        telegraphFlash.BeginPulse(data.telegraphTime, data.telegraphColor);
    }

    protected void EndTelegraphFlash()
    {
        if (telegraphFlash != null) telegraphFlash.EndPulse();
    }

    float ResolveFairSpawnDelay()
    {
        if (data == null) return 0f;
        if (data.fairSpawnDistance <= 0f || data.fairSpawnDelay <= 0f) return 0f;
        if (Target == null) return 0f;
        float d = DistanceToTarget();
        return d <= data.fairSpawnDistance ? data.fairSpawnDelay : 0f;
    }

    protected virtual void HandleStaggerChanged(bool isStaggered)
    {
        // ENEMY_AI_TZ §5.6: stagger aborts in-progress telegraph/attack,
        // releases attack slot (PR 3.E), stops the agent.
        if (isStaggered && State != EnemyAIState.Dead)
        {
            StopAgent();
            ReleaseAttackSlot();
            EndTelegraphFlash();
            SetState(EnemyAIState.Staggered);
        }
    }

    protected virtual void HandleDeath()
    {
        StopAgent();
        ReleaseAttackSlot();
        EndTelegraphFlash();
        SetState(EnemyAIState.Dead);
    }
}
