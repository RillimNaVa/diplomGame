using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Center-screen prompt that appears just under the crosshair when GloryKillExecutor
// has a ready target. Pulses subtly so it reads even at high speed.
public class GloryKillPromptBlock
{
    RectTransform root;
    TextMeshProUGUI text;
    Image accent;

    GloryKillExecutor executor;
    float pulse01;

    public static GloryKillPromptBlock Build(RectTransform canvasRoot, GloryKillExecutor exec)
    {
        var block = new GloryKillPromptBlock { executor = exec };
        block.BuildInternal(canvasRoot);
        return block;
    }

    void BuildInternal(RectTransform canvasRoot)
    {
        var go = new GameObject("GloryKillPrompt", typeof(RectTransform));
        root = (RectTransform)go.transform;
        root.SetParent(canvasRoot, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, -64f);
        root.sizeDelta = new Vector2(280f, 36f);

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(root, false);
        labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.pivot = new Vector2(0.5f, 0.5f);
        labelRt.anchoredPosition = new Vector2(0f, 6f);
        labelRt.sizeDelta = new Vector2(280f, 24f);

        text = labelGo.AddComponent<TextMeshProUGUI>();
        text.font = CombatHUDStyle.DefaultFont();
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(CombatHUDStyle.Cyan.r, CombatHUDStyle.Cyan.g, CombatHUDStyle.Cyan.b, 0f);
        text.text = "[F] EXECUTE";
        text.raycastTarget = false;

        var accentGo = new GameObject("Accent", typeof(RectTransform));
        var accentRt = (RectTransform)accentGo.transform;
        accentRt.SetParent(root, false);
        accentRt.anchorMin = accentRt.anchorMax = new Vector2(0.5f, 0.5f);
        accentRt.pivot = new Vector2(0.5f, 0.5f);
        accentRt.anchoredPosition = new Vector2(0f, -10f);
        accentRt.sizeDelta = new Vector2(180f, 2f);
        accent = accentGo.AddComponent<Image>();
        accent.sprite = CombatHUDStyle.WhiteSprite();
        accent.color = new Color(CombatHUDStyle.Cyan.r, CombatHUDStyle.Cyan.g, CombatHUDStyle.Cyan.b, 0f);
        accent.raycastTarget = false;
    }

    public void Tick(float dt)
    {
        // Lazy resolve in case the executor was added after the HUD was built.
        if (executor == null) executor = UnityEngine.Object.FindAnyObjectByType<GloryKillExecutor>();
        bool show = executor != null && executor.HasReadyTarget;
        // Smooth visibility envelope.
        pulse01 = Mathf.MoveTowards(pulse01, show ? 1f : 0f, dt * 6f);

        // Subtle on-target pulse.
        float pulse = show ? 0.85f + 0.15f * Mathf.Sin(Time.time * 6f) : 0f;
        float alpha = pulse01 * (show ? pulse : 0f);

        if (text != null)
        {
            var c = CombatHUDStyle.Cyan; c.a = alpha;
            text.color = c;
            float scale = 1f + (show ? 0.05f * Mathf.Sin(Time.time * 6f) : 0f);
            text.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
        if (accent != null)
        {
            var c = CombatHUDStyle.Cyan; c.a = alpha * 0.8f;
            accent.color = c;
        }
    }
}
