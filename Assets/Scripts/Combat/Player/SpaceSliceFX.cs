using UnityEngine;

// White "space slice" line drawn through a target. Procedural — no assets.
// Self-destructs after the fade completes.
public class SpaceSliceFX : MonoBehaviour
{
    public float length = 6f;
    public float startWidth = 0.45f;
    public float endWidth = 0.02f;
    public float lifetime = 0.35f;
    public float holdFraction = 0.15f; // fraction of lifetime at full alpha
    public Color color = Color.white;

    LineRenderer lr;
    float age;

    public static SpaceSliceFX Spawn(Vector3 center, Vector3 axis, float length = 6f, float lifetime = 0.35f)
    {
        var go = new GameObject("SpaceSliceFX");
        go.transform.position = center;
        var fx = go.AddComponent<SpaceSliceFX>();
        fx.length = length;
        fx.lifetime = lifetime;
        fx.Init(center, axis.normalized);
        return fx;
    }

    void Init(Vector3 center, Vector3 axis)
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        Vector3 a = center - axis * (length * 0.5f);
        Vector3 b = center + axis * (length * 0.5f);
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth = startWidth;
        lr.endWidth = startWidth;
        lr.numCapVertices = 2;

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        lr.material = mat;
        lr.startColor = color;
        lr.endColor = color;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(age / Mathf.Max(0.0001f, lifetime));

        float alpha;
        float widthLerp;
        if (t < holdFraction)
        {
            alpha = 1f;
            widthLerp = 0f;
        }
        else
        {
            float u = (t - holdFraction) / (1f - holdFraction);
            alpha = 1f - u;
            widthLerp = u;
        }

        if (lr != null)
        {
            float w = Mathf.Lerp(startWidth, endWidth, widthLerp);
            lr.startWidth = w;
            lr.endWidth = w;
            var c = color; c.a = alpha;
            lr.startColor = c;
            lr.endColor = c;
        }

        if (age >= lifetime) Destroy(gameObject);
    }
}
