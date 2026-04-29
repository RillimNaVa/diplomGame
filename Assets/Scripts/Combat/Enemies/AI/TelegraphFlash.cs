using UnityEngine;

// Phase 3 / PR 3.E — placeholder telegraph visual (TZ §8.1).
// Pulses the emission color on every Renderer in the enemy hierarchy via a
// MaterialPropertyBlock so we don't allocate runtime material instances.
// Auto-discovered (or auto-attached) by EnemyBrainBase; brains call BeginPulse
// when entering Telegraph and EndPulse on Attack/Recover/Stagger/Dead.
public class TelegraphFlash : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    Renderer[] renderers;
    MaterialPropertyBlock mpb;
    Color[] baseEmission;
    bool pulsing;
    float pulseStart;
    float pulseDuration;
    Color pulseColor = new Color(1.6f, 0.6f, 0.0f);  // HDR orange default
    [Tooltip("Hz. Higher = faster strobe. 6Hz reads as 'wind-up' without epilepsy.")]
    public float pulseFrequency = 6f;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();
        CacheBaseEmission();
    }

    void CacheBaseEmission()
    {
        if (renderers == null) return;
        baseEmission = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r.sharedMaterial == null) { baseEmission[i] = Color.black; continue; }
            baseEmission[i] = r.sharedMaterial.HasProperty(EmissionColorId)
                ? r.sharedMaterial.GetColor(EmissionColorId)
                : Color.black;
        }
    }

    public void BeginPulse(float duration, Color color)
    {
        if (duration <= 0f) return;
        pulseDuration = duration;
        pulseStart = Time.time;
        pulseColor = color;
        pulsing = true;
    }

    public void EndPulse()
    {
        if (!pulsing) return;
        pulsing = false;
        ApplyEmission(0f);
    }

    void Update()
    {
        if (!pulsing) return;

        float elapsed = Time.time - pulseStart;
        if (elapsed >= pulseDuration)
        {
            // Hold full color on the very last frame so the impact still reads,
            // then EndPulse will be called by the brain on Attack/Recover.
            ApplyEmission(1f);
            return;
        }

        // 0.4 + 0.6*|sin| so emission never fully drops to base mid-wind-up.
        float t = elapsed / pulseDuration;
        float strobe = Mathf.Abs(Mathf.Sin(elapsed * pulseFrequency * Mathf.PI));
        float intensity = 0.4f + 0.6f * strobe;
        // Ramp the strobe ceiling toward 1.0 across the wind-up so the flash
        // peaks right before impact — readability of "now!".
        intensity *= Mathf.Lerp(0.6f, 1.0f, t);
        ApplyEmission(intensity);
    }

    void ApplyEmission(float intensity)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            Color baseE = baseEmission != null && i < baseEmission.Length ? baseEmission[i] : Color.black;
            Color tinted = baseE + pulseColor * intensity;
            mpb.SetColor(EmissionColorId, tinted);
            r.SetPropertyBlock(mpb);
        }
    }
}
