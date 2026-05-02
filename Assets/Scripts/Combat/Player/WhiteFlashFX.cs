using UnityEngine;
using UnityEngine.UI;

// Full-screen white flash. Builds its own Canvas above the HUD so it doesn't
// fight the existing CombatHUDController. Self-destructs after the fade.
public class WhiteFlashFX : MonoBehaviour
{
    public float peakAlpha = 0.55f;
    public float duration = 0.18f;
    public Color color = Color.white;

    Image img;
    float age;

    public static WhiteFlashFX Spawn(float peakAlpha = 0.55f, float duration = 0.18f)
    {
        var go = new GameObject("WhiteFlashFX");
        var fx = go.AddComponent<WhiteFlashFX>();
        fx.peakAlpha = peakAlpha;
        fx.duration = duration;
        fx.Build();
        return fx;
    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000; // above CombatHUD (4000) and DamageDirection (4500)

        var imgGo = new GameObject("Flash", typeof(RectTransform));
        imgGo.transform.SetParent(transform, false);
        var rt = (RectTransform)imgGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        img = imgGo.AddComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0f);
        img.raycastTarget = false;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(age / Mathf.Max(0.0001f, duration));
        // Fast attack, slower decay: alpha = peak * (1 - t) when t > 0.1, sharp ramp before.
        float alpha;
        if (t < 0.1f) alpha = peakAlpha * (t / 0.1f);
        else alpha = peakAlpha * (1f - (t - 0.1f) / 0.9f);

        if (img != null)
        {
            var c = color; c.a = Mathf.Max(0f, alpha);
            img.color = c;
        }

        if (age >= duration) Destroy(gameObject);
    }
}
