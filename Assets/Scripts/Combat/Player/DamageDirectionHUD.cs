using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Phase 5 / PR 5.C — directional damage indicator. When the player takes a
// hit from an attacker outside the camera frustum, draws a red half-arc on
// the HUD pointing toward the attacker so the player can pivot defensively.
//
// Auto-builds its own Canvas + Image so dropping the component on the player
// (or letting GameManager.ResolveReferences attach it) is enough — no Editor
// wiring required.
[RequireComponent(typeof(Health))]
public class DamageDirectionHUD : MonoBehaviour
{
    [Tooltip("Seconds the indicator stays visible after a hit before fading out.")]
    public float visibleDuration = 0.45f;
    [Tooltip("Seconds for the fade-out tail.")]
    public float fadeDuration = 0.35f;
    [Tooltip("Pixel offset from screen center to the inner edge of the indicator.")]
    public float radiusPixels = 240f;
    [Tooltip("Indicator color.")]
    [ColorUsage(true, true)]
    public Color indicatorColor = new Color(1f, 0.22f, 0.30f, 0.85f);

    Health health;
    Camera mainCam;
    Canvas canvas;
    RectTransform pivot;
    Image indicator;
    readonly List<HitInstance> activeHits = new List<HitInstance>();

    struct HitInstance
    {
        public Vector3 source;
        public float fireTime;
    }

    void Awake()
    {
        health = GetComponent<Health>();
        BuildCanvas();
    }

    void OnEnable()
    {
        if (health != null) health.onTakeDamage.AddListener(OnDamage);
    }

    void OnDisable()
    {
        if (health != null) health.onTakeDamage.RemoveListener(OnDamage);
    }

    void BuildCanvas()
    {
        var canvasGo = new GameObject("DamageDirectionCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4500;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        var pivotGo = new GameObject("DamageArrowPivot");
        pivotGo.transform.SetParent(canvasGo.transform, false);
        pivot = pivotGo.AddComponent<RectTransform>();
        pivot.anchorMin = pivot.anchorMax = new Vector2(0.5f, 0.5f);
        pivot.pivot = new Vector2(0.5f, 0.5f);
        pivot.sizeDelta = new Vector2(radiusPixels * 2.2f, radiusPixels * 2.2f);

        var indicatorGo = new GameObject("DamageArrow");
        indicatorGo.transform.SetParent(pivotGo.transform, false);
        indicator = indicatorGo.AddComponent<Image>();
        indicator.raycastTarget = false;
        indicator.sprite = BuildArcSprite();
        indicator.color = new Color(indicatorColor.r, indicatorColor.g, indicatorColor.b, 0f);
        var rt = indicator.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1.7f);
        // Thinner / smaller — was 220x60, now 130x28 with a sharper chevron sprite.
        rt.sizeDelta = new Vector2(130f, 28f);
        rt.anchoredPosition = Vector2.zero;
    }

    static Sprite s_arcSprite;
    static Sprite BuildArcSprite()
    {
        if (s_arcSprite != null) return s_arcSprite;
        const int W = 128, H = 32;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            // Sharp thin chevron pointing up. v=0 is bottom; the line of the
            // chevron is where |u| * slope == (1 - v), so we measure distance
            // to that diagonal and gate it to a thin band.
            float u = (x / (float)(W - 1)) * 2f - 1f;     // -1..1
            float v = y / (float)(H - 1);                 // 0..1 (0 bottom)
            const float slope = 0.55f;
            float dist = Mathf.Abs((1f - v) - Mathf.Abs(u) * slope);
            // Thin stroke (~3 pixels) with a soft edge.
            float stroke = Mathf.Clamp01(1f - dist * 12f);
            // Fade out near the bottom and at the very top so ends taper.
            float taper = Mathf.SmoothStep(0f, 0.25f, v) * Mathf.SmoothStep(1f, 0.7f, v);
            float a = stroke * taper;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        s_arcSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
        s_arcSprite.name = "DamageDirectionArc";
        return s_arcSprite;
    }

    void OnDamage(float damage)
    {
        if (health == null) return;
        Vector3 src = health.HasLastDamageSource ? health.LastDamageSource : transform.position;
        activeHits.Add(new HitInstance { source = src, fireTime = Time.time });
        // Cap at most 4 simultaneous arrows — avoid unbounded growth on chain hits.
        if (activeHits.Count > 4) activeHits.RemoveAt(0);
    }

    void LateUpdate()
    {
        if (indicator == null) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // Show only the most recent visible hit at a time — multiple arrows on
        // the same canvas just blur into a red blob. Pick the most recent.
        // Drop expired entries.
        float total = visibleDuration + fadeDuration;
        for (int i = activeHits.Count - 1; i >= 0; i--)
        {
            if (Time.time - activeHits[i].fireTime > total) activeHits.RemoveAt(i);
        }

        if (activeHits.Count == 0)
        {
            var c = indicator.color; c.a = 0f; indicator.color = c;
            return;
        }

        var hit = activeHits[activeHits.Count - 1];
        float age = Time.time - hit.fireTime;
        float alpha = age <= visibleDuration
            ? 1f
            : Mathf.Clamp01(1f - (age - visibleDuration) / Mathf.Max(0.05f, fadeDuration));

        // Compute angle around the player on the XZ plane: 0 = forward, +90 = right.
        Vector3 toSource = hit.source - transform.position;
        toSource.y = 0f;
        if (toSource.sqrMagnitude < 0.0001f)
        {
            var c = indicator.color; c.a = 0f; indicator.color = c;
            return;
        }
        Vector3 fwd = mainCam.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) return;
        float angle = Vector3.SignedAngle(fwd, toSource, Vector3.up);

        // Pivot rotates around screen center; -angle so positive-right rotates
        // the arrow clockwise (Unity UI rotation is counterclockwise by default).
        if (pivot != null)
        {
            pivot.localEulerAngles = new Vector3(0f, 0f, -angle);
        }
        var col = indicatorColor; col.a = indicatorColor.a * alpha;
        indicator.color = col;
    }
}
