using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VoidSurvivor.ProceduralArena.Run
{
    public enum RunState { Idle, Generating, Entering, Playing, Transitioning, Victory, GameOver }

    /// <summary>
    /// Top-level state machine for a full 5-arena run. Drives
    /// ArenaFlowController transitions, exposes ChooseDoor / NotifyPlayerDied
    /// APIs, and shows placeholder Victory / GameOver screens.
    /// For PR 2.B combat gating is skipped (cfg.skipClearCondition) —
    /// the player may walk to an exit immediately; PR 2.C replaces that
    /// with real encounter clear conditions.
    /// </summary>
    public class RunController : MonoBehaviour
    {
        [Header("Refs")]
        public RunConfig runConfig;
        public ArenaFlowController flow;
        [Tooltip("Optional Health component on the Player — if set, its onDeath triggers GameOver.")]
        public UnityEngine.Events.UnityEvent playerDeathBinding;

        [Header("Debug")]
        public bool logTransitions = true;

        RunGraph graph;
        RunGraphNode current;
        RunState state = RunState.Idle;

        Canvas screenCanvas;
        Text screenText;
        Text statsText;
        Button screenButton;

        public RunState State => state;
        public RunGraph Graph => graph;
        public RunGraphNode Current => current;
        public RunConfig Config => runConfig;

        void Awake()
        {
            if (flow == null) flow = GetComponent<ArenaFlowController>();
            if (flow == null) flow = FindFirstObjectByType<ArenaFlowController>();
            if (flow != null) flow.ArenaBuilt += OnArenaBuilt;
            EnsureDebugOverlay();
            BuildScreenCanvas();
        }

        void OnDestroy()
        {
            if (flow != null) flow.ArenaBuilt -= OnArenaBuilt;
        }

        void OnArenaBuilt(RunGraphNode node)
        {
            // Subscribe to the newly created EncounterController so we know
            // when the clear condition is satisfied. If skipClearCondition
            // is true we still subscribe but the event fires immediately
            // (ClearCondition.None) or we simply ignore it.
            if (flow == null || flow.CurrentEncounter == null) return;
            var enc = flow.CurrentEncounter;
            if (runConfig != null && runConfig.skipClearCondition)
            {
                // Debug walk-through: force the encounter to auto-clear without spawning enemies.
                enc.clearCondition = VoidSurvivor.ProceduralArena.Arena.ClearCondition.None;
                return;
            }
            enc.Cleared += () =>
            {
                if (logTransitions) Debug.Log($"[Run] encounter cleared stage={node.stage} cat={node.typeProfile?.category}");
                var cat = node != null && node.typeProfile != null
                    ? node.typeProfile.category
                    : VoidSurvivor.ProceduralArena.Arena.ArenaCategory.Combat;
                VoidSurvivor.Progression.RunStatsTracker.Instance?.NotifyArenaCleared(cat);
                NotifyArenaClearedIfReady();
            };

            // Phase 4 / PR 4.PC — wire the reward gate. RPC subscribes to enc.Cleared,
            // sets HoldBarriers=true, and opens the 3-card UI before exits unlock.
            // Elite category determines whether the rarity table gets the §10.3 boost.
            var category = node != null && node.typeProfile != null
                ? node.typeProfile.category
                : VoidSurvivor.ProceduralArena.Arena.ArenaCategory.Combat;
            bool isElite = category == VoidSurvivor.ProceduralArena.Arena.ArenaCategory.Elite;
            var rpc = RunProgressionController.Instance;
            if (rpc != null)
            {
                if (rpc.runSeed == 0) rpc.runSeed = graph != null ? graph.runSeed : 0;
                rpc.WatchEncounter(enc, node != null ? node.arenaIndex : 0, isElite, category);
            }

            // Phase 4 / PR 4.PF — Shop rooms are safe utility rooms
            // (ClearCondition.None), so they do not use the reward gate.
            // Prepare deterministic inventory here; the UI opens only when
            // the player steps onto the generated Shop platform.
            if (category == VoidSurvivor.ProceduralArena.Arena.ArenaCategory.Shop)
            {
                ShopController.Instance?.PrepareForArena(
                    category,
                    node != null ? node.arenaIndex : 0,
                    graph != null ? graph.runSeed : 0);
            }

            // Phase 4 / PR 4.PG — Rest rooms gate exits behind a one-shot
            // choice (Heal / +10 Max HP / Prime next reward). Barriers stay
            // closed until RestRoomController.OpenBarriers() is called.
            if (category == VoidSurvivor.ProceduralArena.Arena.ArenaCategory.Rest)
            {
                RestRoomController.Instance?.PrepareForArena(
                    category,
                    node != null ? node.arenaIndex : 0,
                    enc);
            }
        }

        IEnumerator Start()
        {
            if (runConfig != null && runConfig.autoStartOnPlay)
            {
                yield return new WaitForEndOfFrame();
                StartRun(runConfig.runSeed);
            }
        }

        [ContextMenu("Start Run")]
        public void StartRunFromContext() => StartRun(runConfig != null ? runConfig.runSeed : 0);

        public void StartRun(int runSeed)
        {
            if (runConfig == null) { Debug.LogWarning("[RunController] No runConfig."); return; }
            if (flow == null) { Debug.LogWarning("[RunController] No ArenaFlowController."); return; }
            HideScreen();
            // Phase 4 / PR 4.PC — fresh run wipes all active upgrades + rerolls
            // the reward seed so the same run produces the same cards (§16).
            UpgradeSystem.Instance?.ResetForNewRun();
            // Phase 4 / PR 4.PE — wipe Kill Points; new run, fresh wallet (TZ §16).
            KillPointsWallet.Instance?.ResetForNewRun();
            // Phase 4 / PR 4.PH — reset run-scoped stats tracker.
            VoidSurvivor.Progression.RunStatsTracker.Instance?.ResetForNewRun();
            var rpc = RunProgressionController.Instance;
            if (rpc != null)
            {
                rpc.runSeed = runSeed;
                rpc.ResetForNewRun();
            }
            graph = RunGraphGenerator.Build(runSeed, runConfig);
            current = graph.startNode;
            if (logTransitions) Debug.Log($"[Run] start seed={graph.runSeed} nodes={graph.nodes.Count}");
            StopAllCoroutines();
            StartCoroutine(CoEnterCurrent(runConfig.fadeInSeconds, runConfig.fadeHoldSeconds, runConfig.fadeOutSeconds));
        }

        public void ChooseDoor(int childIndex)
        {
            if (state != RunState.Playing) return;
            if (current == null || current.children == null || current.children.Count == 0) return;
            int idx = Mathf.Clamp(childIndex, 0, current.children.Count - 1);
            var next = current.children[idx];
            if (next == null) return;
            if (logTransitions) Debug.Log($"[Run] choose door {idx} -> stage={next.stage} cat={next.typeProfile?.category}");
            current = next;
            StartCoroutine(CoEnterCurrent(runConfig.fadeInSeconds, runConfig.fadeHoldSeconds, runConfig.fadeOutSeconds));
        }

        /// <summary>Called from a Health.onDeath UnityEvent or gameplay code.</summary>
        public void NotifyPlayerDied()
        {
            if (state == RunState.GameOver || state == RunState.Victory) return;
            state = RunState.GameOver;
            StopAllCoroutines();
            ShowScreen("GAME OVER", "Restart");
        }

        public void NotifyArenaClearedIfReady()
        {
            // Hook for PR 2.C to call once real clear-condition is met.
            // For PR 2.B with skipClearCondition=true, exits are always unlocked
            // immediately after arena is built, so nothing to do here.
        }

        IEnumerator CoEnterCurrent(float fadeIn, float hold, float fadeOut)
        {
            state = RunState.Transitioning;
            yield return flow.EnterArena(current, this, fadeIn, hold, fadeOut);
            state = RunState.Playing;
            if (current != null && current.Category == VoidSurvivor.ProceduralArena.Arena.ArenaCategory.Boss && (runConfig == null || runConfig.skipClearCondition))
            {
                // Boss has no children — for PR 2.B we complete the run when player
                // reaches the boss's exit. But boss profile has exitCount=1, which
                // becomes a "victory trigger". We repurpose its ExitDoorTrigger —
                // when fired with childIndex=0 but no children, we trigger victory.
            }
        }

        public void NotifyExitTriggeredOnBoss()
        {
            if (state == RunState.GameOver || state == RunState.Victory) return;
            state = RunState.Victory;
            StopAllCoroutines();
            ShowScreen("VICTORY", "New Run");
        }

        // ---------------- Screens ----------------

        void BuildScreenCanvas()
        {
            var canvasGo = new GameObject("RunScreenCanvas");
            canvasGo.transform.SetParent(transform, false);
            screenCanvas = canvasGo.AddComponent<Canvas>();
            screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            screenCanvas.sortingOrder = 6000;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            var rt = bg.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(canvasGo.transform, false);
            screenText = textGo.AddComponent<Text>();
            screenText.alignment = TextAnchor.UpperCenter;
            screenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            screenText.fontSize = 64;
            screenText.fontStyle = FontStyle.Bold;
            screenText.color = Color.white;
            var trt = screenText.rectTransform;
            trt.anchorMin = new Vector2(0.15f, 0.78f);
            trt.anchorMax = new Vector2(0.85f, 0.94f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

            var statsGo = new GameObject("Stats");
            statsGo.transform.SetParent(canvasGo.transform, false);
            statsText = statsGo.AddComponent<Text>();
            statsText.alignment = TextAnchor.UpperLeft;
            statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statsText.fontSize = 24;
            statsText.color = new Color(0.9f, 0.95f, 1f, 1f);
            statsText.horizontalOverflow = HorizontalWrapMode.Overflow;
            statsText.verticalOverflow = VerticalWrapMode.Overflow;
            var srt = statsText.rectTransform;
            srt.anchorMin = new Vector2(0.22f, 0.3f);
            srt.anchorMax = new Vector2(0.78f, 0.78f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

            var btnGo = new GameObject("Button");
            btnGo.transform.SetParent(canvasGo.transform, false);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 0.9f, 0.9f);
            screenButton = btnGo.AddComponent<Button>();
            var btnRt = btnImg.rectTransform;
            btnRt.anchorMin = new Vector2(0.35f, 0.2f);
            btnRt.anchorMax = new Vector2(0.65f, 0.3f);
            btnRt.offsetMin = Vector2.zero; btnRt.offsetMax = Vector2.zero;

            var btnTextGo = new GameObject("BtnText");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 36;
            btnText.color = Color.white;
            btnText.text = "Restart";
            var btnTRt = btnText.rectTransform;
            btnTRt.anchorMin = Vector2.zero; btnTRt.anchorMax = Vector2.one;
            btnTRt.offsetMin = Vector2.zero; btnTRt.offsetMax = Vector2.zero;

            screenButton.onClick.AddListener(OnScreenButton);
            screenCanvas.enabled = false;
        }

        void EnsureDebugOverlay()
        {
            var overlay = GetComponent<ArenaRuntimeDebugOverlay>();
            if (overlay == null) overlay = gameObject.AddComponent<ArenaRuntimeDebugOverlay>();
            overlay.controller = this;
            overlay.flow = flow;
        }

        void ShowScreen(string title, string buttonLabel)
        {
            if (screenCanvas == null) return;
            screenText.text = title;
            var txt = screenButton.GetComponentInChildren<Text>(true);
            if (txt != null && txt != screenText) txt.text = buttonLabel;
            if (statsText != null)
            {
                var tracker = VoidSurvivor.Progression.RunStatsTracker.Instance;
                if (tracker != null) tracker.StopRun();
                statsText.text = tracker != null ? FormatStats(tracker.Snapshot()) : "";
            }
            screenCanvas.enabled = true;
        }

        static string FormatStats(VoidSurvivor.Progression.RunStatsSnapshot s)
        {
            int minutes = Mathf.FloorToInt(s.runDurationSeconds / 60f);
            int seconds = Mathf.FloorToInt(s.runDurationSeconds - minutes * 60f);
            var sb = new System.Text.StringBuilder(384);
            sb.Append("Time: ").Append(minutes).Append(':').Append(seconds.ToString("D2")).Append('\n');
            sb.Append("Arenas cleared: ").Append(s.arenasCleared).Append('\n');
            sb.Append("Kills: ").Append(s.totalKills)
              .Append("   Brute: ").Append(s.bruteKills)
              .Append("   Glory: ").Append(s.gloryKills).Append('\n');
            sb.Append("Damage taken: ").Append(s.damageTaken).Append('\n');
            sb.Append("Kill Points: +").Append(s.kpEarned).Append("  /  -").Append(s.kpSpent).Append('\n');
            sb.Append("Visits: Shop ").Append(s.shopVisits)
              .Append("   Rest ").Append(s.restVisits)
              .Append("   Elite ").Append(s.eliteVisits).Append('\n');
            sb.Append('\n').Append("Upgrades:");
            if (s.upgradesTaken == null || s.upgradesTaken.Length == 0)
            {
                sb.Append(" (none)");
            }
            else
            {
                for (int i = 0; i < s.upgradesTaken.Length; i++)
                    sb.Append("\n  - ").Append(s.upgradesTaken[i]);
            }
            return sb.ToString();
        }

        void HideScreen()
        {
            if (screenCanvas != null) screenCanvas.enabled = false;
        }

        void OnScreenButton()
        {
            // Restart the full run with a new time-seed for GameOver, or a fresh runSeed for Victory.
            HideScreen();
            if (runConfig != null)
            {
                int nextSeed = state == RunState.Victory ? 0 /* new randomised seed */ : 0;
                StartRun(nextSeed);
            }
            else
            {
                // Fallback: reload scene
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
