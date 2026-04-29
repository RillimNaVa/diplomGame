using UnityEngine;

// Phase 4 / PR 4.A + 5.B — Brute slam impact shockwave.
// Spawned by BruteEnemyBrain.TickAttack at the moment damage is applied.
// Uses the VoidSurvivor/SlamShockwave HLSL shader: hot core + expanding
// primary ring + trailing secondary ring + radial cracks. Falls back to a
// transparent emissive primitive if the shader is missing so a stripped
// build still shows *something* on impact.
//
// _Progress is animated 0 → 1.05 over `lifetime` seconds (default 0.45s),
// then the GameObject auto-destroys.
public class SlamImpactRing : MonoBehaviour
{
    static readonly int ProgressId  = Shader.PropertyToID("_Progress");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int CrackColorId = Shader.PropertyToID("_CrackColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    float radius;
    float lifetime = 0.45f;
    float startTime;
    Material mat;
    bool usingShockwaveShader;

    public static void Spawn(Vector3 worldPos, float slamRadius)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "SlamImpactRing";
        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        // 6 cm above ground to render above the warning decal which sits at 5 cm.
        go.transform.position = worldPos + new Vector3(0f, 0.06f, 0f);

        SlamImpactRing ring = go.AddComponent<SlamImpactRing>();
        ring.radius = slamRadius;
        ring.startTime = Time.time;
        ring.BuildMaterial();
        // The shockwave shader animates expansion via _Progress, so the disc
        // mesh stays at full slam radius — no localScale animation needed.
        go.transform.localScale = new Vector3(slamRadius * 2f, 0.025f, slamRadius * 2f);
    }

    void BuildMaterial()
    {
        Renderer r = GetComponent<Renderer>();
        if (r == null) return;

        Shader sh = Shader.Find("VoidSurvivor/SlamShockwave");
        if (sh != null)
        {
            mat = new Material(sh);
            mat.name = "SlamShockwaveMat(Runtime)";
            mat.SetColor(BaseColorId, new Color(3.5f, 1.5f, 0.3f, 1f));
            mat.SetColor(CrackColorId, new Color(5.0f, 2.5f, 0.6f, 1f));
            mat.SetFloat(ProgressId, 0f);
            usingShockwaveShader = true;
        }
        else
        {
            // Fallback: simple expanding emissive cylinder (legacy PR 4.A behavior).
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");
            mat = new Material(urpLit);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = 3001;
            mat.EnableKeyword("_EMISSION");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetColor(BaseColorId, new Color(1f, 0.7f, 0.2f, 0.85f));
            mat.SetColor(EmissionColorId, new Color(3.5f, 1.6f, 0.3f) * 2.5f);
            usingShockwaveShader = false;
        }
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    void Update()
    {
        float t = (Time.time - startTime) / lifetime;
        if (t >= 1.05f)
        {
            if (mat != null) Destroy(mat);
            Destroy(gameObject);
            return;
        }

        if (mat == null) return;

        if (usingShockwaveShader)
        {
            // Drive the shader. _Progress 0 → 1.05 grows the ring + cracks
            // outward; intensity falloff happens inside the shader after 0.7.
            mat.SetFloat(ProgressId, t);
        }
        else
        {
            // Fallback: legacy scale-up + emission ramp animation.
            float scale = Mathf.Lerp(0.4f, 1.05f, t) * radius * 2f;
            transform.localScale = new Vector3(scale, 0.025f, scale);
            float alpha = Mathf.Lerp(0.9f, 0f, t);
            float emi = Mathf.Lerp(3.5f, 0f, t);
            mat.SetColor(BaseColorId, new Color(1f, 0.7f, 0.2f, alpha));
            mat.SetColor(EmissionColorId, new Color(3.5f, 1.6f, 0.3f) * emi);
        }
    }
}
