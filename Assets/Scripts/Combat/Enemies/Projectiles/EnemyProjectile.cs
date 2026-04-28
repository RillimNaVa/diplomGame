using UnityEngine;

// Phase 3 / PR 3.B — slow visible enemy projectile.
// Owner-filtered: passes through the firing enemy and any other enemy. Damages
// the first non-owner Health it touches and self-destructs. Auto-destructs on
// world geometry hit and after lifetime expires (no projectile pooling yet —
// PR 3.F handles pooling).
//
// Prefab requirements (created by user in Editor):
//   - SphereCollider with isTrigger = true (radius ~0.25)
//   - Visual mesh / TrailRenderer / particle (visible, not hitscan-fast)
//   - This component
[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    [Tooltip("Seconds before the projectile self-destructs if it never hits anything.")]
    public float lifetime = 4f;

    GameObject owner;
    float damage;
    float speed;
    Vector3 direction;
    float spawnTime;

    public void Configure(GameObject ownerObj, float damageAmount, float projSpeed, Vector3 dir)
    {
        owner = ownerObj;
        damage = damageAmount;
        speed = projSpeed;
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
        spawnTime = Time.time;

        // Trigger collider is required so we use OnTriggerEnter and avoid the
        // physics nudge a non-kinematic Rigidbody would apply on impact.
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger) col.isTrigger = true;
    }

    void Update()
    {
        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Manual movement avoids Rigidbody tunneling questions for slow plasma
        // shots. At default speed=10 and 60fps the step is ~0.17m, which is
        // smaller than the recommended SphereCollider radius (0.25), so the
        // OnTriggerEnter path catches walls reliably.
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform))) return;

        // Pass through other enemies — keeps dense fights fair and avoids
        // friendly-fire chains. Identified by the presence of an EnemyBrainBase
        // or legacy SimpleEnemyAI on the hit hierarchy root.
        if (other.GetComponentInParent<EnemyBrainBase>() != null) return;
        if (other.GetComponentInParent<SimpleEnemyAI>() != null) return;

        // Damage anything else that has Health (typically the player).
        Health hp = other.GetComponentInParent<Health>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
