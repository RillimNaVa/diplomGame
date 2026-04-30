using UnityEngine;

// Phase 3 / PR 3.G — physics-driven debris on enemy death. Complements the
// existing dissolve (body fades) + EnemyDeathBurst (particle puff) with a
// short-lived flash light + 6-10 small cube fragments that fly outward and
// auto-destroy after ~1.2s. Auto-attached by EnemyBrainBase.Awake so all
// existing enemy prefabs get the upgrade with no Editor work.
//
// Detached on death so the burst survives the EnemyPool.Return that follows
// roughly 1.5s later (PooledEnemy grace window).
public class EnemyDeathShards : MonoBehaviour
{
    [Tooltip("Number of cube fragments spawned. Brutes scale up by 1.5x.")]
    public int shardCount = 7;
    [Tooltip("Edge length of each cube fragment.")]
    public float shardSize = 0.18f;
    [Tooltip("Initial outward speed range.")]
    public float minSpeed = 4f;
    public float maxSpeed = 9f;
    [Tooltip("Seconds before each shard auto-destroys.")]
    public float shardLifetime = 1.2f;
    [Tooltip("Brief flash-light intensity (HDR).")]
    public float flashIntensity = 16f;
    [Tooltip("Flash light duration in seconds.")]
    public float flashDuration = 0.18f;

    Health health;
    Color tintColor = new Color(1.6f, 0.6f, 0.0f);
    EnemyRole role = EnemyRole.Fodder;
    static Material s_shardMaterial;

    void Awake()
    {
        health = GetComponent<Health>();
        EnemyBrainBase brain = GetComponent<EnemyBrainBase>();
        if (brain != null && brain.data != null)
        {
            tintColor = brain.data.telegraphColor;
            role = brain.data.role;
        }
    }

    void OnEnable()
    {
        if (health != null) health.onDeath.AddListener(SpawnShards);
    }

    void OnDisable()
    {
        if (health != null) health.onDeath.RemoveListener(SpawnShards);
    }

    void SpawnShards()
    {
        Vector3 origin = transform.position + Vector3.up * 0.9f;
        // Tank role gets bigger blast — reads as heavier death.
        float scale = role == EnemyRole.Tank ? 1.5f : 1f;
        int count = Mathf.RoundToInt(shardCount * scale);
        float size = shardSize * (role == EnemyRole.Tank ? 1.4f : 1f);

        SpawnFlashLight(origin, scale);

        Material mat = ResolveMaterial();
        for (int i = 0; i < count; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "DeathShard";
            // Strip the auto-collider — physics is purely visual + we don't
            // want shards to push the player or block bullets.
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * size;

            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null) rend.sharedMaterial = mat;

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.mass = 0.4f;
            // Random outward direction, biased upward so debris arcs.
            Vector3 dir = (Random.onUnitSphere + Vector3.up * 0.7f).normalized;
            float speed = Random.Range(minSpeed, maxSpeed) * scale;
            rb.linearVelocity = dir * speed;
            rb.angularVelocity = Random.insideUnitSphere * 12f;

            // Brief auto-attached fader handles emission decay + destroy.
            var fader = go.AddComponent<DeathShardFader>();
            fader.tint = tintColor;
            fader.lifetime = shardLifetime;
        }
    }

    void SpawnFlashLight(Vector3 worldPos, float scale)
    {
        var go = new GameObject("DeathFlash");
        go.transform.position = worldPos;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = tintColor;
        light.intensity = flashIntensity * scale;
        light.range = 6f * scale;
        light.shadows = LightShadows.None;
        var fader = go.AddComponent<DeathFlashFader>();
        fader.duration = flashDuration;
        fader.startIntensity = light.intensity;
    }

    static Material ResolveMaterial()
    {
        if (s_shardMaterial != null) return s_shardMaterial;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        s_shardMaterial = new Material(sh) { name = "EnemyDeathShard(Runtime)" };
        if (s_shardMaterial.HasProperty("_BaseColor")) s_shardMaterial.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.08f));
        if (s_shardMaterial.HasProperty("_Metallic")) s_shardMaterial.SetFloat("_Metallic", 0.7f);
        if (s_shardMaterial.HasProperty("_Smoothness")) s_shardMaterial.SetFloat("_Smoothness", 0.55f);
        s_shardMaterial.enableInstancing = true;
        return s_shardMaterial;
    }
}

// Tiny helper: ramps a shard's emission up briefly, then fades it + destroys.
public class DeathShardFader : MonoBehaviour
{
    public Color tint = Color.white;
    public float lifetime = 1.2f;

    float t;
    Renderer rend;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / lifetime);
        // Emission peaks early then fades to 0.
        float pulse = Mathf.Lerp(2.5f, 0f, Mathf.Pow(k, 0.7f));
        if (rend != null && rend.sharedMaterial.HasProperty("_EmissionColor"))
        {
            rend.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", tint * pulse);
            rend.SetPropertyBlock(mpb);
        }
        if (t >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}

// Brief point light that fades out and destroys itself.
public class DeathFlashFader : MonoBehaviour
{
    public float duration = 0.18f;
    public float startIntensity = 16f;
    Light targetLight;
    float t;

    void Awake() { targetLight = GetComponent<Light>(); }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        if (targetLight != null) targetLight.intensity = startIntensity * (1f - k * k);
        if (t >= duration) Destroy(gameObject);
    }
}
