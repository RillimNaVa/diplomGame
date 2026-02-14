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

    void EnsureEventsInitialized()
    {
        onDeath ??= new UnityEvent();
        onTakeDamage ??= new UnityEvent<float>();
        onHealthChanged ??= new HealthChangedEvent();
    }

    void Awake()
    {
        EnsureEventsInitialized();
        currentHealth = maxHealth;
        onHealthChanged.Invoke(currentHealth, maxHealth);
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

    void OnGUI()
    {
        if (!gameObject.activeInHierarchy || currentHealth <= 0) return;
        if (Camera.main == null) return;

        Vector3 pos = transform.position + Vector3.up * 2.5f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(pos);

        if (screenPos.z > 0)
        {
            GUI.color = currentHealth < maxHealth * 0.3f ? Color.red : Color.white;
            string text = $"HP: {currentHealth:F0}/{maxHealth:F0}";
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 20, 100, 30), text);
        }
    }
}
