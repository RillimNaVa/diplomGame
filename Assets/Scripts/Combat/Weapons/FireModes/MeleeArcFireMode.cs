using System;
using System.Collections.Generic;
using UnityEngine;

// Short-range arc swing: OverlapSphere centered half-a-range in front of the camera.
// Replaces the temporary PlayerController melee that was removed in PR A.
[Serializable]
public class MeleeArcFireMode : FireModeBase
{
    public override void ExecuteFire(
        WeaponContext context,
        WeaponDefinition definition,
        WeaponBase weapon)
    {
        if (context == null || context.CameraTransform == null) return;

        Transform cam = context.CameraTransform;
        float radius = Mathf.Max(0.1f, definition.range * 0.5f);
        Vector3 center = cam.position + cam.forward * radius;

        Collider[] hits = Physics.OverlapSphere(
            center,
            radius,
            context.HitMask,
            QueryTriggerInteraction.Ignore);

        HashSet<Health> damaged = new HashSet<Health>();
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (context.Owner != null && col.transform.IsChildOf(context.Owner)) continue;

            Health health = col.GetComponentInParent<Health>();
            if (health != null && damaged.Add(health))
            {
                health.TakeDamage(definition.damage);
            }
        }
    }
}
