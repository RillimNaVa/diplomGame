using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent<float> onTakeDamage;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        onTakeDamage?.Invoke(damage);

        // ★ ВЫЗЫВАЕМ UI ★
        if (CompareTag("Player"))
        {
            GameManager.instance?.UpdatePlayerHealth(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
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

    // --- GUI HP ---
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