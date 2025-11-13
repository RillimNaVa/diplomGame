using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public Slider healthBar;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI timerText;

    void Start()
    {
        // Привязка (drag в Inspector)
    }

    public void UpdateHealthBar(float current, float max)
    {
        healthBar.value = current / max;
    }

    public void UpdateWave(int wave) { waveText.text = $"Wave: {wave}"; }
    public void UpdateTimer(float time) { timerText.text = $"Time: {Mathf.CeilToInt(time)}s"; }
}