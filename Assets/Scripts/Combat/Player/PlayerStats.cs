using System;
using UnityEngine;

/// <summary>
/// Central numeric-parameter provider for player-facing systems (heal amounts,
/// streak thresholds, speed multipliers). Future upgrades mutate fields here;
/// other components read from this single source of truth.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Healing")]
    public float orbHealAmount = 5f;
    public float gloryHealAmount = 25f;

    [Header("Glory Kill")]
    public float gloryBonusDamage = 999f;

    [Header("Kill Streak")]
    public int streakThreshold = 5;
    public float streakWindowSeconds = 10f;
    public float streakBoostMultiplier = 1.2f;
    public float streakBoostDuration = 5f;

    public event Action OnStatsChanged;

    public void NotifyStatsChanged() => OnStatsChanged?.Invoke();
}
