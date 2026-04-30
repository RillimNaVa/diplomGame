using UnityEngine;

// Phase 3 / PR 3.B — Plasma Spitter / Sentinel.
// State flow:  Move (close in) -> Reposition (back off) -> Telegraph -> Attack -> Recover -> Move
// Holds preferredDistance with a hysteresis band so the agent does not jitter
// at the band edge. Refuses to enter Telegraph without a valid line-of-sight
// (raycast from muzzleOffset to target). Spawns EnemyProjectile at impact —
// projectile carries owner reference, damage, speed.
// See docs/ENEMY_AI_TZ.md §5.7, §6.3.
public class RangedEnemyBrain : EnemyBrainBase
{
    [Header("Ranged")]
    [Tooltip("Local-space muzzle offset. Y is added to transform.position; default 1.0 puts the muzzle near head height for the standard 1x2x1 enemy capsule.")]
    public Vector3 muzzleOffset = new Vector3(0f, 1.0f, 0f);
    [Tooltip("Hysteresis around preferredDistance. Agent moves in if dist > pref + band, backs off if dist < pref - band.")]
    public float distanceBand = 1.5f;
    [Tooltip("Layers that block line-of-sight. Default Everything except enemy layer; set to walls + props in Editor.")]
    public LayerMask losBlockMask = ~0;

    [Header("Strafing (PR 3.G)")]
    [Tooltip("Lateral distance the brain holds while inside the firing band. 0 disables strafing.")]
    public float strafeRadius = 3.5f;
    [Tooltip("Average seconds before the strafe direction flips. Hash-based jitter so a group of Spitters does not strafe in lockstep.")]
    public float strafeFlipInterval = 2.2f;

    float attackReadyTime;
    float nextLosCheckTime;
    bool losCached;
    SpitterChargeBeam chargeBeam;
    int strafeDir;            // -1 left, +1 right, 0 not yet picked
    float nextStrafeFlipTime;

    protected override void Awake()
    {
        base.Awake();
        // PR 5.A — auto-attach the charge beam visual. Only ranged brains get
        // it, since melee/Brute don't have a "windup laser pointer" moment.
        chargeBeam = GetComponent<SpitterChargeBeam>();
        if (chargeBeam == null) chargeBeam = gameObject.AddComponent<SpitterChargeBeam>();
        chargeBeam.muzzleOffset = muzzleOffset;
    }

    protected override void TickBrain()
    {
        switch (State)
        {
            case EnemyAIState.Move:        TickMove();        break;
            case EnemyAIState.Reposition:  TickReposition();  break;
            case EnemyAIState.Telegraph:   TickTelegraph();   break;
            case EnemyAIState.Attack:      TickAttack();      break;
            case EnemyAIState.Recover:     TickRecover();     break;
        }
    }

    void TickMove()
    {
        float dist = DistanceToTarget();
        float pref = data.preferredDistance;

        if (dist > pref + distanceBand)
        {
            // Approach to preferred distance — aim for a point pref meters from
            // the target along the line target->self, so we do not run past it.
            Vector3 toSelf = (transform.position - Target.position).normalized;
            Vector3 standOff = Target.position + toSelf * pref;
            RequestPathTo(standOff);
        }
        else if (dist < pref - distanceBand)
        {
            SetState(EnemyAIState.Reposition);
            return;
        }
        else
        {
            // Inside the band — strafe sideways while facing the target so the
            // player can dodge and the Spitter does not feel like a static turret.
            FaceTarget();
            if (strafeRadius > 0.01f)
            {
                TickStrafe();
            }
            else
            {
                StopAgent();
            }

            if (Time.time >= attackReadyTime && TargetIsAlive() && CheckLineOfSight())
            {
                // PR 3.E §8.3: ranged slot gate.
                if (!TryAcquireAttackSlot()) return;
                SetState(EnemyAIState.Telegraph);
                BeginTelegraphFlash();
                // PR 5.A — visible charge beam so the player can read the shot direction.
                if (chargeBeam != null) chargeBeam.BeginCharge(Target, data.telegraphTime);
            }
        }
    }

