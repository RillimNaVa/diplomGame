using UnityEngine;

// Phase 3 / PR 3.B — slow visible enemy projectile.
// Owner-filtered: passes through the firing enemy and any other enemy. Damages
// the first non-owner Health it touches and self-destructs. Auto-destructs on
// world geometry hit and after lifetime expires.
//
// Phase 3 / PR 3.F — pool-aware. RangedEnemyBrain rents the projectile via
// EnemyProjectilePool; on hit / lifetime, the projectile returns itself to
// the pool instead of Destroy(gameObject). Falls back to Destroy when no pool
// is bound (e.g. dropped into a scene by hand).
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

    // PR 3.F pooling state
    EnemyProjectilePool pool;
    GameObject originPrefab;
    TrailRenderer[] trails;
    bool trailsCached;

    [Header("Trail Auto-Config (PR 5.A)")]
    [Tooltip("If no TrailRenderer exists at Awake, build a default one so projectile reads as moving plasma.")]
    public bool autoBuildTrail = true;
    [ColorUsage(true, true)]
    public Color trailColor = new Color(0.8f, 2.6f, 3.2f);
    public float trailTime = 0.25f;
    public float trailStartWidth = 0.18f;
    public float trailEndWidth = 0.02f;

    static Material s_trailMaterial;

    void Awake()
    {
        if (autoBuildTrail && GetComponentInChildren<TrailRenderer>(true) == null)
        {
            BuildDefaultTrail();
        }
    }

    void BuildDefaultTrail()
    {
        var tr = gameObject.AddComponent<TrailRenderer>();
        tr.time = trailTime;
        tr.startWidth = trailStartWidth;
        tr.endWidth = trailEndWidth;
        tr.minVertexDistance = 0.05f;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        tr.alignment = LineAlignment.View;
        tr.startColor = trailColor;
        tr.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        tr.sharedMaterial = ResolveTrailMaterial();
    }

    static Material ResolveTrailMaterial()
    {
        if (s_trailMaterial != null) return s_trailMaterial;
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        s_trailMaterial = new Material(sh);
        s_trailMaterial.name = "EnemyProjectileTrailMat(Runtime)";
        if (s_trailMaterial.HasProperty("_Surface")) s_trailMaterial.SetFloat("_Surface", 1f);
        if (s_trailMaterial.HasProperty("_Blend"))   s_trailMaterial.SetFloat("_Blend", 1f);
        if (s_trailMaterial.HasProperty("_ZWrite"))  s_trailMaterial.SetFloat("_ZWrite", 0f);
        return s_trailMaterial;
    }

    public void BindPool(EnemyProjectilePool ownerPool, GameObject prefab)
    {
        pool = ownerPool;
        originPrefab = prefab;
    }

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

    /// <summary>
    /// PR 3.F — pool reset. Clears the owner / damage / direction state and
    /// flushes any TrailRenderer so a recycled projectile does not draw a long
    /// trail from the previous shot's last position to the new muzzle.
    /// </summary>
    public void ResetForPool()
    {
        owner = null;
        damage = 0f;
        speed = 0f;
        direction = Vector3.zero;
        spawnTime = Time.time;

        if (!trailsCached)
        {
            trails = GetComponentsInChildren<TrailRenderer>(true);
            trailsCached = true;
        }
        if (trails != null)
        {
            for (int i = 0; i < trails.Length; i++)
            {
                if (trails[i] != null) trails[i].Clear();
            }
        }
    }

    void Update()
    {
        if (Time.time - spawnTime >= lifetime)
        {
            ReturnOrDestroy();
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
            // PR 5.C — pass projectile origin so the player's damage-direction
            // HUD points at the actual incoming shot, not the spitter that
            // fired it (which the player may have already moved away from).
            hp.TakeDamage(damage, transform.position);
        }
        else
        {
            // PR 5.C — wall hit: leave a scorch decal where the plasma ate.
            ImpactFXSystem.Instance.SpawnBulletDecal(transform.position, -direction);
            // Nearby lamps flicker briefly when a plasma round detonates.
            LampFlicker.NudgeAt(transform.position, 5.5f);
        }

        ReturnOrDestroy();
    }

    void ReturnOrDestroy()
    {
        if (pool != null && originPrefab != null) pool.Return(gameObject, originPrefab);
        else Destroy(gameObject);
    }
}
