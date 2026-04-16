using UnityEngine;

// Default WeaponBase implementation. Behavior is fully driven by WeaponDefinition
// + FireMode strategy assigned in the inspector. All 5 Phase 1 weapons use this
// component — no per-weapon C# subclass required.
[AddComponentMenu("Void Survivor/Weapons/Generic Weapon")]
public class GenericWeapon : WeaponBase
{
}
