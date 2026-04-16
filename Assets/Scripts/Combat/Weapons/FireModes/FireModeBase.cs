using System;

// Strategy class that performs the actual attack.
// WeaponBase decrements ammo and starts cooldown BEFORE calling ExecuteFire,
// so fire modes do not need to touch ammo state.
//
// [Serializable] is required so [SerializeReference] in WeaponDefinition can
// instantiate concrete subclasses from the inspector.
[Serializable]
public abstract class FireModeBase
{
    public abstract void ExecuteFire(
        WeaponContext context,
        WeaponDefinition definition,
        WeaponBase weapon);
}
