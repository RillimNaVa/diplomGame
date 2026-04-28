using UnityEngine;

// Phase 3 / PR 3.A — preserves the public SetTarget(Transform) contract that
// GameManager has used since the SimpleEnemyAI prototype. New brains and the
// legacy SimpleEnemyAI both implement this so GameManager talks to the
// interface only, with no fallback branch (see ENEMY_AI_TZ.md §5.4).
public interface IEnemyTargetReceiver
{
    void SetTarget(Transform target);
}
