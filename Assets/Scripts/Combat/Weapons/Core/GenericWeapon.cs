using UnityEngine;

// Default WeaponBase implementation. Behavior is fully driven by WeaponDefinition
// + FireMode strategy assigned in the inspector. All 5 Phase 1 weapons use this
// component — no per-weapon C# subclass required.
[AddComponentMenu("Void Survivor/Weapons/Generic Weapon")]
public class GenericWeapon : WeaponBase
{
    public override void Initialize(WeaponContext ctx)
    {
        base.Initialize(ctx);

        // Auto-attach melee feedback components (swing arc, blade trail, sparks)
        // so any melee-category weapon prefab gets the polish without per-weapon
        // editor wiring.
        if (definition != null && definition.weaponCategory == WeaponCategory.Melee)
        {
            if (GetComponent<MeleeSwingFeedback>() == null) gameObject.AddComponent<MeleeSwingFeedback>();
            if (GetComponent<BladeTrail>() == null) gameObject.AddComponent<BladeTrail>();
            if (GetComponent<MeleeImpactSparks>() == null)
            {
                var sparks = gameObject.AddComponent<MeleeImpactSparks>();
                if (ctx != null) sparks.hitMask = ctx.HitMask;
            }
        }
    }
}
