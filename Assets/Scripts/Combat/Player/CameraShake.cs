using UnityEngine;

// Phase 4 / PR 4.A - additive camera shake on top of existing camera offsets.
// Auto-attaches to Camera.main when first AddTrauma() is called, so callers
// (PlayerHitFeedback, Brute slam impact) do not need Inspector wiring.
//
// PlayerController writes cameraTransform.localPosition.y in HandleMovement
// (slide camera dip). We remove the previous frame's shake before gameplay
// Update logic runs, then apply the current frame in LateUpdate.
[DefaultExecutionOrder(-1000)]
public class CameraShake : MonoBehaviour
{
    static CameraShake s_instance;

    [Tooltip("Maximum positional shake in meters at trauma=1.")]
    public float maxOffset = 0.18f;
    [Tooltip("Maximum rotational shake in degrees at trauma=1.")]
    public float maxRotation = 1.6f;
    [Tooltip("Trauma decays linearly per second. Higher = shorter shake.")]
    public float decayPerSecond = 4.5f;
    [Tooltip("Trauma is squared before applying - gives nicer falloff curve.")]
    public bool squareTrauma = true;

    float trauma;
    Vector3 lastOffset;
    Quaternion lastRotationOffset = Quaternion.identity;
    bool shakeApplied;
    float seedX, seedY, seedZ, seedRoll;

    public static CameraShake Instance
    {
        get
        {
            if (s_instance != null) return s_instance;
            Camera cam = Camera.main;
            if (cam == null) return null;
            s_instance = cam.GetComponent<CameraShake>();
            if (s_instance == null) s_instance = cam.gameObject.AddComponent<CameraShake>();
            return s_instance;
        }
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this) { Destroy(this); return; }
        s_instance = this;
        seedX = Random.value * 100f;
        seedY = Random.value * 100f;
        seedZ = Random.value * 100f;
        seedRoll = Random.value * 100f;
    }

    void OnDisable()
    {
        RemovePreviousShake();
    }

    void OnDestroy()
    {
        RemovePreviousShake();
        if (s_instance == this) s_instance = null;
    }

    /// <summary>
    /// Adds trauma that decays linearly. Call multiple times to stack up to 1.
    /// 0.4 = small bump, 0.8 = solid hit, 1.0 = catastrophic.
    /// </summary>
    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    void Update()
    {
        RemovePreviousShake();
    }

    void LateUpdate()
    {
        RemovePreviousShake();

        if (trauma <= 0f)
        {
            return;
        }

        float intensity = squareTrauma ? trauma * trauma : trauma;

        float t = Time.unscaledTime * 25f;
        float ox = (Mathf.PerlinNoise(seedX, t) - 0.5f) * 2f;
        float oy = (Mathf.PerlinNoise(seedY, t) - 0.5f) * 2f;
        float oz = (Mathf.PerlinNoise(seedZ, t) - 0.5f) * 2f;
        float or = (Mathf.PerlinNoise(seedRoll, t) - 0.5f) * 2f;

        lastOffset = new Vector3(ox, oy, oz) * (maxOffset * intensity);
        lastRotationOffset = Quaternion.Euler(0f, 0f, or * maxRotation * intensity);
        transform.localPosition += lastOffset;
        transform.localRotation = transform.localRotation * lastRotationOffset;
        shakeApplied = true;

        trauma = Mathf.Max(0f, trauma - decayPerSecond * Time.unscaledDeltaTime);
    }

    void RemovePreviousShake()
    {
        if (!shakeApplied) return;

        transform.localPosition -= lastOffset;
        transform.localRotation = transform.localRotation * Quaternion.Inverse(lastRotationOffset);
        lastOffset = Vector3.zero;
        lastRotationOffset = Quaternion.identity;
        shakeApplied = false;
    }
}