    void TickStrafe()
    {
        if (strafeDir == 0)
        {
            // Hash-based initial direction so a wave of Spitters does not all
            // strafe the same way.
            strafeDir = ((GetInstanceID() & 1) == 0) ? -1 : 1;
            nextStrafeFlipTime = Time.time + strafeFlipInterval * (0.7f + 0.6f * UnityEngine.Random.value);
        }
        if (Time.time >= nextStrafeFlipTime)
        {
            strafeDir = -strafeDir;
            nextStrafeFlipTime = Time.time + strafeFlipInterval * (0.7f + 0.6f * UnityEngine.Random.value);
        }

        Vector3 toTarget = Target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;
        Vector3 right = Vector3.Cross(Vector3.up, toTarget.normalized);
        // Standoff at preferredDistance, then offset sideways by strafeRadius.
        Vector3 standOff = Target.position - toTarget.normalized * data.preferredDistance;
        Vector3 destination = standOff + right * strafeDir * strafeRadius;
        RequestPathTo(destination);
    }

    void TickReposition()
    {
        // Back off in the direction away from the target until we exit the
        // close band, then return to Move.
        Vector3 awayDir = (transform.position - Target.position).normalized;
        Vector3 retreat = transform.position + awayDir * distanceBand;
        RequestPathTo(retreat);

        if (DistanceToTarget() >= data.preferredDistance - distanceBand)
        {
            SetState(EnemyAIState.Move);
        }
    }

    void TickTelegraph()
    {
        FaceTarget();
        if (TimeInState < data.telegraphTime) return;
        SetState(EnemyAIState.Attack);
    }

    void TickAttack()
    {
        // LoS may have broken during the telegraph (target dashed behind cover).
        // In that case eat the cooldown but skip the projectile — TZ §6.3:
        // "Spitter does not shoot through walls if line-of-sight check fails".
        if (CheckLineOfSight() && data.projectilePrefab != null)
        {
            SpawnProjectile();
        }
        EndTelegraphFlash();
        // PR 5.A — drop the charge beam at the firing frame.
        if (chargeBeam != null) chargeBeam.EndCharge();
        SetState(EnemyAIState.Recover);
    }

    void TickRecover()
    {
        if (TimeInState < data.recoveryTime) return;
        attackReadyTime = Time.time + data.attackCooldown;
        ReleaseAttackSlot();
        SetState(EnemyAIState.Move);
    }

    Vector3 MuzzleWorld()
    {
        return transform.position + transform.TransformVector(muzzleOffset);
    }

    void FaceTarget()
    {
        Vector3 flat = Target.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(flat);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, 12f * Time.deltaTime);
    }

    bool CheckLineOfSight()
    {
        if (Target == null) return false;

        // Throttle to data.lineOfSightCheckInterval; reuse last result between
        // checks. interval <= 0 means "every call".
        if (data.lineOfSightCheckInterval > 0f && Time.time < nextLosCheckTime)
        {
            return losCached;
        }
        nextLosCheckTime = Time.time + data.lineOfSightCheckInterval;

        Vector3 from = MuzzleWorld();
        // Aim slightly above target base — most player rigs have origin at feet.
        Vector3 to = Target.position + Vector3.up * 1.0f;
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.001f) { losCached = true; return true; }

        // Raycast — first hit decides. If first hit is the target itself, LoS
        // is clear. If anything else (wall/prop) blocks first, LoS is blocked.
        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, dist, losBlockMask, QueryTriggerInteraction.Ignore))
        {
            losCached = hit.transform == Target || hit.transform.IsChildOf(Target);
        }
        else
        {
            losCached = true;
        }
        return losCached;
    }

    protected override void HandleStaggerChanged(bool isStaggered)
    {
        if (isStaggered && chargeBeam != null) chargeBeam.EndCharge();
        base.HandleStaggerChanged(isStaggered);
    }

    protected override void HandleDeath()
    {
        if (chargeBeam != null) chargeBeam.EndCharge();
        base.HandleDeath();
    }

    void SpawnProjectile()
    {
        Vector3 from = MuzzleWorld();
        Vector3 aim = (Target.position + Vector3.up * 1.0f) - from;
        if (aim.sqrMagnitude < 0.0001f) return;
        Vector3 dir = aim.normalized;

        // PR 3.F: rent through the projectile pool so Spitter cadence does not
        // accumulate dead projectile instances across long runs.
        GameObject go = EnemyProjectilePool.Instance.Rent(data.projectilePrefab, from, Quaternion.LookRotation(dir));
        EnemyProjectile p = go != null ? go.GetComponent<EnemyProjectile>() : null;
        if (p != null)
        {
            p.Configure(gameObject, data.damage, data.projectileSpeed, dir);
        }
    }
}
