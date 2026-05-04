using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// Lightweight runtime overlay for PR 2.D.
    /// Shows the deterministic seed, arena index, category, and biome.
    /// </summary>
    public class ArenaRuntimeDebugOverlay : MonoBehaviour
    {
        public RunController controller;
        public ArenaFlowController flow;

        [Tooltip("Phase 4 / PR 4.PH — show TZ §19 detailed debug fields (KP, style, budgets, upgrades).")]
        public bool showDetailed = true;

        Canvas canvas;
        Text text;
        readonly StringBuilder sb = new StringBuilder(512);

        void Awake()
        {
            BuildUi();
        }

        void LateUpdate()
        {
            if (controller == null) controller = GetComponent<RunController>();
            if (flow == null && controller != null) flow = controller.flow;
            if (text == null) return;

            var current = controller != null ? controller.Current : null;
            var graph = controller != null ? controller.Graph : null;
            var ctx = flow != null ? flow.CurrentContext : null;
            var room = ctx != null && ctx.layout != null && ctx.layout.rooms.Count > 0 ? ctx.layout.rooms[0] : null;

            sb.Clear();
            sb.Append("Seed: ").Append(graph != null ? graph.runSeed : 0);
            sb.Append("\nArena: ");
            if (current != null)
            {
                // PR 4.PD — total visited rooms in a Standard Run = 10 (stages 0..9 inclusive).
                sb.Append(current.arenaIndex + 1).Append("/10");
                sb.Append("  ").Append(current.typeProfile != null ? current.typeProfile.category.ToString() : "Unknown");
            }
            else
            {
                sb.Append("-");
            }

            sb.Append("\nBiome: ").Append(room != null && !string.IsNullOrEmpty(room.biomeId) ? room.biomeId : "default");

            if (showDetailed)
            {
                int kp = KillPointsWallet.Instance != null ? KillPointsWallet.Instance.Total : 0;
                int rawStyle = VoidSurvivor.Progression.StylePointsTracker.Instance != null
                    ? VoidSurvivor.Progression.StylePointsTracker.Instance.CurrentRawStyle
                    : 0;
                sb.Append("\nKP: ").Append(kp).Append("  Style raw: ").Append(rawStyle);

                if (current != null)
                {
                    int idx = current.arenaIndex;
                    int row = Mathf.Clamp(idx, 1, 8) - 1;
                    sb.Append("\nReward tier row: ").Append(row + 1);
                }

                var enc = flow != null ? flow.CurrentEncounter : null;
                if (enc != null)
                {
                    sb.Append("\nEnemy budget: ").Append(enc.enemyCount);
                    float hpMul = enc.enemyHealthMultiplier
                                  * (enc.eliteModifier != null ? enc.eliteModifier.enemyHpMultiplier : 1f);
                    sb.Append("  HP×").Append(hpMul.ToString("0.00"));
                }

                var us = UpgradeSystem.Instance;
                if (us != null)
                {
                    sb.Append("\nUpgrades: ");
                    var stacks = us.ActiveUpgrades;
                    if (stacks == null || stacks.Count == 0) sb.Append("(none)");
                    else
                    {
                        for (int i = 0; i < stacks.Count; i++)
                        {
                            var st = stacks[i];
                            if (st == null || st.data == null) continue;
                            if (i > 0) sb.Append(", ");
                            sb.Append(st.data.id);
                            if (st.stacks > 1) sb.Append('x').Append(st.stacks);
                        }
                    }
                }

                var rpc = RunProgressionController.Instance;
                if (rpc != null && rpc.HasPendingRareBoost)
                    sb.Append("\nPending rare boost: YES");
            }

            text.text = sb.ToString();
            text.color = room != null ? room.biomeDebugTint : Color.white;
        }

        void BuildUi()
        {
            if (canvas != null && text != null) return;

            var canvasGo = new GameObject("ArenaRuntimeDebugCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5500;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("ArenaRuntimeDebugText");
            textGo.transform.SetParent(canvasGo.transform, false);
            text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(18f, -18f);
            rt.sizeDelta = new Vector2(560f, 260f);
        }
    }
}
