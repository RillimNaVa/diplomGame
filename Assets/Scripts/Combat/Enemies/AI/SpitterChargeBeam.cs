using UnityEngine;

// Phase 5 / PR 5.A — visible charge beam during the Plasma Spitter telegraph.
// Driven by RangedEnemyBrain: BeginCharge(target, duration) on entering the
// Telegraph state, EndCharge() on Attack/Recover/Stagger/Dead.
//
// Visual: thin LineRenderer from the muzzle to the player, alpha + thickness
// ramp 0 → max during the wind-up, then snap off at fire. Color is biome-
// agnostic plasma cyan with HDR boost so it punches through bloom.
//
// Auto-attached by RangedEnemyBrain.Awake.
[RequireComponent(typeof(EnemyBrainBase))]
public class SpitterChargeBeam : MonoBehaviour
{
    [ColorUsage(true, true)]
    public Color beamColor = new Color(0.8f, 2.4f, 3.2f);
    public float maxWidth = 0.07f;
    public float minWidth = 0.01f;
    [Tooltip("Local-space muzzle offset. Should match RangedEnemyBrain.muzzleOffset.")]
    public Vector3 muzzleOffset = new Vector3(0f, 1.0f, 0f);

    LineRenderer line;
    Transform target;
    float chargeStart;
    float chargeDuration;
    bool charging;

    static Material s_beamMaterial;

    void Awake()
    {
        BuildLine();
    }

    void OnDisable()
    {
        EndCharge();
    }

    void BuildLine()
    {
        if (line != null) return;
        var go = new GameObject("ChargeBeam");
        go.transform.SetParent(transform, false);
        line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.widthMultiplier = 0;
        line.startColor = beamColor;
        line.endColor = beamColor;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.alignment = LineAlignment.View;
        line.sharedMaterial = ResolveMaterial();
        line.enabled = false;
    }

    static Material ResolveMaterial()
    {
        if (s_beamMaterial != null) return s_beamMaterial;
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        s_beamMaterial = new Material(sh);
        s_beamMaterial.name = "SpitterChargeBeamMat(Runtime)";
        if (s_beamMaterial.HasProperty("_Surface")) s_beamMaterial.SetFloat("_Surface", 1f);
        if (s_beamMaterial.HasProperty("_Blend"))   s_beamMaterial.SetFloat("_Blend", 1f);   // additive
        if (s_beamMaterial.HasProperty("_ZWrite"))  s_beamMaterial.SetFloat("_ZWrite", 0f);
        return s_beamMaterial;
    }

    public void BeginCharge(Transform aimTarget, float duration)
    {
        if (line == null) BuildLine();
        target = aimTarget;
        chargeStart = Time.time;
        chargeDuration = Mathf.Max(0.05f, duration);
        charging = true;
        line.enabled = true;
    }

    public void EndCharge()
    {
        charging = false;
        if (line != null) line.enabled = false;
        target = null;
    }

    void LateUpdate()
    {
        if (!charging || line == null) return;
        if (target == null)
        {
            EndCharge();
            return;
        }

        float t = Mathf.Clamp01((Time.time - chargeStart) / chargeDuration);
        float ease = t * t;  // accelerating ramp — slow start, sharp peak

        // Thin line at the start, thick at the impact frame.
        line.widthMultiplier = Mathf.Lerp(minWidth, maxWidth, ease);

        // Color stays the same hue but ramps brightness via the material's
        // tint — LineRenderer uses startColor / endColor as multiplicative
        // tint on the material color when enabled.
        Color tinted = beamColor * Mathf.Lerp(0.5f, 1.5f, ease);
        line.startColor = tinted;
        line.endColor = tinted;

        Vector3 from = transform.position + transform.TransformVector(muzzleOffset);
        Vector3 to = target.position + Vector3.up * 1.0f;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
    }

    void OnDestroy()
    {
        if (line != null && line.gameObject != null) Destroy(line.gameObject);
    }
}
