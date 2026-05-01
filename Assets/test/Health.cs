using System;
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
    public UnityEvent<float> onHeal;
    public HealthChangedEvent onHealthChanged;

    // Global hit/heal channel for systems that don't want to subscribe to every
    // Health instance individually (e.g., CombatHUDController for hit markers).
    // Args: (Health victim, float amount). Subscribers must filter by victim if needed.
    public static event Action<Health, float> AnyDamaged;
    public static event Action<Health, float> AnyHealed;

    bool autoDisableCancelled;

    /// <summary>
    /// PR 5.C — world-space position of the most recent damage source. Set by
    /// the TakeDamage(damage, sourcePos) overload; consumed by the player's
    /// damage-direction HUD. Defaults to transform.position when unknown.
    /// </summary>
    public Vector3 LastDamageSource { get; private set; }
    public bool HasLastDamageSource { get; private set; }

    void Awake()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return;

        HasLastDamageSource = false;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        onTakeDamage?.Invoke(damage);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        AnyDamaged?.Invoke(this, damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// PR 5.C — same as TakeDamage(damage) but records the world-space position
    /// of the attacker. Used by the player's damage-direction HUD to draw an
    /// arc pointing at the attacker when they're outside the camera frustum.
    /// </summary>
    public void TakeDamage(float damage, Vector3 sourcePosition)
    {
        if (currentHealth <= 0) return;

        LastDamageSource = sourcePosition;
        HasLastDamageSource = true;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        onTakeDamage?.Invoke(damage);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        AnyDamaged?.Invoke(this, damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0f) return;
        if (amount <= 0f) return;

        float before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        float applied = currentHealth - before;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        if (applied > 0f)
        {
            onHeal?.Invoke(applied);
            AnyHealed?.Invoke(this, applied);
        }
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
