using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarView : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Image fillImage;

    private void OnEnable()
    {
        if (health == null)
        {
            health = GetComponentInParent<Health>();
        }

        if (health != null)
        {
            health.onHealthChanged.AddListener(UpdateFill);
            UpdateFill(health.currentHealth, health.maxHealth);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onHealthChanged.RemoveListener(UpdateFill);
        }
    }

    public void SetHealth(Health targetHealth)
    {
        if (health != null)
        {
            health.onHealthChanged.RemoveListener(UpdateFill);
        }

        health = targetHealth;

        if (health != null)
        {
            health.onHealthChanged.AddListener(UpdateFill);
            UpdateFill(health.currentHealth, health.maxHealth);
        }
    }

    private void UpdateFill(float current, float max)
    {
        if (fillImage == null) return;

        var ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        fillImage.fillAmount = ratio;
    }
}
