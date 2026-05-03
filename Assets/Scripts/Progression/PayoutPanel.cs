using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.Progression
{
    /// <summary>
    /// Phase 4 / PR 4.PE — post-clear payout panel (TZ §12.6).
    /// Runtime ScreenSpaceOverlay panel; auto-builds itself + all visuals.
    /// One-shot: show, count up, hold, hide, fire onComplete.
    /// </summary>
    public class PayoutPanel : MonoBehaviour
    {
        public const float CountUpSeconds = 0.55f;
        public const float HoldSeconds = 1.4f;
        public const float FadeOutSeconds = 0.30f;

        Canvas canvas;
        CanvasGroup group;
        TextMeshProUGUI titleText;
        TextMeshProUGUI baseLine;
        TextMeshProUGUI styleLine;
        TextMeshProUGUI eliteLine;
        TextMeshProUGUI totalLine;
        Action onComplete;

        public static PayoutPanel Show(ArenaPayout payout, Action onComplete)
        {
            var go = new GameObject("PayoutPanel");
            var panel = go.AddComponent<PayoutPanel>();
            panel.BuildCanvas();
            panel.PopulateAndAnimate(payout, onComplete);
            return panel;
        }

        void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5800; // above HUD (4000), below WhiteFlash (5000) and reward cards (6000)
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 0f;

            var rootRt = (RectTransform)transform;

            // Background panel
            var bgGo = new GameObject("Bg", typeof(RectTransform));
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.SetParent(rootRt, false);
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(560f, 280f);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = CombatHUDStyle.WhiteSprite();
            bgImg.color = new Color(0.04f, 0.07f, 0.10f, 0.85f);
            bgImg.raycastTarget = false;

            var accentGo = new GameObject("Accent", typeof(RectTransform));
            var accentRt = (RectTransform)accentGo.transform;
            accentRt.SetParent(bgRt, false);
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.pivot = new Vector2(0.5f, 1f);
            accentRt.sizeDelta = new Vector2(0f, 3f);
            accentRt.anchoredPosition = Vector2.zero;
            var accentImg = accentGo.AddComponent<Image>();
            accentImg.sprite = CombatHUDStyle.WhiteSprite();
            accentImg.color = CombatHUDStyle.Cyan;
            accentImg.raycastTarget = false;

            titleText = MakeText(bgRt, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(0f, 50f), 28f, FontStyles.Bold,
                TextAlignmentOptions.Top, CombatHUDStyle.Cyan, "ARENA CLEARED");

            // Lines stacked vertically
            baseLine  = MakeText(bgRt, "BaseLine",  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -88f),  new Vector2(-48f, 32f), 22f, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft, CombatHUDStyle.White, "");
            styleLine = MakeText(bgRt, "StyleLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -124f), new Vector2(-48f, 32f), 22f, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft, CombatHUDStyle.White, "");
            eliteLine = MakeText(bgRt, "EliteLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -160f), new Vector2(-48f, 32f), 22f, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft, CombatHUDStyle.HealCyanGreen, "");
            totalLine = MakeText(bgRt, "TotalLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -212f), new Vector2(-48f, 40f), 30f, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft, CombatHUDStyle.WarnOrange, "");
        }

        static TextMeshProUGUI MakeText(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta, float fontSize, FontStyles style,
            TextAlignmentOptions align, Color color, string initial)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = CombatHUDStyle.DefaultFont();
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.text = initial;
            t.raycastTarget = false;
            return t;
        }

        void PopulateAndAnimate(ArenaPayout payout, Action complete)
        {
            onComplete = complete;
            bool showElite = payout.category == ArenaCategory.Elite;
            if (eliteLine != null) eliteLine.gameObject.SetActive(showElite);

            StartCoroutine(CoAnimate(payout));
        }

        IEnumerator CoAnimate(ArenaPayout payout)
        {
            // Fade in
            float t = 0f;
            const float fadeIn = 0.18f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                if (group != null) group.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            if (group != null) group.alpha = 1f;

            // Count up base, style, elite (if shown), total in parallel.
            float countT = 0f;
            int finalBase = payout.clearReward;
            int finalStyle = payout.cappedStyle;
            int finalTotal = payout.totalKp;
            int eliteBonus = payout.category == ArenaCategory.Elite
                ? Mathf.Max(0, finalBase - ArenaPayoutCalculator.ClearReward(ArenaCategory.Combat, payout.arenaIndex))
                : 0;

            while (countT < CountUpSeconds)
            {
                countT += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(countT / CountUpSeconds);
                // Ease-out so big numbers settle.
                float eased = 1f - Mathf.Pow(1f - k, 3f);

                int baseShown = Mathf.RoundToInt(finalBase * eased);
                int styleShown = Mathf.RoundToInt(finalStyle * eased);
                int totalShown = Mathf.RoundToInt(finalTotal * eased);
                int eliteShown = Mathf.RoundToInt(eliteBonus * eased);

                if (baseLine != null)  baseLine.text  = $"Base Reward    +{baseShown,3} KP";
                if (styleLine != null) styleLine.text = BuildStyleLine(styleShown, payout);
                if (eliteLine != null && eliteLine.gameObject.activeSelf)
                    eliteLine.text = $"Elite Bonus    +{eliteShown,3} KP";
                if (totalLine != null) totalLine.text = $"TOTAL          +{totalShown,3} KP";

                yield return null;
            }
            // Snap to final
            if (baseLine != null)  baseLine.text  = $"Base Reward    +{finalBase,3} KP";
            if (styleLine != null) styleLine.text = BuildStyleLine(finalStyle, payout);
            if (eliteLine != null && eliteLine.gameObject.activeSelf)
                eliteLine.text = $"Elite Bonus    +{eliteBonus,3} KP";
            if (totalLine != null) totalLine.text = $"TOTAL          +{finalTotal,3} KP";

            // Hold
            float hold = 0f;
            while (hold < HoldSeconds) { hold += Time.unscaledDeltaTime; yield return null; }

            // Fade out
            float fo = 0f;
            while (fo < FadeOutSeconds)
            {
                fo += Time.unscaledDeltaTime;
                if (group != null) group.alpha = 1f - Mathf.Clamp01(fo / FadeOutSeconds);
                yield return null;
            }

            var cb = onComplete;
            onComplete = null;
            if (this != null) Destroy(gameObject);
            cb?.Invoke();
        }

        static string BuildStyleLine(int kp, ArenaPayout payout)
        {
            // Compact suffix when the cap clipped the raw style total.
            var sb = new StringBuilder();
            sb.Append("Combat Style   +");
            sb.Append(kp.ToString().PadLeft(3));
            sb.Append(" KP");
            if (payout.styleCap > 0 && payout.rawStyle > payout.styleCap)
            {
                sb.Append("  (raw ");
                sb.Append(payout.rawStyle);
                sb.Append(", cap ");
                sb.Append(payout.styleCap);
                sb.Append(")");
            }
            return sb.ToString();
        }
    }
}
