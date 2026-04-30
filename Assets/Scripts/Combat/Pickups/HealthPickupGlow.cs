using UnityEngine;

// Phase 5 / PR 5.C — auto-attached visual upgrade for HealthPickup. Replaces
// the orb's MeshRenderer material with the VoidSurvivor/PickupGlow shader and
// adds a slow scale pulse so the pickup reads as energetic from across the
// arena. Falls back to a plain emissive material if the shader is missing.
[RequireComponent(typeof(HealthPickup))]
public class HealthPickupGlow : MonoBehaviour
{
    [Tooltip("HDR color the orb glows. Default reads as 'green = heal'.")]
    [ColorUsage(true, true)]
    public Color glowColor = new Color(0.4f, 1.6f, 0.7f);
    [Tooltip("Scale pulse amplitude (fraction of base scale).")]
    public float pulseAmplitude = 0.07f;
    [Tooltip("Pulse speed in Hz.")]
    public float pulseSpeed = 1.8f;

    Vector3 baseScale;
    Renderer rend;
    static Material s_glowMaterial;

    void Awake()
    {
        baseScale = transform.localScale;
        rend = GetComponentInChildren<Renderer>();
        if (rend != null) rend.sharedMaterial = ResolveMaterial();
    }

    void Update()
    {
        float k = 1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * pulseAmplitude;
        transform.localScale = baseScale * k;
    }

    Material ResolveMaterial()
    {
        if (s_glowMaterial != null) return s_glowMaterial;
        Shader sh = Shader.Find("VoidSurvivor/PickupGlow");
        if (sh == null)
        {
            // Fallback: emissive Lit material so the orb is at least visible
            // in a stripped build that lost the shader.
            Shader fallback = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            s_glowMaterial = new Material(fallback) { name = "PickupGlow(Fallback)" };
            if (s_glowMaterial.HasProperty("_BaseColor")) s_glowMaterial.SetColor("_BaseColor", glowColor);
            if (s_glowMaterial.HasProperty("_EmissionColor"))
            {
                s_glowMaterial.EnableKeyword("_EMISSION");
                s_glowMaterial.SetColor("_EmissionColor", glowColor * 2f);
            }
            return s_glowMaterial;
        }
        s_glowMaterial = new Material(sh) { name = "PickupGlow(Runtime)" };
        s_glowMaterial.SetColor("_BaseColor", new Color(glowColor.r * 0.35f, glowColor.g * 0.35f, glowColor.b * 0.35f, 0.85f));
        s_glowMaterial.SetColor("_RimColor", glowColor * 1.8f);
        s_glowMaterial.SetColor("_BeamColor", glowColor * 2.6f);
        return s_glowMaterial;
    }
}
