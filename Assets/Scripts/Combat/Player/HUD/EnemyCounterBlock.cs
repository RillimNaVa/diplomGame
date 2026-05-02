using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Compact upper-right enemy counter. Polls GameManager.EnemiesAlive each frame
// (cheap). Pulses subtly on kill.
public class EnemyCounterBlock
{
    RectTransform root;
    TextMeshProUGUI countText;
    TextMeshProUGUI labelText;
    Image accent;

    int lastShownCount = -1;
    float pulse01;

    public static EnemyCounterBlock Build(RectTransform canvasRoot)
    {
        var block = new EnemyCounterBlock();
        block.BuildInternal(canvasRoot);
        block.Subscribe();
        return block;
    }

    void BuildInternal(RectTransform canvasRoot)
    {
        var go = new GameObject("EnemyCounterBlock", typeof(RectTransform));
        root = (RectTransform)go.transform;
        root.SetParent(canvasRoot, false);
        root.anchorMin = new Vector2(1f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(1f, 1f);
        root.anchoredPosition = new Vector2(-32f, -28f);
        root.sizeDelta = new Vector2(220f, 56f);

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(root, false);
        labelRt.anchorMin = new Vector2(1f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(1f, 1f);
        labelRt.anchoredPosition = new Vector2(-72f, -2f);
        labelRt.sizeDelta = new Vector2(140f, 22f);
        labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.font = CombatHUDStyle.DefaultFont();
        labelText.fontSize = 18f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Right;
        labelText.color = CombatHUDStyle.CyanDim;
        labelText.text = "ENEMIES";
        labelText.raycastTarget = false;

        var countGo = new GameObject("Count", typeof(RectTransform));
        var countRt = (RectTransform)countGo.transform;
        countRt.SetParent(root, false);
        countRt.anchorMin = new Vector2(1f, 1f);
        countRt.anchorMax = new Vector2(1f, 1f);
        countRt.pivot = new Vector2(1f, 1f);
        countRt.anchoredPosition = new Vector2(0f, -2f);
        countRt.sizeDelta = new Vector2(72f, 40f);
        countText = countGo.AddComponent<TextMeshProUGUI>();
        countText.font = CombatHUDStyle.DefaultFont();
        countText.fontSize = 32f;
        countText.fontStyle = FontStyles.Bold;
        countText.alignment = TextAlignmentOptions.TopRight;
        countText.color = CombatHUDStyle.White;
        countText.text = "0";
        countText.raycastTarget = false;

        var accentGo = new GameObject("Accent", typeof(RectTransform));
        var accentRt = (RectTransform)accentGo.transform;
        accentRt.SetParent(root, false);
        accentRt.anchorMin = new Vector2(1f, 1f);
        accentRt.anchorMax = new Vector2(1f, 1f);
        accentRt.pivot = new Vector2(1f, 1f);
        accentRt.anchoredPosition = new Vector2(0f, -46f);
        accentRt.sizeDelta = new Vector2(180f, 2f);
        accent = accentGo.AddComponent<Image>();
        accent.sprite = CombatHUDStyle.WhiteSprite();
        accent.color = CombatHUDStyle.Cyan;
        accent.raycastTarget = false;
    }

    public void Subscribe()
    {
        if (GameManager.instance != null) GameManager.instance.OnEnemyKilled += OnKill;
    }

    public void Unsubscribe()
    {
        if (GameManager.instance != null) GameManager.instance.OnEnemyKilled -= OnKill;
    }

    void OnKill() { pulse01 = 1f; }

    public void Tick(float dt)
    {
        var gm = GameManager.instance;
        int alive = gm != null ? gm.EnemiesAlive : 0;
        if (alive != lastShownCount)
        {
            lastShownCount = alive;
            countText.text = alive.ToString();
        }

        if (pulse01 > 0f) pulse01 = Mathf.Max(0f, pulse01 - dt / 0.25f);
        if (countText != null)
        {
            countText.color = Color.Lerp(CombatHUDStyle.White, CombatHUDStyle.WarnOrange, pulse01 * 0.6f);
            float scale = 1f + pulse01 * 0.08f;
            countText.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
