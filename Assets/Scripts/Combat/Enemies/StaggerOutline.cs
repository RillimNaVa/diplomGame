using UnityEngine;

// Phase 5 / PR 5.A — adds an HDR-orange outline halo when EnemyStagger fires.
// Implementation: appends an instance material using the
// VoidSurvivor/StaggerOutline inverted-hull shader to each renderer's
// materials array. When stagger ends or the enemy is pooled-returned, the
// extra slot is removed (sharedMaterials reset to original references).
//
// The outline material is per-instance (so HDR color/width can be tuned per
// enemy later via Inspector), but the shader itself is shared across enemies.
//
// Auto-attached by EnemyBrainBase.Awake.
public class StaggerOutline : MonoBehaviour
{
    [ColorUsage(true, true)]
    public Color outlineColor = new Color(3.0f, 0.3f, 0.1f);
    [Range(0.005f, 0.1f)] public float outlineWidth = 0.025f;
    public float pulseSpeed = 5f;

    EnemyStagger stagger;
    Renderer[] renderers;
    Material[][] originalSharedMaterials;
    Material[][] outlinedMaterials;
    Material outlineInstance;
    static Shader s_outlineShader;
    bool outlined;

    static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    static readonly int PulseSpeedId   = Shader.PropertyToID("_PulseSpeed");

    void Awake()
    {
        stagger = GetComponent<EnemyStagger>();
        renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers != null)
        {
            originalSharedMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) originalSharedMaterials[i] = renderers[i].sharedMaterials;
        }
    }

    void OnEnable()
    {
        if (stagger != null) stagger.OnStaggerChanged += HandleStaggerChanged;
    }

    void OnDisable()
    {
        if (stagger != null) stagger.OnStaggerChanged -= HandleStaggerChanged;
        RemoveOutline();
    }

    void HandleStaggerChanged(bool isStaggered)
    {
        if (isStaggered && !outlined) AddOutline();
        else if (!isStaggered && outlined) RemoveOutline();
    }

    public void ResetForPool()
    {
        RemoveOutline();
    }

    void AddOutline()
    {
        Shader sh = ResolveShader();
        if (sh == null || renderers == null) return;
        if (outlineInstance == null)
        {
            outlineInstance = new Material(sh);
            outlineInstance.name = "StaggerOutlineMat";
            outlineInstance.SetColor(OutlineColorId, outlineColor);
            outlineInstance.SetFloat(OutlineWidthId, outlineWidth);
            outlineInstance.SetFloat(PulseSpeedId, pulseSpeed);
        }
        // Build outlined materials arrays once per instance lifetime.
        if (outlinedMaterials == null || outlinedMaterials.Length != renderers.Length)
        {
            outlinedMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] originals = originalSharedMaterials[i];
                if (originals == null) continue;
                outlinedMaterials[i] = new Material[originals.Length + 1];
                for (int s = 0; s < originals.Length; s++) outlinedMaterials[i][s] = originals[s];
                outlinedMaterials[i][originals.Length] = outlineInstance;
            }
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && outlinedMaterials[i] != null)
                renderers[i].sharedMaterials = outlinedMaterials[i];
        }
        outlined = true;
    }

    void RemoveOutline()
    {
        if (renderers == null || originalSharedMaterials == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalSharedMaterials[i] != null)
                renderers[i].sharedMaterials = originalSharedMaterials[i];
        }
        outlined = false;
    }

    static Shader ResolveShader()
    {
        if (s_outlineShader != null) return s_outlineShader;
        s_outlineShader = Shader.Find("VoidSurvivor/StaggerOutline");
        return s_outlineShader;
    }

    void OnDestroy()
    {
        if (outlineInstance != null) Destroy(outlineInstance);
    }
}
