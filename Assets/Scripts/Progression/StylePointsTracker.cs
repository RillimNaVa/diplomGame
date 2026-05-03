using System;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Encounter;

namespace VoidSurvivor.Progression
{
    /// <summary>
    /// Phase 4 / PR 4.PE — per-arena style accumulator (TZ §12.4).
    ///
    /// Auto-creating scene singleton. RunProgressionController calls
    /// <see cref="WatchEncounter"/> on every new arena to reset counters and
    /// hook event channels; on <see cref="EncounterController.Cleared"/> the
    /// final <see cref="StyleBreakdown"/> snapshot is emitted via
    /// <see cref="OnArenaFinalized"/>.
    /// </summary>
    public class StylePointsTracker : MonoBehaviour
    {
        static StylePointsTracker s_instance;

        // Per-arena state (reset on WatchEncounter).
        EncounterController watched;
        int kills;
        int bruteKills;
        int gloryKills;
        int streakBonusKills;
        bool noHit;
        bool finalized;
        float startTime;
        float expectedClearTime;
        int enemyBudget;
        Health playerHealthRef;
        KillStreakTracker streakRef;

        public int CurrentRawStyle { get; private set; }
        public bool IsTracking => watched != null && !finalized;

        /// <summary>Fires every time a counter changes during the encounter.
        /// HUD style meter subscribes to this for silent ticking (TZ §12.4 UI rule).</summary>
        public event Action<int> OnRawStyleChanged;

        /// <summary>Fires once per arena, immediately after EncounterController.Cleared.</summary>
        public event Action<StyleBreakdown> OnArenaFinalized;

        public static StylePointsTracker Instance
        {
            get
            {
                if (s_instance != null) return s_instance;
                var found =
#if UNITY_2023_1_OR_NEWER
                    UnityEngine.Object.FindFirstObjectByType<StylePointsTracker>();
#else
                    UnityEngine.Object.FindObjectOfType<StylePointsTracker>();
#endif
                if (found != null) { s_instance = found; return s_instance; }
                var go = new GameObject("StylePointsTracker");
                s_instance = go.AddComponent<StylePointsTracker>();
                return s_instance;
            }
        }

        void Awake()
        {
            if (s_instance != null && s_instance != this) { Destroy(this); return; }
            s_instance = this;
        }

        void OnEnable()
        {
            Health.AnyDeath += OnAnyDeath;
            Health.AnyDamaged += OnAnyDamaged;
            var us = UpgradeSystem.Instance;
            if (us != null) us.OnGloryKill += OnGloryKill;
        }

        void OnDisable()
        {
            Health.AnyDeath -= OnAnyDeath;
            Health.AnyDamaged -= OnAnyDamaged;
            var us = UpgradeSystem.Instance;
            if (us != null) us.OnGloryKill -= OnGloryKill;
        }

        void OnDestroy()
        {
            if (s_instance == this) s_instance = null;
        }

        /// <summary>
        /// Called by RunProgressionController. Resets counters and subscribes
        /// to encounter Cleared. expectedEnemyBudget defaults to enc.enemyCount
        /// when ≤ 0 (TZ §12.4 fast-clear baseline = budget × 1.5s).
        /// </summary>
        public void WatchEncounter(EncounterController enc)
        {
            DropPrev();
            watched = enc;
            kills = bruteKills = gloryKills = streakBonusKills = 0;
            noHit = true;
            finalized = false;
            CurrentRawStyle = 0;
            startTime = Time.time;
            // enemyCount may not be resolved yet at WatchEncounter (composer
            // overwrites it inside SpawnEnemiesViaGameManager); we re-snapshot
            // on first kill or at clear time, whichever comes first.
            enemyBudget = Mathf.Max(0, enc != null ? enc.enemyCount : 0);
            expectedClearTime = enemyBudget * 1.5f;
            playerHealthRef = GameManager.instance != null ? GameManager.instance.playerHealth : null;
            streakRef = playerHealthRef != null ? playerHealthRef.GetComponent<KillStreakTracker>() : null;
            if (streakRef == null) streakRef = FindFirstObjectByType<KillStreakTracker>();
            if (enc != null) enc.Cleared += OnEncounterCleared;
            OnRawStyleChanged?.Invoke(0);
        }

        void DropPrev()
        {
            if (watched != null) watched.Cleared -= OnEncounterCleared;
            watched = null;
        }

        void OnAnyDeath(Health source)
        {
            if (!IsTracking || source == null) return;
            // Filter: count only enemy deaths. Player death is identified by
            // matching playerHealthRef. Anything that isn't an EnemyBrainBase
            // (or legacy SimpleEnemyAI host) is ignored.
            if (source == playerHealthRef) return;
            var brain = source.GetComponent<EnemyBrainBase>();
            bool isEnemy = brain != null || source.GetComponent<SimpleEnemyAI>() != null;
            if (!isEnemy) return;

            kills++;
            // Brute = Tank role (TZ §12.4 "Brute kill +2 replaces +1 kill").
            if (brain != null && brain.data != null && brain.data.role == EnemyRole.Tank)
            {
                bruteKills++;
            }
            // Streak bonus: per kill while streak ≥ 5 counts as +2 bonus.
            if (streakRef != null && streakRef.CurrentStreak >= 5)
            {
                streakBonusKills++;
            }
            RecomputeRaw();
        }

        void OnAnyDamaged(Health victim, float damage)
        {
            if (!IsTracking || victim == null) return;
            if (victim != playerHealthRef) return;
            if (damage <= 0f) return;
            noHit = false;
        }

        void OnGloryKill(GameObject enemy)
        {
            if (!IsTracking) return;
            gloryKills++;
            RecomputeRaw();
        }

        void RecomputeRaw()
        {
            var snapshot = SnapshotCurrent();
            CurrentRawStyle = ArenaPayoutCalculator.ComputeRawStyle(snapshot);
            OnRawStyleChanged?.Invoke(CurrentRawStyle);
        }

        StyleBreakdown SnapshotCurrent()
        {
            return new StyleBreakdown
            {
                kills = kills,
                bruteKills = bruteKills,
                gloryKills = gloryKills,
                streakBonusKills = streakBonusKills,
                noHit = noHit,
                fastClear = false, // only known at clear time
                clearTime = Time.time - startTime,
                expectedClearTime = expectedClearTime,
                rawStylePoints = 0, // filled by recipient if needed
            };
        }

        void OnEncounterCleared()
        {
            if (finalized) return;
            finalized = true;

            // Re-snapshot enemy budget — composer-driven encounters set it after
            // SpawnEnemiesViaGameManager runs, so the figure at WatchEncounter
            // time was just the inspector default.
            int budget = watched != null ? Mathf.Max(enemyBudget, watched.enemyCount) : enemyBudget;
            float expected = budget * 1.5f;
            float ct = Time.time - startTime;

            var snap = new StyleBreakdown
            {
                kills = kills,
                bruteKills = bruteKills,
                gloryKills = gloryKills,
                streakBonusKills = streakBonusKills,
                noHit = noHit,
                clearTime = ct,
                expectedClearTime = expected,
                fastClear = expected > 0f && ct < expected * 0.7f,
            };
            snap.rawStylePoints = ArenaPayoutCalculator.ComputeRawStyle(snap);

            OnArenaFinalized?.Invoke(snap);

            DropPrev();
        }
    }
}
