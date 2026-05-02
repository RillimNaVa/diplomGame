using System;
using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.ProceduralArena.Encounter
{
    /// <summary>
    /// Per-arena orchestrator. Holds spawn points, clear condition,
    /// soft-lock barriers, and enemy-death accounting. Dispatches
    /// BeginEncounter to GameManager (encounter mode) when the player
    /// steps inside, and raises Cleared once the condition is met.
    /// </summary>
    public class EncounterController : MonoBehaviour
    {
        public ClearCondition clearCondition = ClearCondition.KillAll;
        public int enemyCount = 8;
        public float enemyHealthMultiplier = 1f;

        // PR 3.D — Spawn composition.
        // arenaIndex is set by ArenaFlowController.SetupEncounter from the
        // run-graph node and is the single source of truth for composer input
        // (TZ §7.4 Revision). spawnProfile is optional — null falls back to the
        // legacy single-prefab path on GameManager.
        public int arenaIndex;
        public EnemySpawnProfile spawnProfile;

        // Phase 4 / PR 4.PD — Elite modifier wired from ArenaTypeProfile.eliteModifier.
        // When non-null, multiplies budget + per-enemy HP and prepends guaranteed enemies.
        public EliteEncounterModifier eliteModifier;

        public readonly List<Transform> spawnPoints = new List<Transform>();
        public readonly List<SoftLockBarrier> barriers = new List<SoftLockBarrier>();

        public event Action Cleared;

        // Phase 4 / PR 4.PC — reward gate. When a listener (RunProgressionController)
        // sets HoldBarriers=true during the Cleared event, FinishCleared defers
        // the barrier.Open() calls until OpenBarriers() is invoked explicitly.
        public bool HoldBarriers { get; set; }
        bool barriersOpened;

        enum State { Idle, Active, Done }
        State state = State.Idle;
        int kills;

        public bool IsCleared => state == State.Done;

        void Start()
        {
            // Lock barriers up-front for conditions that require clearing.
            bool lockBarriers = clearCondition != ClearCondition.None;
            for (int i = 0; i < barriers.Count; i++)
            {
                if (barriers[i] == null) continue;
                if (lockBarriers) barriers[i].Close();
                else barriers[i].Open();
            }

            // For arenas with no clear condition (Start / Shop / Rest) the exit is
            // immediately available; no BeginEncounter wait needed.
            if (clearCondition == ClearCondition.None)
            {
                state = State.Done;
                Cleared?.Invoke();
            }
        }

        public void BeginEncounter()
        {
            if (state != State.Idle) return;
            state = State.Active;

            switch (clearCondition)
            {
                case ClearCondition.KillAll:
                    SpawnEnemiesViaGameManager();
                    break;
                case ClearCondition.ReachExit:
                    // Exit trigger (ExitDoorTrigger) will call FinishByReach directly.
                    break;
                case ClearCondition.Timer:
                    // Deferred to PR 2.D.
                    FinishCleared();
                    break;
            }
        }

        void SpawnEnemiesViaGameManager()
        {
            var gm = GameManager.instance;
            if (gm == null)
            {
                Debug.LogWarning("[Encounter] No GameManager — cannot spawn enemies, auto-clearing.");
                FinishCleared();
                return;
            }

            Transform[] pts = spawnPoints.Count > 0 ? spawnPoints.ToArray() : null;

            // PR 3.D — when a spawn profile is assigned, run the composer and
            // pass the resolved roster. Composer handles its own LogWarning on
            // fallback, so we just check UsedFallback to decide whether to call
            // the legacy single-prefab path.
            if (spawnProfile != null)
            {
                float budgetMul = eliteModifier != null ? eliteModifier.budgetMultiplier : 1f;
                float hpMul = enemyHealthMultiplier * (eliteModifier != null ? eliteModifier.enemyHpMultiplier : 1f);
                var result = EnemySpawnComposer.Compose(spawnProfile, arenaIndex, null, budgetMul);
                if (!result.UsedFallback && result.Roster.Count > 0)
                {
                    // PR 4.PD — prepend Elite-guaranteed enemies before composer roster
                    // so they spawn first regardless of weighted picks.
                    if (eliteModifier != null && eliteModifier.guaranteedEnemies != null)
                    {
                        var prepended = new List<EnemySpawnEntry>(result.Roster.Count + eliteModifier.guaranteedEnemies.Length);
                        for (int i = 0; i < eliteModifier.guaranteedEnemies.Length; i++)
                        {
                            var entry = eliteModifier.guaranteedEnemies[i];
                            if (entry == null || !entry.IsValid) continue;
                            prepended.Add(entry);
                        }
                        prepended.AddRange(result.Roster);
                        result.Roster = prepended;
                    }
                    gm.BeginEncounter(result.Roster, pts, OnEnemyKilled, hpMul);
                    enemyCount = result.Roster.Count;
                    return;
                }
            }

            gm.BeginEncounter(enemyCount, pts, OnEnemyKilled, enemyHealthMultiplier);
        }

        void OnEnemyKilled()
        {
            if (state != State.Active) return;
            kills++;
            if (kills >= enemyCount) FinishCleared();
        }

        public void FinishByReach()
        {
            if (state == State.Done) return;
            FinishCleared();
        }

        void FinishCleared()
        {
            state = State.Done;
            var gm = GameManager.instance;
            if (gm != null) gm.EndEncounter();
            // Cleared first so listeners can set HoldBarriers=true synchronously.
            Cleared?.Invoke();
            if (!HoldBarriers) OpenBarriers();
        }

        /// <summary>
        /// Idempotent — safe to call multiple times. RunProgressionController
        /// calls this once a reward card is applied (or the player skips).
        /// </summary>
        public void OpenBarriers()
        {
            if (barriersOpened) return;
            barriersOpened = true;
            for (int i = 0; i < barriers.Count; i++)
                if (barriers[i] != null) barriers[i].Open();
        }
    }
}
