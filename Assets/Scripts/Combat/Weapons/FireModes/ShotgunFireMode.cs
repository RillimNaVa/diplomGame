using System;
using System.Collections;
using UnityEngine;

// Multi-ray spread attack: fires definition.pelletCount rays inside a cone
// of definition.spreadAngle degrees around camera forward. Each pellet resolves
// independently — if two pellets hit the same target, damage is applied twice.
// This is the DOOM-style shotgun feel: point-blank = all pellets connect = max damage.
//
// Total DPS = damage * pelletCount * fireRate — tune damage low per pellet.
[Serializable]
public class ShotgunFireMode : FireModeBase
{
    [Tooltip("How long each tracer stays visible after a shot.")]
    public float tracerDuration = 0.05f;

    [Tooltip("Optional impact particle prefab spawned at each pellet hit.")]
    public ParticleSystem impactEffectPrefab;

    public override void ExecuteFire(
        WeaponContext context,
        WeaponDefinition definition,
        WeaponBase weapon)
    {
        if (context == null || context.CameraTransform == null) return;

        Transform cam = context.CameraTransform;
        float maxDistance = definition.range;
        int pellets = Mathf.Max(1, definition.pelletCount);
        float spread = Mathf.Max(0f, definition.spreadAngle);

        for (int p = 0; p < pellets; p++)
        {
            Vector3 direction = GetSpreadDirection(cam.forward, cam.up, cam.right, spread);
            Vector3 origin = cam.position;
            Vector3 endPoint = origin + direction * maxDistance;

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
    }

    private static Vector3 GetSpreadDirection(Vector3 forward, Vector3 up, Vector3 right, float spreadDegrees)
    {
        if (spreadDegrees <= 0f) return forward;
        float angle = UnityEngine.Random.Range(0f, spreadDegrees);
        float rot = UnityEngine.Random.Range(0f, 360f);
        // Tilt forward by `angle` around an axis rotated by `rot` in the camera's up/right plane.
        Quaternion cone = Quaternion.AngleAxis(rot, forward) * Quaternion.AngleAxis(angle, up);
        return cone * forward;
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
