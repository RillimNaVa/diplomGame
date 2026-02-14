using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class HealthChangedEvent : UnityEvent<float, float> { }

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent<float> onTakeDamage;
    public HealthChangedEvent onHealthChanged;

    void Awake()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        onTakeDamage?.Invoke(damage);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            onHealthChanged?.Invoke(currentHealth, maxHealth);
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{name} DIED!");
        onDeath?.Invoke();
        Invoke(nameof(Disable), 1f);
    }

    void Disable()
    {
        gameObject.SetActive(false);
    }
}
