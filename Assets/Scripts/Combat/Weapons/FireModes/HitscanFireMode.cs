using System;
using System.Collections;
using UnityEngine;

// Hitscan attack: ray from camera forward, single hit, optional tracer + impact VFX.
// Used by Pulse Pistol and Void Rifle.
//
// Aim ray always starts at the camera, never the muzzle — this prevents the
// "bullets curve during slides" bug we hit in PR #11.
[Serializable]
public class HitscanFireMode : FireModeBase
{
    [Tooltip("If > 0, overrides definition.range.")]
    public float maxDistanceOverride = 0f;

    [Tooltip("How long the tracer line stays visible after a shot.")]
    public float tracerDuration = 0.05f;

    [Tooltip("Optional impact particle prefab spawned at hit point.")]
    public ParticleSystem impactEffectPrefab;

    public override void ExecuteFire(
        WeaponContext context,
        WeaponDefinition definition,
        WeaponBase weapon)
    {
        if (context == null || context.CameraTransform == null) return;

        Transform cam = context.CameraTransform;
        float maxDistance = maxDistanceOverride > 0f ? maxDistanceOverride : definition.range;
        Vector3 origin = cam.position;
        Vector3 direction = cam.forward;
        Vector3 endPoint = origin + direction * maxDistance;

        // RaycastAll + filter so we can skip the player's own colliders without
        // relying on a separate layer mask setup. First non-owner hit wins.
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            maxDistance,
            context.HitMask,
            QueryTriggerInteraction.Ignore);

        if (hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (context.Owner != null && hit.collider.transform.IsChildOf(context.Owner))
                {
                    continue;
                }

                endPoint = hit.point;

                Health health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    health.TakeDamage(definition.damage);
                }
                else
                {
                    // PR 5.C — bullet impact decal on world surfaces (walls,
                    // props, structures). Skipped on enemies because the hit
                    // flash + dissolve already cover that read.
                    ImpactFXSystem.Instance.SpawnBulletDecal(hit.point, hit.normal);
                }

                if (impactEffectPrefab != null)
                {
                    ParticleSystem fx = UnityEngine.Object.Instantiate(
                        impactEffectPrefab,
                        hit.point,
                        Quaternion.LookRotation(hit.normal));
                    UnityEngine.Object.Destroy(
                        fx.gameObject,
                        fx.main.duration + fx.main.startLifetime.constantMax);
                }
                break;
            }
        }

        if (definition.tracerPrefab != null
            && context.MuzzleTransform != null
            && context.CoroutineHost != null)
        {
            context.CoroutineHost.StartCoroutine(PlayTracer(
                definition.tracerPrefab,
                context.MuzzleTransform.position,
                endPoint,
                tracerDuration));
        }
    }

    private static IEnumerator PlayTracer(LineRenderer prefab, Vector3 start, Vector3 end, float duration)
    {
        LineRenderer tracer = UnityEngine.Object.Instantiate(prefab, start, Quaternion.identity);
        tracer.positionCount = 2;
        tracer.SetPosition(0, start);
        tracer.SetPosition(1, end);
        yield return new WaitForSeconds(duration);
        if (tracer != null)
        {
            UnityEngine.Object.Destroy(tracer.gameObject);
        }
    }
}
