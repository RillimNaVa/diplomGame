using UnityEngine;

// Phase 5 / PR 5.A — visible body-fade death animation.
// On Health.onDeath the component swaps every Renderer's sharedMaterials to
// instance materials using the VoidSurvivor/EnemyDissolve shader and ramps
// _DissolveAmount 0 → 1 over dissolveDuration. The original sharedMaterials
// arrays are cached so PooledEnemy.PrepareForReuse can restore them on the
// next rent (calls ResetForPool here).
//
// Existing _BaseColor / _BaseMap / _EmissionColor are copied from each
// renderer's first source material so the dissolving body keeps its tint.
//
// Auto-attached by EnemyBrainBase.Awake — no Editor work needed for existing
// prefabs. Falls back to a no-op silently if the dissolve shader is missing.
public class EnemyDissolve : MonoBehaviour
{
    static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    static readonly int BaseColorId      = Shader.PropertyToID("_BaseColor");
    static readonly int BaseMapId        = Shader.PropertyToID("_BaseMap");
    static readonly int EmissionColorId  = Shader.PropertyToID("_EmissionColor");

    [Tooltip("Seconds to ramp _DissolveAmount 0 → 1.05 (slight overshoot so the last pixels disappear cleanly).")]
    public float dissolveDuration = 1.0f;
    [Tooltip("Noise scale on the dissolve edge. Larger = finer grain.")]
    public float noiseScale = 4.5f;
    [Tooltip("HDR color of the burning leading edge.")]
    [ColorUsage(true, true)]
    public Color edgeColor = new Color(3.2f, 1.2f, 0.2f);

    Health health;
    Renderer[] renderers;
    Material[][] originalSharedMaterials;
    Material[][] dissolveInstances;
    static Shader s_dissolveShader;

    bool dissolving;
    float dissolveStart;

    void Awake()
    {
        health = GetComponent<Health>();
        renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers != null)
        {
            originalSharedMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) originalSharedMaterials[i] = renderers[i].sharedMaterials;
            }
        }
    }

    void OnEnable()
    {
        if (health != null) health.onDeath.AddListener(OnDeath);
    }

    void OnDisable()
    {
        if (health != null) health.onDeath.RemoveListener(OnDeath);
        // Don't restore here — PooledEnemy.PrepareForReuse calls ResetForPool
        // on rent, and OnDisable also fires when the GO is pooled-returned;
        // restoring here would briefly show the original material before the
        // next rent's dissolve reset.
        dissolving = false;
    }

    void OnDeath()
    {
        if (renderers == null || renderers.Length == 0) return;
        Shader sh = ResolveShader();
        if (sh == null) return;

        BuildDissolveInstances(sh);
        ApplyDissolveMaterials();
        SetDissolveAmount(0f);
        dissolving = true;
        dissolveStart = Time.time;
    }

    void Update()
    {
        if (!dissolving) return;
        float t = (Time.time - dissolveStart) / Mathf.Max(0.05f, dissolveDuration);
        SetDissolveAmount(Mathf.Min(1.05f, t));
        if (t >= 1.05f) dissolving = false;
    }

    public void ResetForPool()
    {
        dissolving = false;
        if (originalSharedMaterials == null || renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalSharedMaterials[i] != null)
                renderers[i].sharedMaterials = originalSharedMaterials[i];
        }
    }

    static Shader ResolveShader()
    {
        if (s_dissolveShader != null) return s_dissolveShader;
        s_dissolveShader = Shader.Find("VoidSurvivor/EnemyDissolve");
        return s_dissolveShader;
    }

    void BuildDissolveInstances(Shader sh)
    {
        if (dissolveInstances != null) return;  // built once, reused for the rest of this instance's lifetime
        dissolveInstances = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            Material[] originals = originalSharedMaterials[i];
            if (originals == null || originals.Length == 0) continue;
            dissolveInstances[i] = new Material[originals.Length];
            for (int s = 0; s < originals.Length; s++)
            {
                Material src = originals[s];
                Material m = new Material(sh);
                m.name = src != null ? $"{src.name}_Dissolve" : "EnemyDissolveMat";
                if (src != null)
                {
                    if (src.HasProperty(BaseColorId))     m.SetColor(BaseColorId,     src.GetColor(BaseColorId));
                    if (src.HasProperty(BaseMapId))       m.SetTexture(BaseMapId,     src.GetTexture(BaseMapId));
                    if (src.HasProperty(EmissionColorId)) m.SetColor(EmissionColorId, src.GetColor(EmissionColorId));
                }
                m.SetFloat("_NoiseScale", noiseScale);
                m.SetColor("_DissolveEdgeColor", edgeColor);
                m.SetFloat(DissolveAmountId, 0f);
                dissolveInstances[i][s] = m;
            }
        }
    }

    void ApplyDissolveMaterials()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && dissolveInstances[i] != null)
                renderers[i].sharedMaterials = dissolveInstances[i];
        }
    }

    void SetDissolveAmount(float v)
    {
        if (dissolveInstances == null) return;
        for (int i = 0; i < dissolveInstances.Length; i++)
        {
            Material[] mats = dissolveInstances[i];
            if (mats == null) continue;
            for (int s = 0; s < mats.Length; s++)
            {
                if (mats[s] != null) mats[s].SetFloat(DissolveAmountId, v);
            }
        }
    }

    void OnDestroy()
    {
        if (dissolveInstances == null) return;
        for (int i = 0; i < dissolveInstances.Length; i++)
        {
            Material[] mats = dissolveInstances[i];
            if (mats == null) continue;
            for (int s = 0; s < mats.Length; s++)
                if (mats[s] != null) Destroy(mats[s]);
        }
    }
}
