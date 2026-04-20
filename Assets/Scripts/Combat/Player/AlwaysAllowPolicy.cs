using UnityEngine;

/// <summary>
/// Default glory-kill policy: any staggered target, anytime. Future upgrades
/// swap this component on the Player for StreakRequiredPolicy / ChargeLimitPolicy
/// / etc.
/// </summary>
public class AlwaysAllowPolicy : MonoBehaviour, IGloryKillPolicy
{
    public bool CanGloryKill(GloryKillContext ctx) => true;
    public void NotifyGloryKillPerformed(GloryKillContext ctx) { }
}
