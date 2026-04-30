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

    bool autoDisableCancelled;

    void Awake()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        onTakeDamage?.Invoke(damage);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0f) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Die()
    {
        autoDisableCancelled = false;
        Debug.Log($"{name} DIED!");
        onDeath?.Invoke();
        if (!autoDisableCancelled)
        {
            Invoke(nameof(Disable), 1f);
        }
    }

    void Disable()
    {
        gameObject.SetActive(false);
    }

    // Phase 3 / PR 3.F — pool reset hooks. PooledEnemy calls these before
    // SetActive(true) on a recycled instance.

    /// <summary>
    /// Restores currentHealth to maxHealth and re-fires onHealthChanged so the
    /// HUD / EnemyStagger reset their visuals. Does not touch listeners.
    /// </summary>
    public void ResetForPool()
    {
        CancelInvoke(nameof(Disable));
        autoDisableCancelled = false;
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Cancels the pending 1s SetActive(false) scheduled by Die(). PooledEnemy
    /// owns disable timing once an instance is pool-managed.
    /// </summary>
    public void CancelAutoDisable()
    {
        autoDisableCancelled = true;
        CancelInvoke(nameof(Disable));
    }
}
