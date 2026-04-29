using UnityEngine;

// Phase 4 / PR 4.A — visible spawn-in for enemies. Replaces the instant pop
// when a fresh or pooled enemy enters the world: scale ramps from 0.55× to
// 1.0× over warpDuration and the renderers pulse an emissive flash.
//
// Triggered automatically:
//  - on the first OnEnable after Instantiate (fresh enemy).
//  - on PooledEnemy.PrepareForReuse → SetActive(true) (recycled rent).
//
// Both cases route through OnEnable, so we don't need a separate hook.
//
// Auto-attached by EnemyBrainBase.Awake so existing prefabs animate without
// Editor work.
public class SpawnWarpIn : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Tooltip("Seconds the warp-in animation runs.")]
    public float warpDuration = 0.4f;
    [Tooltip("Starting scale multiplier. Final scale is the prefab's authored scale.")]
    [Range(0.05f, 1f)] public float startScale = 0.55f;
    [Tooltip("HDR color of the spawn-in emissive pulse.")]
    [ColorUsage(true, true)]
    public Color pulseColor = new Color(0.6f, 1.4f, 1.8f);

    Renderer[] renderers;
    MaterialPropertyBlock mpb;
    Color[] baseEmission;
    Vector3 finalScale = Vector3.one;
    float startTime;
    bool warping;
    bool capturedScale;

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
        finalScale = transform.localScale;
        capturedScale = true;
    }

    void OnEnable()
    {
        // Capture finalScale once — Awake already did, but if PrepareForReuse
        // shrunk us (via a previous warp interrupted by death), restore.
        if (!capturedScale)
        {
            finalScale = transform.localScale;
            capturedScale = true;
        }
        startTime = Time.time;
        warping = true;
        // Snap immediately to the start state so the first rendered frame
        // is already small + flashing, not the full-size pop.
        transform.localScale = finalScale * startScale;
        ApplyEmission(1f);
    }

    void OnDisable()
    {
        // Restore so the next OnEnable starts from a clean baseline.
        warping = false;
        if (capturedScale) transform.localScale = finalScale;
        ApplyEmission(0f);
    }

    void Update()
    {
        if (!warping) return;
        float t = (Time.time - startTime) / Mathf.Max(0.05f, warpDuration);
        if (t >= 1f)
        {
            warping = false;
            transform.localScale = finalScale;
            ApplyEmission(0f);
            return;
        }

        // Smooth-step out of the small scale; emission decays from 1 -> 0.
        float ease = t * t * (3f - 2f * t);
        transform.localScale = Vector3.Lerp(finalScale * startScale, finalScale, ease);
        ApplyEmission(1f - ease);
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
            mpb.SetColor(EmissionColorId, intensity > 0f ? (baseE + pulseColor * intensity) : baseE);
            r.SetPropertyBlock(mpb);
        }
    }
}
