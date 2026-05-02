using TMPro;
using UnityEngine;

// Small "+HP" floater that appears near the HP block on heal events.
// Triggered explicitly from CombatHUDController when player Heal fires.
public class HealFloatBlock
{
    RectTransform root;
    TextMeshProUGUI text;
    float life;
    const float MaxLife = 0.9f;

    public static HealFloatBlock Build(RectTransform canvasRoot)
    {
        var block = new HealFloatBlock();
        block.BuildInternal(canvasRoot);
        return block;
    }

    void BuildInternal(RectTransform canvasRoot)
    {
        var go = new GameObject("HealFloat", typeof(RectTransform));
        root = (RectTransform)go.transform;
        root.SetParent(canvasRoot, false);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 0f);
        root.pivot = new Vector2(0f, 0f);
        root.anchoredPosition = new Vector2(220f, 130f);
        root.sizeDelta = new Vector2(160f, 32f);

        text = go.AddComponent<TextMeshProUGUI>();
        text.font = CombatHUDStyle.DefaultFont();
        text.fontSize = 26f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.color = new Color(CombatHUDStyle.HealCyanGreen.r, CombatHUDStyle.HealCyanGreen.g, CombatHUDStyle.HealCyanGreen.b, 0f);
        text.text = "+0 HP";
        text.raycastTarget = false;
    }

    public void Show(float amount)
    {
        if (text == null) return;
        text.text = $"+{Mathf.CeilToInt(amount)} HP";
        life = MaxLife;
    }

    public void Tick(float dt)
    {
        if (life <= 0f)
        {
            if (text != null)
            {
                var c = text.color; c.a = 0f; text.color = c;
            }
            return;
        }
        life -= dt;
        float t = Mathf.Clamp01(life / MaxLife);
        // Float up + fade out
        float yOffset = (1f - t) * 24f;
        var rt = root;
        rt.anchoredPosition = new Vector2(220f, 130f + yOffset);
        var col = text.color;
        col.a = Mathf.SmoothStep(0f, 1f, t);
        text.color = col;
    }
}
