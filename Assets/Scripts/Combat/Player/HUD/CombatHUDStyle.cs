using TMPro;
using UnityEngine;

// Centralized palette + procedural texture utilities for the combat HUD.
// Keeping this in one place lets us retune the look without touching block code.
public static class CombatHUDStyle
{
    // Base
    public static readonly Color Cyan = new Color32(0x5C, 0xE6, 0xFF, 0xFF);
    public static readonly Color CyanDim = new Color32(0x2A, 0x6E, 0x80, 0xFF);
    public static readonly Color White = new Color32(0xE8, 0xF4, 0xFF, 0xFF);
    public static readonly Color Outline = new Color32(0x12, 0x1B, 0x22, 0xC0);

    // Warning / state
    public static readonly Color WarnOrange = new Color32(0xFF, 0x95, 0x40, 0xFF);
    public static readonly Color WarnRed = new Color32(0xFF, 0x3A, 0x4C, 0xFF);

    // Heal
    public static readonly Color HealCyanGreen = new Color32(0x7F, 0xFF, 0xCB, 0xFF);

    // Cached procedural textures
    static Sprite s_white;
    static Sprite s_softCircle;
    static Sprite s_diamond;

    public static Sprite WhiteSprite()
    {
        if (s_white != null) return s_white;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(pixels);
        tex.Apply();
        s_white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        s_white.name = "HUD_White";
        return s_white;
    }

    public static Sprite SoftCircleSprite()
    {
        if (s_softCircle != null) return s_softCircle;
        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 c = new Vector2((N - 1) * 0.5f, (N - 1) * 0.5f);
        float r = (N - 1) * 0.5f;
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c) / r;
            float a = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(d));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        s_softCircle = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        s_softCircle.name = "HUD_SoftCircle";
        return s_softCircle;
    }

    // A diamond / chevron sprite useful for HP segments and dash pips.
    public static Sprite DiamondSprite()
    {
        if (s_diamond != null) return s_diamond;
        const int W = 32, H = 32;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float u = (x / (float)(W - 1)) * 2f - 1f;
            float v = (y / (float)(H - 1)) * 2f - 1f;
            float d = Mathf.Abs(u) + Mathf.Abs(v); // diamond metric
            float a = Mathf.SmoothStep(1f, 0.85f, d);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        s_diamond = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
        s_diamond.name = "HUD_Diamond";
        return s_diamond;
    }

    public static TMP_FontAsset DefaultFont()
    {
        return TMP_Settings.defaultFontAsset;
    }

    public static Color HpColorForFraction(float f01)
    {
        if (f01 > 0.6f) return Cyan;
        if (f01 > 0.3f) return WarnOrange;
        return WarnRed;
    }
}
