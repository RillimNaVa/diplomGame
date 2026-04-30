using UnityEngine;
using UnityEngine.AI;

// Phase 4 / PR 4.B — Crawler differentiation. Adds a leap-attack behavior on
// top of the regular melee chase loop. State flow:
//
//   Move:
//     - dist > leapMaxRange:                     walk closer (NavMesh)
//     - leapMinRange .. leapMaxRange, ready:     enter Telegraph (leap mode)
//     - dist <= attackRange:                     enter Telegraph (regular melee)
//   Telegraph (leap):  crouch + telegraph flash for leapTelegraphTime, snapshot
//                       launch direction at the end of the wind-up.
//   Attack (leap):      kinematic translate toward the snapshot at leapSpeed
//                       for leapTravelTime. NavMeshAgent is disabled for the
//                       duration. On finish, deal damage if player is in range.
//   Recover:           re-enable agent, eat the regular attackCooldown plus a
//                       longer leapCooldown gate.
//
// Same EnemyData-driven damage / range as the standard melee brain — we don't
// add new SO fields; leap tuning lives on the brain component (Crawler is the
// only enemy that uses it). Slot manager + TelegraphFlash + HitFlash all
// behave identically to other brains.
public class LeapMeleeEnemyBrain : EnemyBrainBase
{
    [Header("Leap Attack")]
    [Tooltip("Below this distance the brain falls through to a regular melee swing instead of leaping.")]
    public float leapMinRange = 4f;
    [Tooltip("Above this distance the brain just walks closer.")]
    public float leapMaxRange = 8f;
    [Tooltip("Wind-up before the leap launches. Slightly longer than regular melee telegraph so the player has time to read it.")]
    public float leapTelegraphTime = 0.45f;
    [Tooltip("Kinematic travel speed in m/s during the leap.")]
    public float leapSpeed = 14f;
    [Tooltip("Maximum travel time in seconds. The leap may end earlier if it reaches the snapshot landing point.")]
    public float leapTravelTime = 0.5f;
    [Tooltip("Damage radius around the Crawler's body at the end of the leap. Should be slightly bigger than attackRange so the leap reads as an arc-attack.")]
    public float leapImpactRadius = 1.9f;
    [Tooltip("Extra cooldown applied after a leap on top of the regular attackCooldown.")]
    public float leapCooldown = 1.6f;
    [Tooltip("Vertical offset added to the snapshot landing point so the Crawler arcs slightly. 0 = pure horizontal lunge.")]
    public float leapArcHeight = 0.6f;

    enum LeapPhase { None, Telegraph, Travel }

    LeapPhase leapPhase = LeapPhase.None;
    Vector3 leapStart;
    Vector3 leapTarget;
    float leapPhaseStart;
    float attackReadyTime;
    float leapReadyTime;
    bool agentDisabledForLeap;

    protected override void TickBrain()
    {
        switch (State)
        {
            case EnemyAIState.Move:       TickMove();      break;
            case EnemyAIState.Telegraph:  TickTelegraph(); break;
            case EnemyAIState.Attack:     TickAttack();    break;
            case EnemyAIState.Recover:    TickRecover();   break;
        }
    }

    void TickMove()
    {
        float dist = DistanceToTarget();

        // Inside attackRange: regular melee swing path.
        if (dist <= data.attackRange && Time.time >= attackReadyTime && TargetIsAlive())
        {
            if (!TryAcquireAttackSlot()) { RequestPathTo(Target.position); return; }
            StopAgent();
            leapPhase = LeapPhase.None;
            SetState(EnemyAIState.Telegraph);
            BeginTelegraphFlash();
            return;
        }

        // Inside leap window: telegraph leap.
        if (dist >= leapMinRange && dist <= leapMaxRange
            && Time.time >= leapReadyTime
            && Time.time >= attackReadyTime
            && TargetIsAlive())
        {
            if (!TryAcquireAttackSlot()) { RequestPathTo(Target.position); return; }
            StopAgent();
            leapPhase = LeapPhase.Telegraph;
            leapPhaseStart = Time.time;
            SetState(EnemyAIState.Telegraph);
            BeginTelegraphFlash();
            return;
        }

        // Otherwise just chase.
        RequestPathTo(Target.position);
    }

