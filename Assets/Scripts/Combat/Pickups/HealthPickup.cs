using UnityEngine;

/// <summary>
/// World-space HP orb. Heals the Player on trigger overlap, reading the
/// heal amount from PlayerStats (seam #1) so upgrades can scale it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HealthPickup : MonoBehaviour
{
    [Header("Lifetime")]
    [Tooltip("Seconds before the orb despawns if not picked up.")]
    public float lifetime = 15f;

    [Header("Magnet (future polish)")]
    [Tooltip("If > 0, orb accelerates toward the player within this range. Leave 0 for static pickup.")]
    public float magnetRange = 0f;
    public float magnetSpeed = 12f;

    [Header("FX (optional)")]
    public AudioClip pickupSfx;
    public GameObject pickupVfx;

    private bool consumed;
    private Transform playerTransform;

    void Start()
    {
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }

        GameObject tagged = GameObject.FindWithTag("Player");
        if (tagged != null)
        {
            playerTransform = tagged.transform;
            // Phase 4 / PR 4.PB — pull magnet radius from PlayerStats so
            // HpOrbMagnetRadius upgrades extend orb attraction at spawn time.
            // (Re-evaluating per-frame would let mid-flight upgrades scale a
            //  flying orb, which is rare and not worth the cost.)
            PlayerStats stats = tagged.GetComponentInParent<PlayerStats>();
            if (stats != null)
            {
                float resolved = stats.GetOrbMagnetRadius();
                if (resolved > magnetRange) magnetRange = resolved;
            }
        }

        // PR 5.C — auto-attach the glow visual so existing orb prefabs upgrade
        // without Editor work.
        if (GetComponent<HealthPickupGlow>() == null) gameObject.AddComponent<HealthPickupGlow>();
    }

    void Update()
    {
        if (magnetRange <= 0f || playerTransform == null) return;

        Vector3 delta = playerTransform.position - transform.position;
        float sqr = delta.sqrMagnitude;
        if (sqr <= magnetRange * magnetRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, magnetSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (consumed) return;
        if (!other.CompareTag("Player")) return;

        Health health = other.GetComponentInParent<Health>();
        if (health == null || health.currentHealth <= 0f) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        float amount = stats != null ? stats.GetOrbHealAmount() : 5f;

        health.Heal(amount);
        consumed = true;

        if (pickupSfx != null)
        {
            AudioSource.PlayClipAtPoint(pickupSfx, transform.position);
        }

        if (pickupVfx != null)
        {
            Instantiate(pickupVfx, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
