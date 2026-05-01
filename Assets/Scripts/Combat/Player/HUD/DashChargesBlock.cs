using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Lower-center dash charges. Reads MaxDashCharges from PlayerController so
// adding charges later (e.g., via upgrades) automatically grows the row.
public class DashChargesBlock
{
    RectTransform root;
    Image[] pips;
    Image[] pipFills; // partial recharge fill on the next-pending pip
    TextMeshProUGUI label;

    PlayerController controller;
    int builtCount = -1;

    public static DashChargesBlock Build(RectTransform canvasRoot, PlayerController pc)
    {
        var block = new DashChargesBlock { controller = pc };
        block.BuildInternal(canvasRoot);
        return block;
    }

    void BuildInternal(RectTransform canvasRoot)
    {
        var go = new GameObject("DashChargesBlock", typeof(RectTransform));
        root = (RectTransform)go.transform;
        root.SetParent(canvasRoot, false);
        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 32f);
        root.sizeDelta = new Vector2(240f, 38f);

        // "DASH" label
        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(root, false);
        labelRt.anchorMin = new Vector2(0.5f, 0f);
        labelRt.anchorMax = new Vector2(0.5f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 0f);
        labelRt.sizeDelta = new Vector2(120f, 14f);
        label = labelGo.AddComponent<TextMeshProUGUI>();
        label.font = CombatHUDStyle.DefaultFont();
        label.fontSize = 12f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = CombatHUDStyle.CyanDim;
        label.text = "DASH";
        label.raycastTarget = false;

        RebuildPips();
    }

    void RebuildPips()
    {
        if (controller == null) return;
        int count = Mathf.Max(1, controller.MaxDashCharges);
        if (count == builtCount) return;

        // Tear down old
        if (pips != null)
        {
            for (int i = 0; i < pips.Length; i++) if (pips[i] != null) Object.Destroy(pips[i].gameObject);
        }

        pips = new Image[count];
        pipFills = new Image[count];

        const float pipW = 32f;
        const float pipH = 18f;
        const float spacing = 8f;
        float total = count * pipW + (count - 1) * spacing;
        float startX = -total * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var pipGo = new GameObject($"Pip_{i}", typeof(RectTransform));
            var pipRt = (RectTransform)pipGo.transform;
            pipRt.SetParent(root, false);
            pipRt.anchorMin = new Vector2(0f, 0f);
            pipRt.anchorMax = new Vector2(0f, 0f);
            pipRt.pivot = new Vector2(0f, 0f);
            pipRt.anchoredPosition = new Vector2(root.sizeDelta.x * 0.5f + startX + i * (pipW + spacing), 16f);
            pipRt.sizeDelta = new Vector2(pipW, pipH);

            var bg = pipGo.AddComponent<Image>();
            bg.sprite = CombatHUDStyle.WhiteSprite();
            bg.color = CombatHUDStyle.CyanDim * new Color(1f, 1f, 1f, 0.45f);
            bg.raycastTarget = false;
            pips[i] = bg;

            // Fill child (left -> right) for recharge progress when this pip is pending
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.SetParent(pipRt, false);
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(0f, 0f);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = CombatHUDStyle.WhiteSprite();
            fill.color = CombatHUDStyle.Cyan;
            fill.raycastTarget = false;
            pipFills[i] = fill;
        }

        builtCount = count;
    }

    public void Tick()
    {
        if (controller == null) return;
        RebuildPips();
        int charges = controller.DashCharges;
        int max = Mathf.Max(1, controller.MaxDashCharges);
        float progress = controller.DashRechargeProgress01;

        for (int i = 0; i < pips.Length; i++)
        {
            if (i < charges)
            {
                pips[i].color = CombatHUDStyle.CyanDim * new Color(1f, 1f, 1f, 0.45f);
                pipFills[i].color = CombatHUDStyle.Cyan;
                var rt = pipFills[i].rectTransform;
                rt.sizeDelta = new Vector2(pips[i].rectTransform.sizeDelta.x, 0f);
            }
            else
            {
                // Empty pip; the next-pending one (i == charges) gets fill animated by progress.
                pips[i].color = CombatHUDStyle.CyanDim * new Color(1f, 1f, 1f, 0.25f);
                var rt = pipFills[i].rectTransform;
                if (i == charges)
                {
                    pipFills[i].color = new Color(CombatHUDStyle.Cyan.r, CombatHUDStyle.Cyan.g, CombatHUDStyle.Cyan.b, 0.7f);
                    rt.sizeDelta = new Vector2(pips[i].rectTransform.sizeDelta.x * progress, 0f);
                }
                else
                {
                    rt.sizeDelta = new Vector2(0f, 0f);
                }
            }
        }
    }
}
