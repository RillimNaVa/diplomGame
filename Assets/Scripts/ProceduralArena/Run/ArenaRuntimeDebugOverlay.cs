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

        Canvas canvas;
        Text text;
        readonly StringBuilder sb = new StringBuilder(256);

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
                sb.Append(current.arenaIndex + 1).Append("/5");
                sb.Append("  ").Append(current.stage);
                sb.Append("  ").Append(current.typeProfile != null ? current.typeProfile.category.ToString() : "Unknown");
            }
            else
            {
                sb.Append("-");
            }

            sb.Append("\nBiome: ").Append(room != null && !string.IsNullOrEmpty(room.biomeId) ? room.biomeId : "default");
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
            rt.sizeDelta = new Vector2(420f, 96f);
        }
    }
}
