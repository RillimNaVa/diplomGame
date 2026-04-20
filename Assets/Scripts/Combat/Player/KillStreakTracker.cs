using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Counts enemy kills in a sliding time window. When the count crosses
/// <see cref="PlayerStats.streakThreshold"/> upward, applies a speed-boost
/// multiplier to the PlayerController for <see cref="PlayerStats.streakBoostDuration"/>
/// seconds. Drives events that a HUD can later subscribe to.
/// </summary>
public class KillStreakTracker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-resolved from this GameObject if empty.")]
    public PlayerStats playerStats;
    [Tooltip("Auto-resolved from this GameObject if empty.")]
    public PlayerController playerController;

    public int CurrentStreak { get; private set; }
    public bool IsBoostActive { get; private set; }

    public event Action<int> OnStreakChanged;
    public event Action<bool> OnBoostChanged;

    private readonly List<float> killTimestamps = new List<float>();
    private float boostExpiresAt;
    private bool subscribedToGM;

    void Awake()
    {
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    void OnEnable()
    {
        TrySubscribeToGameManager();
    }

    void Start()
    {
        // GameManager.instance is set in its own Awake; if this ran first, retry here.
        TrySubscribeToGameManager();
    }

    void OnDisable()
    {
        if (subscribedToGM && GameManager.instance != null)
        {
            GameManager.instance.OnEnemyKilled -= HandleEnemyKilled;
        }
        subscribedToGM = false;
    }

    void TrySubscribeToGameManager()
    {
        if (subscribedToGM) return;
        if (GameManager.instance == null) return;
        GameManager.instance.OnEnemyKilled += HandleEnemyKilled;
        subscribedToGM = true;
    }

    void Update()
    {
        if (playerStats == null) return;

        // Evict kills that have aged out of the streak window.
        float cutoff = Time.time - playerStats.streakWindowSeconds;
        int evicted = 0;
        while (killTimestamps.Count > 0 && killTimestamps[0] < cutoff)
        {
            killTimestamps.RemoveAt(0);
            evicted++;
        }
        if (evicted > 0)
        {
            SetStreak(killTimestamps.Count);
        }

        if (IsBoostActive && Time.time >= boostExpiresAt)
        {
            SetBoostActive(false);
        }
    }

    void HandleEnemyKilled()
    {
        if (playerStats == null) return;

        killTimestamps.Add(Time.time);
        int newStreak = killTimestamps.Count;

        // Threshold crossing triggers (or refreshes) the boost.
        if (newStreak >= playerStats.streakThreshold)
        {
            boostExpiresAt = Time.time + playerStats.streakBoostDuration;
            if (!IsBoostActive)
            {
                SetBoostActive(true);
            }
        }

        SetStreak(newStreak);
    }

    void SetStreak(int value)
    {
        if (CurrentStreak == value) return;
        CurrentStreak = value;
        OnStreakChanged?.Invoke(value);
    }

    void SetBoostActive(bool value)
    {
        if (IsBoostActive == value) return;
        IsBoostActive = value;

        if (playerController != null)
        {
            float mul = value && playerStats != null ? playerStats.streakBoostMultiplier : 1f;
            playerController.SetSpeedMultiplier(mul);
        }

        OnBoostChanged?.Invoke(value);
    }
}
