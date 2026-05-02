using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bottom-left HP block: angled segmented bar + large numeric HP value, with
// short damage / heal pulses driven by CombatHUDController. All visuals are
// procedurally built — no prefabs or art assets required.
public class HpBlock
{
    const int SegmentCount = 12;

    RectTransform root;
    TextMeshProUGUI valueText;
    Image[] segments;
    Image flashOverlay; // covers the bar; pulses on damage/heal

    float currentHp;
    float maxHp = 100f;

    // Animation state
    float displayHp;       // what the number shows (lerps toward currentHp)
    float damagePulse01;   // 1 -> 0 over time
    float healPulse01;
    Color flashColor;

    public static HpBlock Build(RectTransform canvasRoot)
    {
        var block = new HpBlock();
        block.BuildInternal(canvasRoot);
        return block;
    }

    void BuildInternal(RectTransform canvasRoot)
    {
        var go = new GameObject("HpBlock", typeof(RectTransform));
        root = (RectTransform)go.transform;
        root.SetParent(canvasRoot, false);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 0f);
        root.pivot = new Vector2(0f, 0f);
        root.anchoredPosition = new Vector2(48f, 48f);
        root.sizeDelta = new Vector2(420f, 110f);

        // Numeric HP — large, aligned to the bottom-left
        var numberGo = new GameObject("HpValue", typeof(RectTransform));
        var numberRt = (RectTransform)numberGo.transform;
        numberRt.SetParent(root, false);
        numberRt.anchorMin = new Vector2(0f, 0f);
        numberRt.anchorMax = new Vector2(0f, 0f);
        numberRt.pivot = new Vector2(0f, 0f);
        numberRt.anchoredPosition = new Vector2(4f, 36f);
        numberRt.sizeDelta = new Vector2(220f, 80f);

        valueText = numberGo.AddComponent<TextMeshProUGUI>();
        valueText.font = CombatHUDStyle.DefaultFont();
        valueText.fontSize = 64f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.BottomLeft;
        valueText.color = CombatHUDStyle.White;
        valueText.text = "100";
        valueText.raycastTarget = false;

        // Small "HP" label tucked under the number's right edge
        var labelGo = new GameObject("HpLabel", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(root, false);
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(0f, 0f);
        labelRt.pivot = new Vector2(0f, 0f);
        labelRt.anchoredPosition = new Vector2(150f, 42f);
        labelRt.sizeDelta = new Vector2(80f, 24f);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.font = CombatHUDStyle.DefaultFont();
        labelTmp.fontSize = 20f;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.BottomLeft;
        labelTmp.color = CombatHUDStyle.Cyan;
        labelTmp.text = "HP";
        labelTmp.raycastTarget = false;

        // Segmented bar container — sits just below the number row.
        var barGo = new GameObject("HpBar", typeof(RectTransform));
        var barRt = (RectTransform)barGo.transform;
        barRt.SetParent(root, false);
        barRt.anchorMin = new Vector2(0f, 0f);
        barRt.anchorMax = new Vector2(0f, 0f);
        barRt.pivot = new Vector2(0f, 0f);
        barRt.anchoredPosition = new Vector2(4f, 8f);
        barRt.sizeDelta = new Vector2(412f, 22f);

        segments = new Image[SegmentCount];
        const float spacing = 4f;
        float totalSpacing = spacing * (SegmentCount - 1);
        float segWidth = (barRt.sizeDelta.x - totalSpacing) / SegmentCount;
        float segHeight = barRt.sizeDelta.y;

        for (int i = 0; i < SegmentCount; i++)
        {
            var segGo = new GameObject($"Seg_{i}", typeof(RectTransform));
            var segRt = (RectTransform)segGo.transform;
            segRt.SetParent(barRt, false);
            segRt.anchorMin = new Vector2(0f, 0f);
            segRt.anchorMax = new Vector2(0f, 0f);
            segRt.pivot = new Vector2(0f, 0f);
            segRt.anchoredPosition = new Vector2(i * (segWidth + spacing), 0f);
            segRt.sizeDelta = new Vector2(segWidth, segHeight);
            // Slight skew via rotation around bottom-left for the angled feel.
            segRt.localEulerAngles = new Vector3(0f, 0f, 0f);

            var img = segGo.AddComponent<Image>();
            img.sprite = CombatHUDStyle.WhiteSprite();
            img.color = CombatHUDStyle.Cyan;
            img.raycastTarget = false;
            segments[i] = img;
        }

        // Flash overlay — covers the bar area, pulses to white/cyan/red on events.
        var flashGo = new GameObject("FlashOverlay", typeof(RectTransform));
        var flashRt = (RectTransform)flashGo.transform;
        flashRt.SetParent(barRt, false);
        flashRt.anchorMin = Vector2.zero;
        flashRt.anchorMax = Vector2.one;
        flashRt.offsetMin = new Vector2(-2f, -2f);
        flashRt.offsetMax = new Vector2(2f, 2f);
        flashOverlay = flashGo.AddComponent<Image>();
        flashOverlay.sprite = CombatHUDStyle.WhiteSprite();
        flashOverlay.color = new Color(1f, 1f, 1f, 0f);
        flashOverlay.raycastTarget = false;
    }

