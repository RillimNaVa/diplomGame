using System;
using UnityEngine;

// Phase 4 / PR 4.PE — Kill Points wallet.
//
// Auto-creating scene singleton (mirrors UpgradeSystem / EnemyPool). Holds the
// run-scoped KP total. Awarded by RunProgressionController on arena clear via
// ArenaPayoutCalculator; spent by Shop room (PR 4.PF).
//
// KP is run-scoped: ResetForNewRun zeroes it. See TZ §12, §16.
public class KillPointsWallet : MonoBehaviour
{
    static KillPointsWallet s_instance;

    int total;

    public int Total => total;

    /// <summary>Fired on every Add or Spend. Args: (newTotal, signedDelta).</summary>
    public event Action<int, int> OnTotalChanged;

    public static KillPointsWallet Instance
    {
        get
        {
            if (s_instance != null) return s_instance;
            var found =
#if UNITY_2023_1_OR_NEWER
                UnityEngine.Object.FindFirstObjectByType<KillPointsWallet>();
#else
                UnityEngine.Object.FindObjectOfType<KillPointsWallet>();
#endif
            if (found != null) { s_instance = found; return s_instance; }
            var go = new GameObject("KillPointsWallet");
            s_instance = go.AddComponent<KillPointsWallet>();
            return s_instance;
        }
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this) { Destroy(this); return; }
        s_instance = this;
    }

    void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        total += amount;
        OnTotalChanged?.Invoke(total, amount);
    }

    /// <summary>Returns true iff the spend went through.</summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return false;
        if (total < amount) return false;
        total -= amount;
        OnTotalChanged?.Invoke(total, -amount);
        return true;
    }

    public bool CanAfford(int amount) => amount > 0 && total >= amount;

    public void ResetForNewRun()
    {
        if (total == 0) { OnTotalChanged?.Invoke(0, 0); return; }
        int prev = total;
        total = 0;
        OnTotalChanged?.Invoke(0, -prev);
    }
}
