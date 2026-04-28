using UnityEngine;

// Phase 3 / PR 3.A — shared melee behavior for Drone (fodder) and Crawler
// (fast pressure). The two enemies share this class and differ only by their
// EnemyData asset. See docs/ENEMY_AI_TZ.md §5.7, §6.1, §6.2.
//
// State flow:  Move -> Telegraph -> Attack -> Recover -> Move
// Damage applies once at the END of telegraphTime, with a re-check of attack
// range at the impact frame so the player can still escape during wind-up.
public class MeleeEnemyBrain : EnemyBrainBase
{
    float attackReadyTime;

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
            StopAgent();
            SetState(EnemyAIState.Telegraph);
        }
    }

    void TickTelegraph()
    {
        if (TimeInState < data.telegraphTime) return;
        SetState(EnemyAIState.Attack);
    }

    void TickAttack()
    {
        // Single-shot impact: re-check range so a moving target can dodge by
        // leaving the range during the wind-up (TZ §5.6 damage contract).
        if (TargetIsAlive() && DistanceToTarget() <= data.attackRange)
        {
            if (targetHealth != null) targetHealth.TakeDamage(data.damage);
        }
        SetState(EnemyAIState.Recover);
    }

    void TickRecover()
    {
        if (TimeInState < data.recoveryTime) return;
        attackReadyTime = Time.time + data.attackCooldown;
        SetState(EnemyAIState.Move);
    }
}
