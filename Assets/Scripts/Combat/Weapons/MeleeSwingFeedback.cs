using UnityEngine;

// Auto-attached by GenericWeapon when the weapon is melee. Drives a short
// procedural swing arc on the player's WeaponHolder + a small camera kick on
// every fire event. No Animator / no clip required; works on any model.
//
// Side-channel — does not mutate WeaponBase or MeleeArcFireMode.
public class MeleeSwingFeedback : MonoBehaviour
{
    [Header("Swing")]
    [Tooltip("Total swing duration in seconds.")]
    public float swingDuration = 0.18f;
    [Tooltip("Local-space pre-swing rotation (degrees) the holder rewinds to before the cut.")]
    public Vector3 windupEuler = new Vector3(-12f, -25f, 0f);
    [Tooltip("Local-space peak swing rotation (degrees).")]
    public Vector3 peakEuler = new Vector3(20f, 35f, -10f);
    [Tooltip("Local-space forward thrust at the peak of the swing (meters).")]
    public Vector3 peakOffset = new Vector3(0.05f, -0.02f, 0.18f);

    [Header("Camera Kick")]
    public float cameraTrauma = 0.18f;

    WeaponBase weapon;
    Transform holder;
    Quaternion holderBase;
    Vector3 holderBasePos;
    bool holderCached;
    float swingT01 = -1f; // -1 = idle

    void Awake()
    {
        weapon = GetComponent<WeaponBase>();
    }

    void OnEnable()
    {
        if (weapon != null) weapon.OnFired += OnFired;
        ResolveHolder();
    }

    void OnDisable()
    {
        if (weapon != null) weapon.OnFired -= OnFired;
        RestoreHolder();
    }

    void ResolveHolder()
    {
        if (holderCached || holder != null) return;
        var pc = GetComponentInParent<PlayerController>();
        if (pc == null) pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null && pc.weaponHolder != null)
        {
            holder = pc.weaponHolder;
            holderBase = holder.localRotation;
            holderBasePos = holder.localPosition;
            holderCached = true;
        }
    }

    void RestoreHolder()
    {
        if (holder != null && holderCached)
        {
            holder.localRotation = holderBase;
            holder.localPosition = holderBasePos;
        }
    }

    void OnFired(WeaponBase _)
    {
        ResolveHolder();
        swingT01 = 0f;
        if (cameraTrauma > 0f && CameraShake.Instance != null)
            CameraShake.Instance.AddTrauma(cameraTrauma);
    }

    void LateUpdate()
    {
        if (holder == null || swingT01 < 0f) return;

        swingT01 += Time.deltaTime / Mathf.Max(0.05f, swingDuration);
        if (swingT01 >= 1f)
        {
            swingT01 = -1f;
            holder.localRotation = holderBase;
            holder.localPosition = holderBasePos;
            return;
        }

        // 0..0.25 windup, 0.25..0.55 cut, 0.55..1 follow-through.
        float t = swingT01;
        Quaternion target;
        Vector3 offset;
        if (t < 0.25f)
        {
            float k = t / 0.25f;
            target = Quaternion.Slerp(holderBase, holderBase * Quaternion.Euler(windupEuler), EaseOut(k));
            offset = Vector3.Lerp(Vector3.zero, peakOffset * 0.15f, EaseOut(k));
        }
        else if (t < 0.55f)
        {
            float k = (t - 0.25f) / 0.30f;
            target = Quaternion.Slerp(
                holderBase * Quaternion.Euler(windupEuler),
                holderBase * Quaternion.Euler(peakEuler),
                EaseInOut(k));
            offset = Vector3.Lerp(peakOffset * 0.15f, peakOffset, EaseInOut(k));
        }
        else
        {
            float k = (t - 0.55f) / 0.45f;
            target = Quaternion.Slerp(
                holderBase * Quaternion.Euler(peakEuler),
                holderBase,
                EaseInOut(k));
            offset = Vector3.Lerp(peakOffset, Vector3.zero, EaseInOut(k));
        }
        holder.localRotation = target;
        holder.localPosition = holderBasePos + offset;
    }

    static float EaseOut(float k) { k = Mathf.Clamp01(k); return 1f - (1f - k) * (1f - k); }
    static float EaseInOut(float k) { k = Mathf.Clamp01(k); return k * k * (3f - 2f * k); }
}
