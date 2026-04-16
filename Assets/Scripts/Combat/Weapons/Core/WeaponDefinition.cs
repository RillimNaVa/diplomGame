using UnityEngine;

// Static configuration of a single weapon. One asset per weapon, edited in inspector.
// Runtime state (ammo, cooldown) lives on WeaponBase, not here.
//
// fireMode uses [SerializeReference] so the inspector can pick any FireModeBase
// subclass per asset. Adding a new fire mode requires zero code changes here.
[CreateAssetMenu(menuName = "Void Survivor/Weapon Definition", fileName = "Weapon_Def")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public string weaponId;
    public string displayName;
    [Range(0, 4)] public int slotIndex;
    public WeaponCategory weaponCategory;

    [Header("Firing")]
    public float damage = 25f;
    [Tooltip("Shots per second.")]
    public float fireRate = 5f;
    [Tooltip("Max ray distance for hitscan/melee. Ignored by projectile fire mode.")]
    public float range = 100f;
    [Tooltip("If true, weapon fires while trigger is held. If false, one shot per press.")]
    public bool isAutomatic;

    [Header("Ammo")]
    public bool usesAmmo;
    public int clipSize = 30;
    public int maxReserveAmmo = 90;
    [Tooltip("Seconds to complete a magazine reload. Ignored if usesAmmo is false.")]
    public float reloadDuration = 1.5f;

    [Header("Shotgun (only read by ShotgunFireMode)")]
    public int pelletCount = 1;
    [Tooltip("Spread cone half-angle in degrees.")]
    public float spreadAngle = 0f;

    [Header("Projectile (only read by ProjectileFireMode)")]
    public Projectile projectilePrefab;

    [Header("Visuals")]
    [Tooltip("Optional: prefab spawned when this weapon is first equipped if no viewmodel exists.")]
    public GameObject viewModelPrefab;
    [Tooltip("Optional: tracer line prefab spawned per shot by hitscan/shotgun fire modes.")]
    public LineRenderer tracerPrefab;
    [Tooltip("Optional: animator trigger fired on each successful shot.")]
    public string animTriggerName;

    [Header("Behavior")]
    [SerializeReference] public FireModeBase fireMode;

    public float FireCooldown => fireRate > 0f ? 1f / fireRate : 0f;
}
