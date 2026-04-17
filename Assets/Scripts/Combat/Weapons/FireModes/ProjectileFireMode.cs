using System;
using UnityEngine;

// Spawns a Projectile prefab from the muzzle pointed along camera forward.
// Uses the existing Projectile.Launch API so the current prefab workflow keeps working.
//
// Friendly-fire safety: on spawn, every collider on the projectile is set to
// ignore every collider on the owner root. No layer setup required.
[Serializable]
public class ProjectileFireMode : FireModeBase
{
    public override void ExecuteFire(
        WeaponContext context,
        WeaponDefinition definition,
        WeaponBase weapon)
    {
        if (context == null || context.CameraTransform == null) return;
        if (definition.projectilePrefab == null) return;

        Transform muzzle = context.MuzzleTransform != null
            ? context.MuzzleTransform
            : context.CameraTransform;

        Vector3 spawnPos = muzzle.position;
        Vector3 direction = context.CameraTransform.forward;
        Quaternion rotation = Quaternion.LookRotation(direction);

        Projectile projectile = UnityEngine.Object.Instantiate(
            definition.projectilePrefab,
            spawnPos,
            rotation);

        IgnoreOwnerCollisions(projectile, context.Owner);

        projectile.Launch(direction, definition.damage);
    }

    private static void IgnoreOwnerCollisions(Projectile projectile, Transform owner)
    {
        if (projectile == null || owner == null) return;

        Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>();
        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        for (int i = 0; i < projectileColliders.Length; i++)
        {
            for (int j = 0; j < ownerColliders.Length; j++)
            {
                Physics.IgnoreCollision(projectileColliders[i], ownerColliders[j], true);
            }
        }
    }
}
