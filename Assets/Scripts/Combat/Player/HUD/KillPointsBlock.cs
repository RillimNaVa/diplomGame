using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Phase 4 / PR 4.PE — top-right KP counter beneath the EnemyCounterBlock.
// Subscribes to KillPointsWallet.OnTotalChanged and pulses the value on award.
public class KillPointsBlock
{
    RectTransform root;
    TextMeshProUGUI valueText;
    TextMeshProUGUI labelText;
    Image accent;

    int displayedValue = -1;
    float pulse01;
    KillPointsWallet wallet;

    public static KillPointsBlock Build(RectTransform canvasRoot)
    {
        var block = new KillPointsBlock();
        block.BuildInternal(canvasRoot);
        block.Subscribe();
        return block;
    }

    void BuildInternal(RectTransform canvasRoot)
    {
        var go = new GameObject("KillPointsBlock", typeof(RectTransform));
        root = (RectTransform)go.transform;
        root.SetParent(canvasRoot, false);
        root.anchorMin = new Vector2(1f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(1f, 1f);
        // Sit just below EnemyCounterBlock (which is at -28y, height 56 → ~-90y).
        root.anchoredPosition = new Vector2(-32f, -100f);
        root.sizeDelta = new Vector2(220f, 48f);

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(root, false);
        labelRt.anchorMin = new Vector2(1f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(1f, 1f);
        labelRt.anchoredPosition = new Vector2(-72f, 0f);
        labelRt.sizeDelta = new Vector2(140f, 22f);
        labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.font = CombatHUDStyle.DefaultFont();
        labelText.fontSize = 16f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Right;
        labelText.color = CombatHUDStyle.CyanDim;
        labelText.text = "KILL POINTS";
        labelText.raycastTarget = false;

        var valGo = new GameObject("Value", typeof(RectTransform));
        var valRt = (RectTransform)valGo.transform;
        valRt.SetParent(root, false);
        valRt.anchorMin = new Vector2(1f, 1f);
        valRt.anchorMax = new Vector2(1f, 1f);
        valRt.pivot = new Vector2(1f, 1f);
        valRt.anchoredPosition = new Vector2(0f, 0f);
        valRt.sizeDelta = new Vector2(72f, 32f);
        valueText = valGo.AddComponent<TextMeshProUGUI>();
        valueText.font = CombatHUDStyle.DefaultFont();
        valueText.fontSize = 26f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.TopRight;
        valueText.color = CombatHUDStyle.WarnOrange;
        valueText.text = "0";
        valueText.raycastTarget = false;

        var accentGo = new GameObject("Accent", typeof(RectTransform));
        var accentRt = (RectTransform)accentGo.transform;
        accentRt.SetParent(root, false);
        accentRt.anchorMin = new Vector2(1f, 1f);
        accentRt.anchorMax = new Vector2(1f, 1f);
        accentRt.pivot = new Vector2(1f, 1f);
        accentRt.anchoredPosition = new Vector2(0f, -42f);
        accentRt.sizeDelta = new Vector2(180f, 2f);
        accent = accentGo.AddComponent<Image>();
        accent.sprite = CombatHUDStyle.WhiteSprite();
        accent.color = CombatHUDStyle.WarnOrange;
        accent.raycastTarget = false;
    }

    void Subscribe()
    {
        wallet = KillPointsWallet.Instance;
        if (wallet == null) return;
        wallet.OnTotalChanged += OnTotalChanged;
        OnTotalChanged(wallet.Total, 0);
    }

    public void Unsubscribe()
    {
        if (wallet != null) wallet.OnTotalChanged -= OnTotalChanged;
        wallet = null;
    }

    void OnTotalChanged(int newTotal, int delta)
    {
        if (valueText != null) valueText.text = newTotal.ToString();
        displayedValue = newTotal;
        if (delta > 0) pulse01 = 1f;
    }

    public void Tick(float dt)
    {
        // Fall-back poll — covers the case where the block was built before the
        // wallet existed and Subscribe() bound to nothing.
        if (wallet == null)
        {
            wallet = KillPointsWallet.Instance;
            if (wallet != null)
            {
                wallet.OnTotalChanged += OnTotalChanged;
                OnTotalChanged(wallet.Total, 0);
            }
        }
        if (wallet != null && wallet.Total != displayedValue)
        {
            OnTotalChanged(wallet.Total, wallet.Total - displayedValue);
        }

        if (pulse01 > 0f) pulse01 = Mathf.Max(0f, pulse01 - dt / 0.45f);
        if (valueText != null)
        {
            valueText.color = Color.Lerp(CombatHUDStyle.WarnOrange, CombatHUDStyle.HealCyanGreen, pulse01);
            float scale = 1f + pulse01 * 0.18f;
            valueText.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
