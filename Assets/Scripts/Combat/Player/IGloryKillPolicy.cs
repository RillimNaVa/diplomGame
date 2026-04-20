using UnityEngine;

/// <summary>
/// Runtime context passed to glory-kill policies at decision time. Lets future
/// policies gate on streak, charges, weapon-specific rules, etc., without
/// changing the detector's call site.
/// </summary>
public struct GloryKillContext
{
    public Health target;
    public WeaponBase weapon;
    public KillStreakTracker streakTracker;
}

/// <summary>
/// Seam #3: decides whether a glory kill is allowed right now. Swap the
/// MonoBehaviour on the Player to change rules (streak-required, charge-limit,
/// etc.) without touching GloryKillDetector.
/// </summary>
public interface IGloryKillPolicy
{
    bool CanGloryKill(GloryKillContext ctx);
    void NotifyGloryKillPerformed(GloryKillContext ctx);
}
