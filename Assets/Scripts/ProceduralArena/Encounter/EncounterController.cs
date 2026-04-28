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

        public readonly List<Transform> spawnPoints = new List<Transform>();
        public readonly List<SoftLockBarrier> barriers = new List<SoftLockBarrier>();

        public event Action Cleared;

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
                var result = EnemySpawnComposer.Compose(spawnProfile, arenaIndex);
                if (!result.UsedFallback && result.Roster.Count > 0)
                {
                    gm.BeginEncounter(result.Roster, pts, OnEnemyKilled, enemyHealthMultiplier);
                    // EncounterController's clear-on-N-kills logic uses enemyCount,
                    // so update it to match the actual roster size.
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
            for (int i = 0; i < barriers.Count; i++)
                if (barriers[i] != null) barriers[i].Open();
            var gm = GameManager.instance;
            if (gm != null) gm.EndEncounter();
            Cleared?.Invoke();
        }
    }
}
