using System.Collections.Generic;
using UnityEngine;

// Phase 3 / PR 3.C — Station Brute / tank.
// State flow:  Move -> Telegraph -> Attack (area slam) -> Recover -> Move
// Slam damage is applied ONCE at the end of telegraphTime via Physics.OverlapSphere
// at the Brute's feet sampled at the impact frame (not at telegraph start), so the
// player can escape by leaving slamRadius during the wind-up. See ENEMY_AI_TZ §6.4.
//
// PR 3.C ships with maxAlive = 1 enforced via the Spitter/Brute EnemyData SO until
// PR 3.E lands the slot manager (TZ Revision Log v2).
public class BruteEnemyBrain : EnemyBrainBase
{
    [Header("Brute")]
    [Tooltip("Local-space offset for the slam origin. Default zero = transform position (feet for the standard enemy capsule which has its origin at the base).")]
    public Vector3 slamOriginOffset = Vector3.zero;
    [Tooltip("Draw the slam radius gizmo in the Scene view. Editor-only debug aid.")]
    public bool drawSlamGizmo = true;

    float attackReadyTime;
    BruteSlamDecal slamDecal;
    static readonly Collider[] s_overlapBuffer = new Collider[32];

    protected override void Awake()
    {
        base.Awake();
        // PR 3.E: spawn the slam-radius warning decal once. Configure with the
        // current slamRadius — Configure() handles the lazy primitive build.
        slamDecal = GetComponent<BruteSlamDecal>();
        if (slamDecal == null) slamDecal = gameObject.AddComponent<BruteSlamDecal>();
        if (data != null) slamDecal.Configure(data.slamRadius, slamOriginOffset);
    }

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
        RequestPathTo(Target.position);

        if (Time.time < attackReadyTime) return;
        if (DistanceToTarget() <= data.attackRange && TargetIsAlive())
        {
            // PR 3.E §8.3: heavy slot gate (cap 1 by default — only one Brute
            // slamming at a time even if max-alive is later raised).
            if (!TryAcquireAttackSlot()) return;
            StopAgent();
            SetState(EnemyAIState.Telegraph);
            BeginTelegraphFlash();
            if (slamDecal != null) slamDecal.Show(data.telegraphTime);
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
        Vector3 slamOrigin = transform.TransformPoint(slamOriginOffset);
        if (data.slamRadius > 0f)
        {
            ApplySlamDamage();
        }
        // PR 4.A — visible impact: expanding ring + camera shake. The ring
        // spawns regardless of whether the player is in range; readability
        // matters more than strict simulation here.
        SlamImpactRing.Spawn(slamOrigin, data.slamRadius);
        // PR 5.C — slam shake makes lamps in a wide radius flicker.
        LampFlicker.NudgeAt(slamOrigin, data.slamRadius * 2.2f);
        if (CameraShake.Instance != null)
        {
            // Trauma scaled by distance — close slam shakes hard, far slam
            // is a soft bump. Flat 0.65 cap at point-blank.
            float distToCam = Camera.main != null
                ? Vector3.Distance(slamOrigin, Camera.main.transform.position)
                : data.slamRadius;
            float falloff = Mathf.Clamp01(1f - distToCam / Mathf.Max(0.1f, data.slamRadius * 2.5f));
            CameraShake.Instance.AddTrauma(0.65f * falloff);
        }

        EndTelegraphFlash();
        if (slamDecal != null) slamDecal.Hide();
        SetState(EnemyAIState.Recover);
    }

    void TickRecover()
    {
        if (TimeInState < data.recoveryTime) return;
        attackReadyTime = Time.time + data.attackCooldown;
        ReleaseAttackSlot();
        SetState(EnemyAIState.Move);
    }

    protected override void HandleStaggerChanged(bool isStaggered)
    {
        if (isStaggered && slamDecal != null) slamDecal.Hide();
        base.HandleStaggerChanged(isStaggered);
    }

    protected override void HandleDeath()
    {
        if (slamDecal != null) slamDecal.Hide();
        base.HandleDeath();
    }

    void ApplySlamDamage()
    {
        Vector3 origin = transform.TransformPoint(slamOriginOffset);
        int count = Physics.OverlapSphereNonAlloc(
            origin, data.slamRadius, s_overlapBuffer, data.slamHitMask, QueryTriggerInteraction.Ignore);

        // Multiple colliders may belong to the same Health (player has CharacterController + child colliders),
        // so dedupe by Health reference before calling TakeDamage.
        HashSet<Health> seen = null;
        for (int i = 0; i < count; i++)
        {
            Collider col = s_overlapBuffer[i];
            if (col == null) continue;
            Health hp = col.GetComponentInParent<Health>();
            if (hp == null || hp.gameObject == gameObject) continue;
            if (seen == null) seen = new HashSet<Health>();
            if (seen.Add(hp)) hp.TakeDamage(data.damage, origin);
        }
    }

    void FaceTarget()
    {
        if (Target == null) return;
        Vector3 flat = Target.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(flat);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, 6f * Time.deltaTime);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawSlamGizmo || data == null || data.slamRadius <= 0f) return;
        Vector3 origin = transform.TransformPoint(slamOriginOffset);
        Gizmos.color = State == EnemyAIState.Telegraph
            ? new Color(1f, 0.5f, 0f, 0.35f)   // orange while winding up
            : new Color(1f, 0.2f, 0.1f, 0.20f); // red baseline
        Gizmos.DrawWireSphere(origin, data.slamRadius);
    }
#endif
}