    public void SetHp(float current, float max, bool instant)
    {
        currentHp = Mathf.Max(0f, current);
        maxHp = Mathf.Max(0.01f, max);
        if (instant) displayHp = currentHp;
        UpdateSegments();
    }

    public void PulseDamage()
    {
        damagePulse01 = 1f;
        flashColor = CombatHUDStyle.WarnRed;
    }

    public void PulseHeal(float amount)
    {
        healPulse01 = 1f;
        flashColor = CombatHUDStyle.HealCyanGreen;
    }

    public void Tick(float dt)
    {
        // Number lerp — fast on damage (drops within ~0.25s), instant on heal.
        if (displayHp > currentHp)
        {
            displayHp = Mathf.MoveTowards(displayHp, currentHp, Mathf.Max(40f, (displayHp - currentHp) * 4f) * dt);
        }
        else if (displayHp < currentHp)
        {
            displayHp = currentHp;
        }
        if (valueText != null)
        {
            valueText.text = Mathf.CeilToInt(displayHp).ToString();
            valueText.color = Color.Lerp(CombatHUDStyle.White, CombatHUDStyle.WarnRed,
                Mathf.Clamp01(1f - currentHp / maxHp - 0.4f));
        }

        // Pulse decay
        if (damagePulse01 > 0f) damagePulse01 = Mathf.Max(0f, damagePulse01 - dt / 0.35f);
        if (healPulse01 > 0f) healPulse01 = Mathf.Max(0f, healPulse01 - dt / 0.45f);
        float pulse = Mathf.Max(damagePulse01, healPulse01);
        if (flashOverlay != null)
        {
            var c = flashColor;
            c.a = pulse * 0.55f;
            flashOverlay.color = c;
        }

        UpdateSegments();
    }

    void UpdateSegments()
    {
        if (segments == null) return;
        float frac = Mathf.Clamp01(currentHp / maxHp);
        Color baseColor = CombatHUDStyle.HpColorForFraction(frac);
        float filledF = frac * SegmentCount;
        int filledFull = Mathf.FloorToInt(filledF);
        float partial = filledF - filledFull;

        for (int i = 0; i < SegmentCount; i++)
        {
            Color c;
            if (i < filledFull)
            {
                c = baseColor;
            }
            else if (i == filledFull && partial > 0.01f)
            {
                c = Color.Lerp(CombatHUDStyle.CyanDim, baseColor, partial);
            }
            else
            {
                c = CombatHUDStyle.CyanDim;
                c.a = 0.45f;
            }
            segments[i].color = c;
        }
    }
}
