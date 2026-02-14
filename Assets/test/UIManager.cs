using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public Slider healthBar;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI timerText;

    public void UpdateHealthBar(float current, float max)
    {
        if (healthBar == null || max <= 0f) return;
        healthBar.value = current / max;
    }

    public void UpdateWave(int wave)
    {
        if (waveText == null) return;
        waveText.text = $"Wave: {wave}";
    }

    public void UpdateTimer(float time)
    {
        if (timerText == null) return;
        timerText.text = $"Next wave in: {Mathf.CeilToInt(Mathf.Max(0f, time))}s";
    }

    public void ShowWaveState(string message)
    {
        if (waveText == null) return;
        waveText.text = message;
    }
}
