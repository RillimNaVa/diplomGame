using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;
using VoidSurvivor.ProceduralArena.Encounter;

namespace VoidSurvivor.Progression
{
    /// <summary>
    /// Phase 4 / PR 4.PH — run-scoped stats tracker (TZ §18 Run Stats screen).
    ///
    /// Auto-creating scene singleton. Subscribes to gameplay events and aggregates
    /// per-run totals. Reset on <see cref="ResetForNewRun"/>; queried by
    /// <c>RunController</c> when showing Victory / GameOver screens.
    ///
    /// Distinct from <see cref="StylePointsTracker"/> which is per-arena.
    /// </summary>
    public class RunStatsTracker : MonoBehaviour
    {
        static RunStatsTracker s_instance;

        // Aggregates
        int totalKills;
        int bruteKills;
        int gloryKills;
        float damageTaken;
        int deaths;
        int kpEarned;
        int kpSpent;
        int arenasCleared;
        int shopVisits;
        int restVisits;
        int eliteVisits;
        readonly List<string> upgradesTaken = new List<string>(16);
        float runStartTime;
        float runEndTime;
        bool running;

        Health playerHealthRef;
        UpgradeSystem upgradeSystemRef;
        KillPointsWallet walletRef;

        public static RunStatsTracker Instance
        {
            get
            {
                if (s_instance != null) return s_instance;
                var found =
#if UNITY_2023_1_OR_NEWER
                    UnityEngine.Object.FindFirstObjectByType<RunStatsTracker>();
#else
                    UnityEngine.Object.FindObjectOfType<RunStatsTracker>();
#endif
                if (found != null) { s_instance = found; return s_instance; }
                var go = new GameObject("RunStatsTracker");
                s_instance = go.AddComponent<RunStatsTracker>();
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
            BindUpgradeSystem();
            BindWallet();
        }

        void OnDisable()
        {
            Health.AnyDeath -= OnAnyDeath;
            Health.AnyDamaged -= OnAnyDamaged;
            if (upgradeSystemRef != null)
            {
                upgradeSystemRef.OnUpgradesChanged -= OnUpgradesChanged;
                upgradeSystemRef.OnGloryKill -= OnGloryKill;
                upgradeSystemRef = null;
            }
            if (walletRef != null)
            {
                walletRef.OnTotalChanged -= OnWalletChanged;
                walletRef = null;
            }
        }

        void OnDestroy()
        {
            if (s_instance == this) s_instance = null;
        }

        void BindUpgradeSystem()
        {
            if (upgradeSystemRef != null) return;
            upgradeSystemRef = UpgradeSystem.Instance;
            if (upgradeSystemRef != null)
            {
                upgradeSystemRef.OnUpgradesChanged += OnUpgradesChanged;
                upgradeSystemRef.OnGloryKill += OnGloryKill;
            }
        }

        void BindWallet()
        {
            if (walletRef != null) return;
            walletRef = KillPointsWallet.Instance;
            if (walletRef != null) walletRef.OnTotalChanged += OnWalletChanged;
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------
        public void ResetForNewRun()
        {
            totalKills = 0;
            bruteKills = 0;
            gloryKills = 0;
            damageTaken = 0f;
            deaths = 0;
            kpEarned = 0;
            kpSpent = 0;
            arenasCleared = 0;
            shopVisits = 0;
            restVisits = 0;
            eliteVisits = 0;
            upgradesTaken.Clear();
            runStartTime = Time.time;
            runEndTime = 0f;
            running = true;
            playerHealthRef = GameManager.instance != null ? GameManager.instance.playerHealth : null;

            // Late-bind in case singletons spawn after our Awake.
            BindUpgradeSystem();
            BindWallet();
        }

        public void NotifyArenaCleared(ArenaCategory category)
        {
            if (!running) return;
            arenasCleared++;
            switch (category)
            {
                case ArenaCategory.Shop: shopVisits++; break;
                case ArenaCategory.Rest: restVisits++; break;
                case ArenaCategory.Elite: eliteVisits++; break;
            }
        }

        public void StopRun()
        {
            if (!running) return;
            running = false;
            runEndTime = Time.time;
        }

        public RunStatsSnapshot Snapshot()
        {
            float endTime = running ? Time.time : runEndTime;
            return new RunStatsSnapshot
            {
                totalKills = totalKills,
                bruteKills = bruteKills,
                gloryKills = gloryKills,
                damageTaken = Mathf.RoundToInt(damageTaken),
                deaths = deaths,
                kpEarned = kpEarned,
                kpSpent = kpSpent,
                arenasCleared = arenasCleared,
                shopVisits = shopVisits,
                restVisits = restVisits,
                eliteVisits = eliteVisits,
                upgradesTaken = upgradesTaken.ToArray(),
                runDurationSeconds = endTime - runStartTime,
            };
        }

        // ------------------------------------------------------------------
        // Event handlers
        // ------------------------------------------------------------------
        void OnAnyDeath(Health source)
        {
            if (!running || source == null) return;
            if (playerHealthRef == null && GameManager.instance != null)
                playerHealthRef = GameManager.instance.playerHealth;
            if (source == playerHealthRef)
            {
                deaths++;
                return;
            }
            var brain = source.GetComponent<EnemyBrainBase>();
            bool isEnemy = brain != null || source.GetComponent<SimpleEnemyAI>() != null;
            if (!isEnemy) return;
            totalKills++;
            if (brain != null && brain.data != null && brain.data.role == EnemyRole.Tank)
                bruteKills++;
        }

        void OnAnyDamaged(Health victim, float damage)
        {
            if (!running || victim == null || damage <= 0f) return;
            if (playerHealthRef == null && GameManager.instance != null)
                playerHealthRef = GameManager.instance.playerHealth;
            if (victim == playerHealthRef) damageTaken += damage;
        }

        void OnGloryKill(GameObject enemy)
        {
            if (!running) return;
            gloryKills++;
        }

        void OnUpgradesChanged()
        {
            // Cheapest accurate path: re-derive the list from UpgradeSystem.
            // Names are stable for the run; user-visible ordering is acquisition order
            // since UpgradeSystem stores stacks in insertion order.
            if (upgradeSystemRef == null) return;
            upgradesTaken.Clear();
            var stacks = upgradeSystemRef.ActiveUpgrades;
            if (stacks == null) return;
            for (int i = 0; i < stacks.Count; i++)
            {
                var stack = stacks[i];
                if (stack == null || stack.data == null) continue;
                string name = !string.IsNullOrEmpty(stack.data.displayName)
                    ? stack.data.displayName : stack.data.id;
                if (stack.stacks > 1) name += " x" + stack.stacks;
                upgradesTaken.Add(name);
            }
        }

        void OnWalletChanged(int newTotal, int delta)
        {
            if (!running) return;
            if (delta > 0) kpEarned += delta;
            else if (delta < 0) kpSpent += -delta;
        }
    }

    public struct RunStatsSnapshot
    {
        public int totalKills;
        public int bruteKills;
        public int gloryKills;
        public int damageTaken;
        public int deaths;
        public int kpEarned;
        public int kpSpent;
        public int arenasCleared;
        public int shopVisits;
        public int restVisits;
        public int eliteVisits;
        public string[] upgradesTaken;
        public float runDurationSeconds;
    }
}
