using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public Slider healthBar;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI timerText;

    // CombatHUDController raises this on Start so legacy widgets stop drawing
    // without us having to remove existing GameManager call sites.
    public bool suppressLegacyHud;

    public void SuppressLegacyHud()
    {
        suppressLegacyHud = true;
        if (healthBar != null) healthBar.gameObject.SetActive(false);
        if (waveText != null) waveText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
    }

    public void UpdateHealthBar(float current, float max)
    {
        if (suppressLegacyHud) return;
        if (healthBar == null || max <= 0f) return;
        healthBar.value = current / max;
    }

    public void UpdateWave(int wave)
    {
        if (suppressLegacyHud) return;
        if (waveText == null) return;
        waveText.text = $"Wave: {wave}";
    }

    public void UpdateTimer(float time)
    {
        if (suppressLegacyHud) return;
        if (timerText == null) return;
        timerText.text = $"Next wave in: {Mathf.CeilToInt(Mathf.Max(0f, time))}s";
    }

    public void ShowWaveState(string message)
    {
        if (suppressLegacyHud) return;
        if (waveText == null) return;
        waveText.text = message;
    }
}
