using System.Collections.Generic;
using UnityEngine;

// Auto-attached to a melee weapon by GenericWeapon. Listens to the weapon's
// OnFired event, re-runs the same OverlapSphere as MeleeArcFireMode, and spawns
// a tiny spark burst at the impact point of each hit Health it finds.
//
// Side-channel — does not mutate MeleeArcFireMode. Sparks are short-lived
// procedurally built ParticleSystem instances pooled by the FX cache below.
[RequireComponent(typeof(WeaponBase))]
public class MeleeImpactSparks : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask hitMask = ~0;
    [Tooltip("Camera the swing aims through. Auto-resolved from Camera.main.")]
    public Transform cameraTransform;

    [Header("Sparks")]
    [ColorUsage(true, true)]
    public Color sparkColor = new Color(0.55f, 0.95f, 1f, 1f);
    public int sparkCount = 8;
    public float sparkLifetime = 0.32f;
    public float sparkSpeed = 6f;

    WeaponBase weapon;
    Transform owner;

    void Awake()
    {
        weapon = GetComponent<WeaponBase>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        var pc = GetComponentInParent<PlayerController>();
        owner = pc != null ? pc.transform : null;
    }

    void OnEnable()
    {
        if (weapon != null) weapon.OnFired += OnFired;
    }

    void OnDisable()
    {
        if (weapon != null) weapon.OnFired -= OnFired;
    }

    void OnFired(WeaponBase w)
    {
        if (cameraTransform == null) cameraTransform = Camera.main != null ? Camera.main.transform : null;
        if (cameraTransform == null || w == null || w.Definition == null) return;

        float radius = Mathf.Max(0.1f, w.Definition.range * 0.5f);
        Vector3 center = cameraTransform.position + cameraTransform.forward * radius;

        var hits = Physics.OverlapSphere(center, radius, hitMask, QueryTriggerInteraction.Ignore);
        var seen = new HashSet<Health>();
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (owner != null && col.transform.IsChildOf(owner)) continue;
            var h = col.GetComponentInParent<Health>();
            if (h == null || !seen.Add(h)) continue;
            // Use the closest point on the collider as a reasonable impact pos.
            Vector3 impact = col.ClosestPoint(center);
            SparkPool.Spawn(impact, (impact - center).normalized, sparkColor, sparkCount, sparkLifetime, sparkSpeed);
        }
    }
}

// Lightweight pool of tiny burst ParticleSystems so we don't allocate per swing.
internal static class SparkPool
{
    static readonly Stack<ParticleSystem> s_pool = new Stack<ParticleSystem>();

    public static void Spawn(Vector3 pos, Vector3 normal, Color color, int count, float life, float speed)
    {
        ParticleSystem ps = s_pool.Count > 0 ? s_pool.Pop() : Build();
        var t = ps.transform;
        t.position = pos;
        t.rotation = normal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(normal) : Quaternion.identity;

        var main = ps.main;
        main.startColor = color;
        main.startLifetime = life;
        main.startSpeed = speed;
        ps.gameObject.SetActive(true);
        ps.Emit(count);
        // Recycle after lifetime + small grace.
        var host = ps.gameObject.GetComponent<SparkHost>();
        if (host == null) host = ps.gameObject.AddComponent<SparkHost>();
        host.RecycleAfter(life + 0.15f);
    }

    public static void Recycle(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Clear();
        ps.gameObject.SetActive(false);
        s_pool.Push(ps);
    }

    static ParticleSystem Build()
    {
        var go = new GameObject("MeleeSpark");
        Object.DontDestroyOnLoad(go);
        var ps = go.AddComponent<ParticleSystem>();

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startSpeed = 6f;
        main.startLifetime = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.2f;

        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.05f;

        var col = ps.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default"));
        return ps;
    }
}

// Helper component on the spark GO that schedules its own return to the pool.
internal class SparkHost : MonoBehaviour
{
    float deactivateAt;
    bool armed;

    public void RecycleAfter(float seconds)
    {
        deactivateAt = Time.time + seconds;
        armed = true;
    }

    void Update()
    {
        if (!armed) return;
        if (Time.time >= deactivateAt)
        {
            armed = false;
            var ps = GetComponent<ParticleSystem>();
            SparkPool.Recycle(ps);
        }
    }
}
