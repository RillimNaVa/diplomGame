using UnityEngine;

// Bundles execution dependencies passed from WeaponManager into each WeaponBase.
// Fire modes read aim/owner data from here without touching the scene directly.
// MuzzleTransform is filled in by WeaponBase right before each shot, since the
// muzzle is a per-weapon child object.
public class WeaponContext
{
    public Transform Owner;
    public Transform CameraTransform;
    public Transform MuzzleTransform;
    public LayerMask HitMask;
    public MonoBehaviour CoroutineHost;

    public WeaponContext(
        Transform owner,
        Transform cameraTransform,
        LayerMask hitMask,
        MonoBehaviour coroutineHost)
    {
        Owner = owner;
        CameraTransform = cameraTransform;
        MuzzleTransform = null;
        HitMask = hitMask;
        CoroutineHost = coroutineHost;
    }
}
