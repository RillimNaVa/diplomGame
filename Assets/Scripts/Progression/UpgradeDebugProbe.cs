using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Phase 4 / PR 4.PB — temporary debug component for verifying that upgrades
// flow into the gameplay layer end-to-end. Discovers all UpgradeData assets
// under Resources/Progression/Upgrades/ and exposes hotkeys:
//
//   F9  — add a random eligible upgrade (skips maxed)
//   F10 — log current modifier state for all enum types
//   F11 — ResetForNewRun
//
// Auto-attaches to the player GameObject via GameManager.ResolveReferences.
// Will be removed once PR 4.PC ships the real reward-card UI.
public class UpgradeDebugProbe : MonoBehaviour
{
    [Tooltip("Resource path under Assets/Resources/. Default = 'Progression/Upgrades'.")]
    public string resourcePath = "Progression/Upgrades";

    [Tooltip("If true, log every Add/Reset to the console.")]
    public bool verbose = true;

    UpgradeData[] pool;
    System.Random rng = new System.Random();

    void Awake()
    {
        pool = Resources.LoadAll<UpgradeData>(resourcePath);
        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning($"[UpgradeDebugProbe] No UpgradeData found under Resources/{resourcePath}");
        }
        else if (verbose)
        {
            Debug.Log($"[UpgradeDebugProbe] Loaded {pool.Length} upgrades. Hotkeys: F9 add random, F10 log modifiers, F11 reset run.");
        }
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f9Key.wasPressedThisFrame) TryAddRandom();
        if (kb.f10Key.wasPressedThisFrame) LogModifiers();
        if (kb.f11Key.wasPressedThisFrame) ResetRun();
    }

    void TryAddRandom()
    {
        if (pool == null || pool.Length == 0) return;
        UpgradeSystem sys = UpgradeSystem.Instance;
        if (sys == null) return;

        List<UpgradeData> eligible = new List<UpgradeData>();
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] != null && sys.CanAdd(pool[i])) eligible.Add(pool[i]);
        }
        if (eligible.Count == 0)
        {
            Debug.Log("[UpgradeDebugProbe] All upgrades maxed.");
            return;
        }
        UpgradeData pick = eligible[rng.Next(eligible.Count)];
        sys.AddUpgrade(pick);
        if (verbose)
        {
            int stacks = sys.GetStackCount(pick.id);
            Debug.Log($"[UpgradeDebugProbe] +{pick.displayName} (stack {stacks}/{pick.maxStacks})");
        }
    }

    void LogModifiers()
    {
        UpgradeSystem sys = UpgradeSystem.Instance;
        if (sys == null) { Debug.Log("[UpgradeDebugProbe] No UpgradeSystem."); return; }
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"[UpgradeDebugProbe] Active stacks: {sys.ActiveUpgrades.Count}");
        foreach (ActiveUpgradeStack s in sys.ActiveUpgrades)
        {
            sb.AppendLine($"  - {s.data.displayName} x{s.stacks} (effect={s.data.effectType}, valueA={s.data.valueA})");
        }
        sb.AppendLine("Modifiers (non-zero):");
        foreach (UpgradeEffectType t in System.Enum.GetValues(typeof(UpgradeEffectType)))
        {
            float add = sys.GetAdditive(t);
            if (Mathf.Abs(add) > 0.0001f)
            {
                sb.AppendLine($"  {t}: additive={add:F3}, multiplier={sys.GetMultiplier(t):F3}");
            }
        }
        Debug.Log(sb.ToString());
    }

    void ResetRun()
    {
        UpgradeSystem sys = UpgradeSystem.Instance;
        if (sys == null) return;
        sys.ResetForNewRun();
        if (verbose) Debug.Log("[UpgradeDebugProbe] Run reset.");
    }
}
