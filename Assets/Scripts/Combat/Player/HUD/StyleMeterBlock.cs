using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoidSurvivor.Progression;

// Phase 4 / PR 4.PE — small bottom-center "STYLE" meter that ticks up silently
// during an encounter (TZ §12.4 UI rule: no per-kill +1 spam). Hidden between
// arenas when StylePointsTracker isn't watching anything.
public class StyleMeterBlock
{
    RectTransform root;
    TextMeshProUGUI labelText;
    TextMeshProUGUI valueText;
    Image fillBar;
    RectTransform fillRt;

    StylePointsTracker tracker;
    int displayed = -1;
    float pulse01;

    // Soft "expected raw style for a healthy clear" — drives the fill bar; not
    // the actual cap (cap is per-room and applied only to KP, not raw style).
    const float DisplayCeiling = 24f;

    public static StyleMeterBlock Build(RectTransform canvasRoot)
    {
        var block = new StyleMeterBlock();
        block.BuildInternal(canvasRoot);
        block.Subscribe();
        return block;
    }

    void BuildInternal(RectTransform canvasRoot)
    {
        var go = new GameObject("StyleMeterBlock", typeof(RectTransform));
        root = (RectTransform)go.transform;
        root.SetParent(canvasRoot, false);
        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        // Sit just above the dash charges row.
        root.anchoredPosition = new Vector2(0f, 154f);
        root.sizeDelta = new Vector2(280f, 32f);

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(root, false);
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(0f, 1f);
        labelRt.pivot = new Vector2(0f, 1f);
        labelRt.anchoredPosition = new Vector2(0f, 0f);
        labelRt.sizeDelta = new Vector2(80f, 18f);
        labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.font = CombatHUDStyle.DefaultFont();
        labelText.fontSize = 14f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.color = CombatHUDStyle.CyanDim;
        labelText.text = "STYLE";
        labelText.raycastTarget = false;

        var valGo = new GameObject("Value", typeof(RectTransform));
        var valRt = (RectTransform)valGo.transform;
        valRt.SetParent(root, false);
        valRt.anchorMin = new Vector2(1f, 1f);
        valRt.anchorMax = new Vector2(1f, 1f);
        valRt.pivot = new Vector2(1f, 1f);
        valRt.anchoredPosition = new Vector2(0f, 0f);
        valRt.sizeDelta = new Vector2(80f, 18f);
        valueText = valGo.AddComponent<TextMeshProUGUI>();
        valueText.font = CombatHUDStyle.DefaultFont();
        valueText.fontSize = 14f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.color = CombatHUDStyle.White;
        valueText.text = "0";
        valueText.raycastTarget = false;

        // Track + fill bar at the bottom of the block.
        var trackGo = new GameObject("Track", typeof(RectTransform));
        var trackRt = (RectTransform)trackGo.transform;
        trackRt.SetParent(root, false);
        trackRt.anchorMin = new Vector2(0f, 0f);
        trackRt.anchorMax = new Vector2(1f, 0f);
        trackRt.pivot = new Vector2(0.5f, 0f);
        trackRt.anchoredPosition = new Vector2(0f, 0f);
        trackRt.sizeDelta = new Vector2(0f, 6f);
        var trackImg = trackGo.AddComponent<Image>();
        trackImg.sprite = CombatHUDStyle.WhiteSprite();
        trackImg.color = new Color(0.06f, 0.10f, 0.14f, 0.9f);
        trackImg.raycastTarget = false;

        var fillGo = new GameObject("Fill", typeof(RectTransform));
        fillRt = (RectTransform)fillGo.transform;
        fillRt.SetParent(trackRt, false);
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = new Vector2(0f, 0f);
        fillBar = fillGo.AddComponent<Image>();
        fillBar.sprite = CombatHUDStyle.WhiteSprite();
        fillBar.color = CombatHUDStyle.Cyan;
        fillBar.raycastTarget = false;

        SetVisible(false);
    }

    void Subscribe()
    {
        tracker = StylePointsTracker.Instance;
        if (tracker == null) return;
        tracker.OnRawStyleChanged += OnRawStyleChanged;
    }

    public void Unsubscribe()
    {
        if (tracker != null) tracker.OnRawStyleChanged -= OnRawStyleChanged;
        tracker = null;
    }

    void OnRawStyleChanged(int raw)
    {
        if (raw > displayed) pulse01 = 1f;
        displayed = raw;
        if (valueText != null) valueText.text = raw.ToString();
        UpdateFill(raw);
    }

    void UpdateFill(int raw)
    {
        if (fillRt == null || root == null) return;
        float k = Mathf.Clamp01(raw / DisplayCeiling);
        float trackW = root.rect.width;
        fillRt.sizeDelta = new Vector2(trackW * k, 0f);
    }

    void SetVisible(bool v)
    {
        if (root != null) root.gameObject.SetActive(v);
    }

    public void Tick(float dt)
    {
        if (tracker == null)
        {
            tracker = StylePointsTracker.Instance;
            if (tracker != null) tracker.OnRawStyleChanged += OnRawStyleChanged;
        }
        bool active = tracker != null && tracker.IsTracking;
        SetVisible(active);
        if (!active)
        {
            displayed = 0;
            return;
        }

        if (pulse01 > 0f) pulse01 = Mathf.Max(0f, pulse01 - dt / 0.35f);
        if (fillBar != null)
            fillBar.color = Color.Lerp(CombatHUDStyle.Cyan, CombatHUDStyle.WarnOrange, pulse01);
        if (valueText != null)
        {
            float scale = 1f + pulse01 * 0.15f;
            valueText.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
