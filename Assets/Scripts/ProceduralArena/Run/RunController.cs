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
        Button screenButton;

        public RunState State => state;
        public RunGraph Graph => graph;
        public RunGraphNode Current => current;

        void Awake()
        {
            if (flow == null) flow = GetComponent<ArenaFlowController>();
            if (flow == null) flow = FindObjectOfType<ArenaFlowController>();
            BuildScreenCanvas();
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
            if (current != null && current.stage == RunStage.Boss && (runConfig == null || runConfig.skipClearCondition))
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
            screenText.alignment = TextAnchor.MiddleCenter;
            screenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            screenText.fontSize = 72;
            screenText.color = Color.white;
            var trt = screenText.rectTransform;
            trt.anchorMin = new Vector2(0.2f, 0.4f);
            trt.anchorMax = new Vector2(0.8f, 0.7f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

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

        void ShowScreen(string title, string buttonLabel)
        {
            if (screenCanvas == null) return;
            screenText.text = title;
            var txt = screenButton.GetComponentInChildren<Text>(true);
            if (txt != null && txt != screenText) txt.text = buttonLabel;
            screenCanvas.enabled = true;
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
