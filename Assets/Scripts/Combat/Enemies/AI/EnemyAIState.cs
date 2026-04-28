// Phase 3 / PR 3.A — shared state vocabulary for all enemy brains.
// See docs/ENEMY_AI_TZ.md §5.3. Not every brain must use every state.
public enum EnemyAIState
{
    Spawn,
    Move,
    Telegraph,
    Attack,
    Recover,
    Reposition,
    Staggered,
    Dead
}