    void TickTelegraph()
    {
        if (leapPhase == LeapPhase.Telegraph)
        {
            FaceTarget();
            float wind = Mathf.Max(0.05f, leapTelegraphTime);
            if (Time.time - leapPhaseStart < wind) return;

            // Snapshot launch — direction toward the target at the moment the
            // leap commits. Player can still dodge by moving perpendicular
            // during travel; that's the readability contract.
            leapStart = transform.position;
            leapTarget = Target.position;
            leapPhaseStart = Time.time;
            leapPhase = LeapPhase.Travel;
            SetState(EnemyAIState.Attack);
            // Disable NavMeshAgent for the kinematic phase so SetDestination
            // doesn't fight our manual transform writes.
            if (agent != null && agent.enabled)
            {
                agentDisabledForLeap = true;
                agent.enabled = false;
            }
            return;
        }

        // Regular melee telegraph fall-through.
        if (TimeInState < data.telegraphTime) return;
        SetState(EnemyAIState.Attack);
    }

    void TickAttack()
    {
        if (leapPhase == LeapPhase.Travel)
        {
            float t = (Time.time - leapPhaseStart) / Mathf.Max(0.05f, leapTravelTime);
            if (t >= 1f)
            {
                ResolveLeapImpact();
                FinishLeap();
                return;
            }

            // Parabolic-ish arc: linear horizontal + sin vertical.
            Vector3 horiz = Vector3.Lerp(leapStart, leapTarget, t);
            horiz.y = Mathf.Lerp(leapStart.y, leapTarget.y, t) + Mathf.Sin(t * Mathf.PI) * leapArcHeight;
            // Cap distance per frame by leapSpeed so a long lerp doesn't teleport.
            Vector3 next = Vector3.MoveTowards(transform.position, horiz, leapSpeed * Time.deltaTime);
            transform.position = next;

            // Early-out if we hit the player before the timer expires.
            if (DistanceToTarget() <= leapImpactRadius * 0.6f && TargetIsAlive())
            {
                ResolveLeapImpact();
                FinishLeap();
            }
            return;
        }

        // Regular melee attack frame.
        if (TargetIsAlive() && DistanceToTarget() <= data.attackRange)
        {
            if (targetHealth != null) targetHealth.TakeDamage(data.damage, transform.position);
        }
        EndTelegraphFlash();
        SetState(EnemyAIState.Recover);
    }

    void TickRecover()
    {
        float wait = leapPhase == LeapPhase.None ? data.recoveryTime : data.recoveryTime + 0.15f;
        if (TimeInState < wait) return;

        attackReadyTime = Time.time + data.attackCooldown;
        if (leapPhase != LeapPhase.None) leapReadyTime = Time.time + leapCooldown;
        leapPhase = LeapPhase.None;
        ReleaseAttackSlot();
        SetState(EnemyAIState.Move);
    }

    void ResolveLeapImpact()
    {
        if (!TargetIsAlive() || targetHealth == null) return;
        if (DistanceToTarget() <= leapImpactRadius)
        {
            targetHealth.TakeDamage(data.damage, transform.position);
        }
    }

    void FinishLeap()
    {
        EndTelegraphFlash();
        if (agentDisabledForLeap && agent != null)
        {
            agent.enabled = true;
            // Warp so the agent picks up wherever we landed; ResetPath clears
            // the stale destination from before the leap.
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.Warp(transform.position);
            agentDisabledForLeap = false;
        }
        SetState(EnemyAIState.Recover);
    }

    void FaceTarget()
    {
        if (Target == null) return;
        Vector3 flat = Target.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(flat);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, 14f * Time.deltaTime);
    }

    protected override void HandleStaggerChanged(bool isStaggered)
    {
        if (isStaggered && agentDisabledForLeap && agent != null)
        {
            // Stagger mid-leap: snap the agent back on so the brain can stop properly.
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.Warp(transform.position);
            agentDisabledForLeap = false;
            leapPhase = LeapPhase.None;
        }
        base.HandleStaggerChanged(isStaggered);
    }

    protected override void HandleDeath()
    {
        if (agentDisabledForLeap && agent != null)
        {
            agent.enabled = true;
            agentDisabledForLeap = false;
        }
        leapPhase = LeapPhase.None;
        base.HandleDeath();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, leapMinRange);
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.18f);
        Gizmos.DrawWireSphere(transform.position, leapMaxRange);
    }
#endif
}
