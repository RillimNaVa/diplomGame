using System.Collections.Generic;
using UnityEngine;

// Phase 5 / PR 5.C — reactive ceiling-lamp flicker.
//
// Each ceiling fill-light gets a LampFlicker on Awake. All instances register
// in a static set so impact events (Brute slam, projectile detonation, future
// explosions) can call LampFlicker.NudgeAt(worldPos, radius) to make nearby
// lamps flicker for ~0.5s. Cheap: linear scan, max ~5 lamps per arena.
[RequireComponent(typeof(Light))]
public class LampFlicker : MonoBehaviour
{
    [Tooltip("Seconds the flicker animation runs after a nudge.")]
    public float flickerDuration = 0.55f;
    [Tooltip("Frequency of the noise-driven dip in Hz.")]
    public float flickerFrequency = 14f;
    [Tooltip("Lower bound of the intensity dip multiplier (0..1).")]
    [Range(0f, 1f)] public float minIntensityFactor = 0.05f;

    Light targetLight;
    float baselineIntensity;
    float flickerEndTime;
    float noisePhase;
    Renderer[] panelRenderers;
    MaterialPropertyBlock[] panelBlocks;
    Color[] panelBaseColors;
    Color[] panelEmissionColors;
    bool[] panelHasBaseColor;
    bool[] panelHasEmissionColor;
    bool panelVisualsResolved;

    static readonly HashSet<LampFlicker> ActiveLamps = new HashSet<LampFlicker>();

    void Awake()
    {
        targetLight = GetComponent<Light>();
        if (targetLight != null) baselineIntensity = targetLight.intensity;
        noisePhase = Random.value * 100f;
    }

    void Start()
    {
        ResolvePanelVisuals();
    }

    void OnEnable() { ActiveLamps.Add(this); }
    void OnDisable() { ActiveLamps.Remove(this); RestoreBaseline(); RestorePanelVisuals(); }

    /// <summary>
    /// PR 5.C — call from any source of physical impact (Brute slam,
    /// projectile detonation, future explosions). Lamps within `radius` from
    /// `worldPos` flicker for `flickerDuration`.
    /// </summary>
    public static void NudgeAt(Vector3 worldPos, float radius)
    {
        if (radius <= 0.001f) return;
        float r2 = radius * radius;
        foreach (var lamp in ActiveLamps)
        {
            if (lamp == null) continue;
            // Impacts happen near the floor while lamps sit near the ceiling.
            // Use horizontal XZ distance so a slam under a lamp affects it even
            // in tall arenas.
            Vector3 delta = lamp.transform.position - worldPos;
            delta.y = 0f;
            if (delta.sqrMagnitude > r2) continue;
            lamp.flickerEndTime = Time.time + lamp.flickerDuration;
        }
    }

    void Update()
    {
        if (targetLight == null) return;
        if (Time.time >= flickerEndTime)
        {
            if (Mathf.Abs(targetLight.intensity - baselineIntensity) > 0.001f)
            {
                targetLight.intensity = baselineIntensity;
            }
            RestorePanelVisuals();
            return;
        }

        // Noise-driven dip so the flicker reads as electrical instability,
        // not a clean sine wave.
        float t = Time.time * flickerFrequency + noisePhase;
        float noise = Mathf.PerlinNoise(t, 0.37f);
        // Sharp downward bias so most frames are bright with brief stutters.
        float dip = Mathf.SmoothStep(1f, minIntensityFactor, Mathf.Pow(noise, 0.4f));
        targetLight.intensity = baselineIntensity * dip;
        ApplyPanelFlicker(dip);
    }

    void RestoreBaseline()
    {
        if (targetLight != null) targetLight.intensity = baselineIntensity;
    }

    void ResolvePanelVisuals()
    {
        if (panelVisualsResolved) return;
        panelVisualsResolved = true;

        Transform parent = transform.parent;
        if (parent == null) return;

        string panelName = gameObject.name.Replace("FillLight_", "CeilingLamp_") + "_Panel";
        Transform panel = parent.Find(panelName);
        if (panel == null) return;

        panelRenderers = panel.GetComponentsInChildren<Renderer>();
        if (panelRenderers == null || panelRenderers.Length == 0) return;

        panelBlocks = new MaterialPropertyBlock[panelRenderers.Length];
        panelBaseColors = new Color[panelRenderers.Length];
        panelEmissionColors = new Color[panelRenderers.Length];
        panelHasBaseColor = new bool[panelRenderers.Length];
        panelHasEmissionColor = new bool[panelRenderers.Length];

        for (int i = 0; i < panelRenderers.Length; i++)
        {
            panelBlocks[i] = new MaterialPropertyBlock();
            Material mat = panelRenderers[i] != null ? panelRenderers[i].sharedMaterial : null;
            if (mat == null) continue;

            panelHasBaseColor[i] = mat.HasProperty("_BaseColor");
            if (panelHasBaseColor[i]) panelBaseColors[i] = mat.GetColor("_BaseColor");

            panelHasEmissionColor[i] = mat.HasProperty("_EmissionColor");
            if (panelHasEmissionColor[i]) panelEmissionColors[i] = mat.GetColor("_EmissionColor");
        }
    }

    void ApplyPanelFlicker(float factor)
    {
        ResolvePanelVisuals();
        if (panelRenderers == null) return;

        float visibleFactor = Mathf.Lerp(0.2f, 1f, factor);
        for (int i = 0; i < panelRenderers.Length; i++)
        {
            Renderer rend = panelRenderers[i];
            if (rend == null || panelBlocks[i] == null) continue;

            rend.GetPropertyBlock(panelBlocks[i]);
            if (panelHasBaseColor[i]) panelBlocks[i].SetColor("_BaseColor", panelBaseColors[i] * visibleFactor);
            if (panelHasEmissionColor[i]) panelBlocks[i].SetColor("_EmissionColor", panelEmissionColors[i] * visibleFactor);
            rend.SetPropertyBlock(panelBlocks[i]);
        }
    }

    void RestorePanelVisuals()
    {
        if (panelRenderers == null) return;

        for (int i = 0; i < panelRenderers.Length; i++)
        {
            Renderer rend = panelRenderers[i];
            if (rend == null || panelBlocks[i] == null) continue;

            rend.GetPropertyBlock(panelBlocks[i]);
            if (panelHasBaseColor[i]) panelBlocks[i].SetColor("_BaseColor", panelBaseColors[i]);
            if (panelHasEmissionColor[i]) panelBlocks[i].SetColor("_EmissionColor", panelEmissionColors[i]);
            rend.SetPropertyBlock(panelBlocks[i]);
        }
    }
}
