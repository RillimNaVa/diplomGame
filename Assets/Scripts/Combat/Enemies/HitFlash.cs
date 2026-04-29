using UnityEngine;

// Phase 4 / PR 4.A — combat readability layer.
// Briefly tints every Renderer in the enemy hierarchy white when the enemy
// takes damage. Pure visual, no gameplay impact. Same MPB-based pattern as
// TelegraphFlash so it composes correctly: HitFlash overrides emission for a
// few frames; TelegraphFlash resumes its pulse once HitFlash decays.
//
// Auto-attached by EnemyBrainBase.Awake (added there in this PR) so existing
// prefabs get hit feedback without Editor work.
public class HitFlash : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Flash")]
    [Tooltip("Color of the hit flash. White reads as 'damage taken' on every biome.")]
    [ColorUsage(true, true)]
    public Color flashColor = new Color(3f, 3f, 3f);
    [Tooltip("Seconds. Short — too long fights TelegraphFlash and EnemyStagger.")]
    public float flashDuration = 0.07f;

    Renderer[] renderers;
    MaterialPropertyBlock mpb;
    Color[] baseEmission;
    bool flashing;
    float flashStart;
    Health health;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();
        baseEmission = new Color[renderers != null ? renderers.Length : 0];
        for (int i = 0; i < baseEmission.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r.sharedMaterial == null) { baseEmission[i] = Color.black; continue; }
            baseEmission[i] = r.sharedMaterial.HasProperty(EmissionColorId)
                ? r.sharedMaterial.GetColor(EmissionColorId)
                : Color.black;
        }
        health = GetComponent<Health>();
    }

    void OnEnable()
    {
        if (health != null) health.onTakeDamage.AddListener(OnDamage);
    }

    void OnDisable()
    {
        if (health != null) health.onTakeDamage.RemoveListener(OnDamage);
        flashing = false;
        ApplyEmission(0f);
    }

    void OnDamage(float damage)
    {
        flashing = true;
        flashStart = Time.time;
    }

    void Update()
    {
        if (!flashing) return;
        float t = (Time.time - flashStart) / Mathf.Max(0.001f, flashDuration);
        if (t >= 1f)
        {
            flashing = false;
            ApplyEmission(0f);
            return;
        }
        // Sharp on, fast decay — ease-out cubic gives "pop" feel.
        float intensity = 1f - t * t * t;
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
            Color baseE = i < baseEmission.Length ? baseEmission[i] : Color.black;
            // Set, not add — overrides whatever TelegraphFlash put there for the
            // few frames the hit flash is active. TelegraphFlash will repaint
            // next frame after flashing flips false.
            mpb.SetColor(EmissionColorId, intensity > 0f ? (baseE + flashColor * intensity) : baseE);
            r.SetPropertyBlock(mpb);
        }
    }
}
