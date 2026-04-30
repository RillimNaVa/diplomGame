using UnityEngine;
using UnityEngine.UI;

// Phase 5 / PR 5.C follow-up: lightweight speed feedback for dash and slide.
// No custom URP render pass: just a short FOV kick, screen-edge flash, and
// camera-local stretch particles that read as speed lines.
public class SpeedBurstFeedback : MonoBehaviour
{
    [Header("FOV")]
    public float dashFovKick = 3.2f;
    public float slideFovKick = 1.6f;
    public float fovReturnSpeed = 22f;

    [Header("Screen Edge Pulse")]
    public Color dashEdgeColor = new Color(0.55f, 0.9f, 1f, 0.11f);
    public Color slideEdgeColor = new Color(0.75f, 0.95f, 1f, 0.06f);

    [Header("Speed Lines")]
    public int dashLineBurst = 26;
    public int slideLineBurst = 12;

    Camera playerCamera;
    Transform cameraTransform;
    Canvas canvas;
    Image edgePulse;
    ParticleSystem speedLines;
    ParticleSystemRenderer speedLineRenderer;
    Material speedLineMaterial;

    float baseFov;
    float pulseStart;
    float pulseDuration;
    float activeFovKick;
    Color activeEdgeColor;

    public void Configure(Transform camTransform)
    {
        cameraTransform = camTransform;
        playerCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : Camera.main;
        if (playerCamera != null)
        {
            baseFov = playerCamera.fieldOfView;
        }

        BuildEdgePulse();
        BuildSpeedLines();
    }

    void Awake()
    {
        if (cameraTransform == null)
        {
            var controller = GetComponent<PlayerController>();
            if (controller != null) cameraTransform = controller.cameraTransform;
        }

        Configure(cameraTransform);
    }

    void OnDisable()
    {
        if (playerCamera != null) playerCamera.fieldOfView = baseFov;
        if (edgePulse != null)
        {
            Color c = edgePulse.color;
            c.a = 0f;
            edgePulse.color = c;
        }
    }

    public void PulseDash(float duration)
    {
        Trigger(Mathf.Max(0.10f, duration * 0.75f), dashFovKick, dashEdgeColor, dashLineBurst);
    }

    public void PulseSlide(float duration)
    {
        Trigger(Mathf.Clamp(duration * 0.22f, 0.12f, 0.22f), slideFovKick, slideEdgeColor, slideLineBurst);
    }

    void Trigger(float duration, float fovKick, Color edgeColor, int lineCount)
    {
        if (playerCamera == null || edgePulse == null || speedLines == null)
        {
            Configure(cameraTransform);
        }

        if (playerCamera != null && Time.time > pulseStart + pulseDuration)
        {
            baseFov = playerCamera.fieldOfView;
        }

        pulseStart = Time.time;
        pulseDuration = Mathf.Max(0.05f, duration);
        activeFovKick = fovKick;
        activeEdgeColor = edgeColor;

        if (speedLines != null)
        {
            speedLines.Clear(true);
            speedLines.Emit(Mathf.Max(0, lineCount));
        }
    }

    void LateUpdate()
    {
        if (playerCamera == null || edgePulse == null) return;

        float age = Time.time - pulseStart;
        float t = pulseDuration > 0f ? Mathf.Clamp01(age / pulseDuration) : 1f;
        float remaining = 1f - t;
        float ease = remaining * remaining;

        float targetFov = baseFov + activeFovKick * ease;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, fovReturnSpeed * Time.deltaTime);

        Color c = activeEdgeColor;
        c.a *= ease;
        edgePulse.color = c;
    }

    void BuildEdgePulse()
    {
        if (canvas != null) return;

        var canvasGo = new GameObject("SpeedBurstCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4300;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        var imageGo = new GameObject("SpeedBurstEdgePulse");
        imageGo.transform.SetParent(canvasGo.transform, false);
        edgePulse = imageGo.AddComponent<Image>();
        edgePulse.raycastTarget = false;
        edgePulse.sprite = BuildEdgeSprite();
        edgePulse.color = new Color(1f, 1f, 1f, 0f);
        RectTransform rt = edgePulse.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Sprite edgeSprite;
    static Sprite BuildEdgeSprite()
    {
        if (edgeSprite != null) return edgeSprite;

        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));
            Vector2 d = uv - center;
            float radial = d.magnitude * 2f;
            float edge = Mathf.SmoothStep(0.78f, 1.0f, radial);
            float alpha = Mathf.Clamp01(edge * 0.22f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        tex.Apply();

        edgeSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        edgeSprite.name = "SpeedBurstEdgeSprite";
        return edgeSprite;
    }

    void BuildSpeedLines()
    {
        if (speedLines != null || cameraTransform == null) return;

        var go = new GameObject("SpeedLinesFX");
        go.transform.SetParent(cameraTransform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.75f);
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        speedLines = go.AddComponent<ParticleSystem>();
        speedLines.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        speedLineRenderer = go.GetComponent<ParticleSystemRenderer>();
        speedLineMaterial = BuildSpeedLineMaterial();
        if (speedLineRenderer != null)
        {
            speedLineRenderer.sharedMaterial = speedLineMaterial;
            speedLineRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            speedLineRenderer.velocityScale = 0.08f;
            speedLineRenderer.lengthScale = 1.15f;
            speedLineRenderer.cameraVelocityScale = 0f;
        }

        var main = speedLines.main;
        main.playOnAwake = false;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(9f, 16f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.018f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.9f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.16f));
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 48;

        var emission = speedLines.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = speedLines.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 42f;
        shape.radius = 0.62f;
        shape.length = 0.05f;

        var colorOverLifetime = speedLines.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.7f, 0.95f, 1f), 0f),
                new GradientColorKey(Color.white, 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.18f, 0.12f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);
    }

    static Material BuildSpeedLineMaterial()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh) { name = "SpeedLines(Runtime)" };
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1f);
        return mat;
    }
}
