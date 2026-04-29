using System.Collections;
using UnityEngine;

// Phase 5 / PR 5.A — pre-spawn warning marker.
// Plays for ~0.7s at the chosen spawn point BEFORE the enemy is rented from
// the pool, so the player has time to react. Two visual layers:
//
//  1. Floor circle — flat Cylinder primitive scaled wide+thin, with an
//     animated emissive material that pulses + scales 0.4× → 1.05×.
//  2. Vertical beam — thin tall Cylinder from the floor up to the ceiling
//     with alpha that fades-in then fades-out as the enemy materializes.
//
// Spawned via SpawnTelegraph.SpawnAt(worldPos, duration). Auto-destructs on
// completion. Pure runtime — no prefab asset / authoring required.
public class SpawnTelegraph : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId     = Shader.PropertyToID("_BaseColor");

    [ColorUsage(true, true)]
    public Color color = new Color(0.4f, 1.6f, 2.4f);
    public float circleRadius = 1.2f;
    public float beamHeight = 5.5f;

    Transform circle;
    Transform beam;
    Material circleMat;
    Material beamMat;
    float startTime;
    float fullDuration;

    /// <summary>
    /// Convenience entry point — creates a SpawnTelegraph GameObject at
    /// worldPos, animates it for `duration` seconds, then auto-destroys.
    /// </summary>
    public static SpawnTelegraph SpawnAt(Vector3 worldPos, float duration, Color? tint = null)
    {
        var go = new GameObject("SpawnTelegraph");
        go.transform.position = worldPos;
        var tg = go.AddComponent<SpawnTelegraph>();
        if (tint.HasValue) tg.color = tint.Value;
        tg.Begin(duration);
        return tg;
    }

    public void Begin(float duration)
    {
        fullDuration = Mathf.Max(0.1f, duration);
        startTime = Time.time;
        BuildCircle();
        BuildBeam();
        StartCoroutine(LifecycleCoroutine());
    }

    void BuildCircle()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "TelegraphCircle";
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        circle = go.transform;
        circle.SetParent(transform, false);
        circle.localPosition = new Vector3(0f, 0.04f, 0f);
        circle.localScale = new Vector3(circleRadius * 2f, 0.02f, circleRadius * 2f);

        circleMat = MakeTransparentEmissive(color, "TelegraphCircleMat");
        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = circleMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    void BuildBeam()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "TelegraphBeam";
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        beam = go.transform;
        beam.SetParent(transform, false);
        // Cylinder primitive default height = 2m → scale Y by beamHeight/2.
        // Position pivot at the midpoint of the beam.
        beam.localPosition = new Vector3(0f, beamHeight * 0.5f, 0f);
        beam.localScale = new Vector3(0.18f, beamHeight * 0.5f, 0.18f);

        beamMat = MakeTransparentEmissive(color * 0.7f, "TelegraphBeamMat");
        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = beamMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    static Material MakeTransparentEmissive(Color c, string name)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        m.name = name;
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend"))   m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_ZWrite"))  m.SetFloat("_ZWrite", 0f);
        m.renderQueue = 3050;
        m.EnableKeyword("_EMISSION");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetColor(BaseColorId, new Color(c.r * 0.4f, c.g * 0.4f, c.b * 0.4f, 0.55f));
        m.SetColor(EmissionColorId, c * 1.4f);
        return m;
    }

    IEnumerator LifecycleCoroutine()
    {
        // The animation loop runs here; auto-destroy when fullDuration elapses
        // plus a small post-spawn fade-out tail.
        while (Time.time - startTime < fullDuration + 0.25f)
        {
            float t = Mathf.Clamp01((Time.time - startTime) / fullDuration);
            UpdateVisuals(t);
            yield return null;
        }
        Destroy(gameObject);
    }

    void UpdateVisuals(float t)
    {
        // Phase 0..0.85: ramp up. Phase 0.85..1.0: ramp down (enemy materializes,
        // telegraph fades out).
        float intensity;
        float scaleFactor;
        if (t < 0.85f)
        {
            float u = t / 0.85f;
            float ease = u * u * (3f - 2f * u);
            intensity = Mathf.Lerp(0.3f, 1.6f, ease);
            scaleFactor = Mathf.Lerp(0.4f, 1.05f, ease);
        }
        else
        {
            float u = (t - 0.85f) / 0.15f;
            intensity = Mathf.Lerp(1.6f, 0f, u);
            scaleFactor = Mathf.Lerp(1.05f, 1.15f, u);  // small overshoot at the end
        }

        // Circle: scale + emissive ramp + slow rotation.
        if (circle != null)
        {
            circle.localScale = new Vector3(circleRadius * 2f * scaleFactor, 0.02f, circleRadius * 2f * scaleFactor);
            circle.localRotation = Quaternion.Euler(0f, (Time.time - startTime) * 60f, 0f);
        }
        if (circleMat != null)
        {
            circleMat.SetColor(EmissionColorId, color * intensity * 1.5f);
            Color baseC = circleMat.GetColor(BaseColorId);
            baseC.a = 0.55f * intensity;
            circleMat.SetColor(BaseColorId, baseC);
        }

        // Beam: same alpha/emission ramp, no scale.
        if (beamMat != null)
        {
            beamMat.SetColor(EmissionColorId, color * intensity * 1.0f);
            Color baseC = beamMat.GetColor(BaseColorId);
            baseC.a = 0.4f * intensity;
            beamMat.SetColor(BaseColorId, baseC);
        }
    }

    void OnDestroy()
    {
        if (circleMat != null) Destroy(circleMat);
        if (beamMat != null) Destroy(beamMat);
    }
}
