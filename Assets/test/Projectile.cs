using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile")]
    public float speed = 40f;
    public float damage = 25f;
    public float lifetime = 3f;

    [Header("Impact")]
    public GameObject impactVfxPrefab;

    private Rigidbody rb;
    private bool hasHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        hasHit = false;
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector3 direction, float overrideDamage = -1f)
    {
        if (overrideDamage >= 0f)
        {
            damage = overrideDamage;
        }

        rb.velocity = direction.normalized * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
        {
            return;
        }

        hasHit = true;

        Health health = collision.collider.GetComponentInParent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        if (impactVfxPrefab != null)
        {
            ContactPoint contact = collision.contacts.Length > 0
                ? collision.contacts[0]
                : default;

            Vector3 point = collision.contacts.Length > 0 ? contact.point : transform.position;
            Vector3 normal = collision.contacts.Length > 0 ? contact.normal : -transform.forward;
            Quaternion rotation = Quaternion.LookRotation(normal);
            Instantiate(impactVfxPrefab, point, rotation);
        }

        Destroy(gameObject);
    }
}
