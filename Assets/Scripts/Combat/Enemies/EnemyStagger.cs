using System;
using UnityEngine;

/// <summary>
/// Enters a one-way staggered state when health drops below
/// <see cref="staggerThreshold"/>. While staggered the enemy's renderers pulse
/// emissive color, exposing a glory-kill window. Stays staggered until death.
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyStagger : MonoBehaviour
{
    [Header("Trigger")]
    [Range(0f, 1f)] public float staggerThreshold = 0.2f;

    [Header("Visual")]
    [Tooltip("Renderers to pulse. Auto-resolved from children if empty.")]
    public Renderer[] targetRenderers;
    public Color staggerEmission = Color.red;
    public float pulseSpeed = 4f;
    [Tooltip("Max emissive intensity multiplier at pulse peak.")]
    public float pulseIntensity = 2.5f;

    public bool IsStaggered { get; private set; }
    public event Action<bool> OnStaggerChanged;

    private Health health;
    private Material[] materialInstances;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        health = GetComponent<Health>();
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.onHealthChanged.AddListener(OnHealthChanged);
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.onHealthChanged.RemoveListener(OnHealthChanged);
        }
    }

    void Update()
    {
        if (!IsStaggered || materialInstances == null) return;
        float t = Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
        ApplyEmission(staggerEmission * (t * pulseIntensity));
    }

    void OnHealthChanged(float current, float max)
    {
        if (IsStaggered || max <= 0f) return;
        if (current / max <= staggerThreshold && current > 0f)
        {
            EnterStagger();
        }
    }

    void EnterStagger()
    {
        IsStaggered = true;

        // Instance materials once and enable the emission keyword so the pulsed
        // _EmissionColor updates actually render (URP Lit requires _EMISSION).
        materialInstances = new Material[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer r = targetRenderers[i];
            if (r == null) continue;
            materialInstances[i] = r.material;
            materialInstances[i].EnableKeyword("_EMISSION");
        }

        OnStaggerChanged?.Invoke(true);
    }

    void ApplyEmission(Color emission)
    {
        for (int i = 0; i < materialInstances.Length; i++)
        {
            Material m = materialInstances[i];
            if (m == null) continue;
            m.SetColor(EmissionColorId, emission);
        }
    }
}
