using UnityEngine;
using VoidSurvivor.ProceduralArena.Run;

// Phase 4 / PR 4.A — drives camera shake + damage vignette pulse when the
// player's Health takes damage. Auto-resolves the player Health, the active
// ArenaPostProcessingController, and Camera.main on Awake. No Inspector
// wiring needed — drop this on the player root or the GameManager.
[RequireComponent(typeof(Health))]
public class PlayerHitFeedback : MonoBehaviour
{
    [Header("Camera Shake")]
    [Tooltip("Trauma added per damage event. Multiplied by damage / shakeReferenceDamage so big hits shake harder.")]
    public float baseShakeTrauma = 0.35f;
    [Tooltip("A damage value of this much produces baseShakeTrauma * 1.0. Smaller damage scales linearly down, larger scales up.")]
    public float shakeReferenceDamage = 18f;
    [Tooltip("Hard cap on a single damage event's trauma so a Brute slam does not blackout the camera.")]
    public float maxShakeTrauma = 0.85f;

    [Header("Vignette")]
    public float vignetteDuration = 0.45f;

    Health health;
    ArenaPostProcessingController postFx;

    void Awake()
    {
        health = GetComponent<Health>();
    }

    void OnEnable()
    {
        if (health != null) health.onTakeDamage.AddListener(OnDamage);
    }

    void OnDisable()
    {
        if (health != null) health.onTakeDamage.RemoveListener(OnDamage);
    }

    void ResolvePostFx()
    {
        if (postFx != null) return;
#if UNITY_2023_1_OR_NEWER
        postFx = Object.FindFirstObjectByType<ArenaPostProcessingController>();
#else
        postFx = Object.FindObjectOfType<ArenaPostProcessingController>();
#endif
    }

    void OnDamage(float damage)
    {
        // Vignette pulse — biome-scoped because the controller lives on the
        // ArenaFlowController and rebuilds per arena.
        ResolvePostFx();
        if (postFx != null) postFx.PulseDamageVignette(vignetteDuration);

        // Camera shake — singleton on Camera.main, auto-attaches.
        CameraShake shake = CameraShake.Instance;
        if (shake != null)
        {
            float scale = damage / Mathf.Max(0.1f, shakeReferenceDamage);
            float trauma = Mathf.Clamp(baseShakeTrauma * scale, 0f, maxShakeTrauma);
            shake.AddTrauma(trauma);
        }
    }
}
