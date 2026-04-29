using UnityEngine;

// Phase 3 / PR 3.E + 5.B — ground decal that previews the Brute slam radius
// during Telegraph. Auto-spawned by BruteEnemyBrain; uses a primitive
// Cylinder scaled flat with the VoidSurvivor/SlamWarning HLSL shader so the
// rune reads as an animated countdown (sweeping arc + pulsing inner cross +
// rotating tick marks). Falls back to a transparent emissive material if
// the shader is missing.
//
// _Progress is animated 0 → 1 across telegraphTime; the sweep arc fills the
// outer band clockwise, pulse breathes, intensity ramps toward impact.
public class BruteSlamDecal : MonoBehaviour
{
    static readonly int ProgressId   = Shader.PropertyToID("_Progress");
    static readonly int BaseColorId  = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    Transform decal;
    Renderer decalRenderer;
    Material decalMat;
    bool usingWarningShader;
    float fullDuration;
    float startTime;
    bool active;

    public void Configure(float radius, Vector3 localOffset)
    {
        if (decal == null) BuildDecal();
        decal.localPosition = localOffset + new Vector3(0f, 0.05f, 0f); // 5 cm above feet
        // Cylinder primitive radius = 0.5 in object space → scale by radius * 2
        // to match the slamRadius in world units. Y kept flat at 5 cm.
        decal.localScale = new Vector3(radius * 2f, 0.025f, radius * 2f);
        decal.gameObject.SetActive(false);
    }

    void BuildDecal()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "SlamWarningDecal";
        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        decal = go.transform;
        decal.SetParent(transform, false);

        decalRenderer = go.GetComponent<Renderer>();
        // Try the new SlamWarning shader first; fall back to URP Lit transparent.
        Shader sh = Shader.Find("VoidSurvivor/SlamWarning");
        if (sh != null)
        {
            decalMat = new Material(sh);
            decalMat.name = "SlamWarningMat(Runtime)";
            decalMat.SetColor(BaseColorId, new Color(3f, 1f, 0.15f, 1f));
            decalMat.SetFloat(ProgressId, 0f);
            usingWarningShader = true;
        }
        else
        {
            // Fallback — keeps the old "static orange disk" look if shader is missing.
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");
            decalMat = new Material(urpLit);
            decalMat.SetFloat("_Surface", 1f);
            decalMat.SetFloat("_Blend", 0f);
            decalMat.SetFloat("_ZWrite", 0f);
            decalMat.renderQueue = 3000;
            decalMat.EnableKeyword("_EMISSION");
            decalMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            decalMat.SetColor(BaseColorId, new Color(1f, 0.45f, 0.05f, 0.45f));
            decalMat.SetColor(EmissionColorId, new Color(2.4f, 0.9f, 0.1f) * 1.5f);
            usingWarningShader = false;
        }
        decalRenderer.sharedMaterial = decalMat;
        decalRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        decalRenderer.receiveShadows = false;
    }

    public void Show(float duration)
    {
        if (decal == null) return;
        fullDuration = Mathf.Max(0.01f, duration);
        startTime = Time.time;
        active = true;
        decal.gameObject.SetActive(true);
        if (decalMat != null && usingWarningShader) decalMat.SetFloat(ProgressId, 0f);
    }

    public void Hide()
    {
        active = false;
        if (decal != null) decal.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!active || decalMat == null) return;
        float t = Mathf.Clamp01((Time.time - startTime) / fullDuration);

        if (usingWarningShader)
        {
            decalMat.SetFloat(ProgressId, t);
        }
        else
        {
            // Fallback path — old alpha + emission ramp behavior.
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 12f);
            Color baseC = new Color(1f, 0.45f, 0.05f, Mathf.Lerp(0.30f, 0.85f, t));
            decalMat.SetColor(BaseColorId, baseC);
            Color emi = new Color(2.4f, 0.9f, 0.1f) * Mathf.Lerp(1.2f, 3.0f, t) * (0.7f + 0.3f * pulse);
            decalMat.SetColor(EmissionColorId, emi);
        }
    }

    void OnDestroy()
    {
        if (decalMat != null) Destroy(decalMat);
    }
}
