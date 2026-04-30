using UnityEngine;

// Phase 4 / PR 4.A — particle burst on enemy death. Spawns a runtime-built,
// short-lived ParticleSystem keyed off the enemy's telegraphColor (or a
// neutral orange fallback) so different roles read distinct.
//
// The particle GO detaches from the enemy on death, lives in world space, and
// auto-destroys after its lifetime. This is intentional: the enemy is about
// to be returned to the EnemyPool (PooledEnemy.OnDeath), so the burst must
// not be parented to the recycled instance.
//
// Auto-attached by EnemyBrainBase.Awake so existing prefabs get death VFX
// without Editor work.
public class EnemyDeathBurst : MonoBehaviour
{
    [Tooltip("Number of particles emitted on death.")]
    public int burstCount = 24;
    [Tooltip("Particle lifetime seconds.")]
    public float particleLifetime = 0.9f;
    [Tooltip("Initial speed range of particles in m/s.")]
    public float minSpeed = 3f;
    public float maxSpeed = 7f;
    [Tooltip("Particle world-size at spawn.")]
    public float startSize = 0.18f;

    Health health;
    Color tintColor = new Color(1.6f, 0.6f, 0.0f);
    static Material s_burstMaterial;

    void Awake()
    {
        health = GetComponent<Health>();
        // Pull tint from EnemyData.telegraphColor if available — same emissive
        // hue used by TelegraphFlash, so role-color stays consistent.
        EnemyBrainBase brain = GetComponent<EnemyBrainBase>();
        if (brain != null && brain.data != null) tintColor = brain.data.telegraphColor;
    }

    void OnEnable()
    {
        if (health != null) health.onDeath.AddListener(SpawnBurst);
    }

    void OnDisable()
    {
        if (health != null) health.onDeath.RemoveListener(SpawnBurst);
    }

    void SpawnBurst()
    {
        Vector3 origin = transform.position + Vector3.up * 0.9f;
        GameObject go = new GameObject("EnemyDeathBurst");
        go.transform.position = origin;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        if (psr != null) psr.sharedMaterial = ResolveMaterial();

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.05f;
        main.loop = false;
        main.startLifetime = particleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = startSize;
        main.startColor = tintColor;
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = burstCount * 2;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)burstCount)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(tintColor, 0f), new GradientColorKey(tintColor * 0.4f, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.05f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ps.Play();
    }

    static Material ResolveMaterial()
    {
        if (s_burstMaterial != null) return s_burstMaterial;
        // Use the URP particle additive material if available, else fall back
        // to the built-in Default-ParticleSystem.
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        s_burstMaterial = new Material(sh);
        s_burstMaterial.name = "EnemyDeathBurst(Runtime)";
        if (s_burstMaterial.HasProperty("_Surface")) s_burstMaterial.SetFloat("_Surface", 1f); // transparent
        if (s_burstMaterial.HasProperty("_Blend")) s_burstMaterial.SetFloat("_Blend", 1f);     // additive
        if (s_burstMaterial.HasProperty("_EmissionColor")) s_burstMaterial.EnableKeyword("_EMISSION");
        return s_burstMaterial;
    }
}
