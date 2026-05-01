using UnityEngine;

// Auto-attached to a melee weapon by GenericWeapon. Builds a TrailRenderer at
// the weapon's MuzzlePoint (placed at the blade tip in the prefab) and pulses
// it on every fire event for ~0.18s. No prefab/material setup required.
[RequireComponent(typeof(WeaponBase))]
public class BladeTrail : MonoBehaviour
{
    [Header("Trail")]
    public float activeDuration = 0.18f;
    public float trailTime = 0.12f;
    public float startWidth = 0.25f;
    public float endWidth = 0.02f;
    [ColorUsage(true, true)]
    public Color trailColor = new Color(0.36f, 0.9f, 1f, 1f); // cyan plasma

    WeaponBase weapon;
    Transform tipPoint;
    TrailRenderer trail;
    float activeUntil;

    void Awake()
    {
        weapon = GetComponent<WeaponBase>();
        BuildTrail();
    }

    void OnEnable()
    {
        if (weapon != null) weapon.OnFired += OnFired;
    }

    void OnDisable()
    {
        if (weapon != null) weapon.OnFired -= OnFired;
        if (trail != null) trail.emitting = false;
    }

    void BuildTrail()
    {
        if (weapon == null) return;
        tipPoint = weapon.MuzzlePoint;
        if (tipPoint == null)
        {
            // Fallback — find the deepest child as the visual tip.
            tipPoint = FindDeepestChild(transform);
        }
        if (tipPoint == null) return;

        var trailGo = new GameObject("BladeTrail");
        trailGo.transform.SetParent(tipPoint, false);
        trailGo.transform.localPosition = Vector3.zero;
        trail = trailGo.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.minVertexDistance = 0.02f;
        trail.startWidth = startWidth;
        trail.endWidth = endWidth;
        trail.emitting = false;

        var mat = new Material(Shader.Find("Sprites/Default"));
        if (mat == null || mat.shader == null) mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = trailColor;
        trail.material = mat;
        trail.startColor = trailColor;
        var endC = trailColor; endC.a = 0f;
        trail.endColor = endC;
    }

    static Transform FindDeepestChild(Transform t)
    {
        Transform deepest = t;
        int depth = 0;
        Walk(t, 0, ref deepest, ref depth);
        return deepest;
    }

    static void Walk(Transform t, int d, ref Transform best, ref int bestDepth)
    {
        if (d > bestDepth) { best = t; bestDepth = d; }
        for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), d + 1, ref best, ref bestDepth);
    }

    void OnFired(WeaponBase _)
    {
        if (trail == null) return;
        trail.Clear();
        trail.emitting = true;
        activeUntil = Time.time + activeDuration;
    }

    void LateUpdate()
    {
        if (trail != null && trail.emitting && Time.time >= activeUntil)
        {
            trail.emitting = false;
        }
    }
}
