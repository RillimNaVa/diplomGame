using System;
using UnityEngine;

// Owns the player's weapon slots, routes input from PlayerController, and forwards
// state to UI via events. Public API is intentionally stable across PR A and PR B
// so future work (switching, ammo HUD, pickups) plugs in without rewrites.
public class WeaponManager : MonoBehaviour
{
    public const int SlotCount = 5;

    [Header("References")]
    [Tooltip("Player root — used to skip own colliders in hit tests.")]
    [SerializeField] private Transform owner;
    [Tooltip("Camera used as aim source. Required.")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Layers considered valid hit targets for hitscan/melee.")]
    [SerializeField] private LayerMask hitMask = ~0;
    [Tooltip("Player health — manager unsubs from input when this dies.")]
    [SerializeField] private Health ownerHealth;

    [Header("Weapons")]
    [Tooltip("Weapon instances by slot (0-4). Empty slots can be left null.")]
    [SerializeField] private WeaponBase[] slots = new WeaponBase[SlotCount];

    [Tooltip("Slot equipped on Start.")]
    [Range(0, 4)] [SerializeField] private int defaultSlot = 0;

    private WeaponBase currentWeapon;
    private int currentSlot = -1;
    private bool ownerIsDead;

    public WeaponBase CurrentWeapon => currentWeapon;
    public int CurrentSlot => currentSlot;
    public bool IsSlotOccupied(int slot) => slot >= 0 && slot < slots.Length && slots[slot] != null;

    public event Action<WeaponBase> OnWeaponEquipped;
    public event Action<WeaponBase, int, int> OnAmmoChanged;

    void Awake()
    {
        if (owner == null) owner = transform;
        if (ownerHealth == null) ownerHealth = owner.GetComponent<Health>();
    }

    void Start()
    {
        InitializeAllWeapons();

        if (ownerHealth != null && ownerHealth.onDeath != null)
        {
            ownerHealth.onDeath.AddListener(OnOwnerDied);
        }

        EquipSlot(defaultSlot);
    }

    void OnDestroy()
    {
        if (ownerHealth != null && ownerHealth.onDeath != null)
        {
            ownerHealth.onDeath.RemoveListener(OnOwnerDied);
        }

        if (currentWeapon != null)
        {
            currentWeapon.OnAmmoChanged -= ForwardAmmoChanged;
        }
    }

    void Update()
    {
        if (ownerIsDead) return;
        if (currentWeapon != null) currentWeapon.Tick(Time.deltaTime);
    }

    // --- Public API used by PlayerController ---

    public void SetFireHeld(bool held)
    {
        if (ownerIsDead || currentWeapon == null) return;
        if (held) currentWeapon.TryStartFire();
        else currentWeapon.TryStopFire();
    }

    public void Reload()
    {
        if (ownerIsDead || currentWeapon == null) return;
        currentWeapon.TryReload();
    }

    public void EquipSlot(int slot)
    {
        if (ownerIsDead) return;
        if (slot < 0 || slot >= slots.Length) return;
        if (slots[slot] == null) return;
        if (slot == currentSlot) return;

        if (currentWeapon != null)
        {
            currentWeapon.OnAmmoChanged -= ForwardAmmoChanged;
            currentWeapon.OnUnequip();
        }

        currentSlot = slot;
        currentWeapon = slots[slot];
        currentWeapon.OnEquip();
        currentWeapon.OnAmmoChanged += ForwardAmmoChanged;

        OnWeaponEquipped?.Invoke(currentWeapon);
        ForwardAmmoChanged(currentWeapon);
    }

    // direction: +1 = next, -1 = previous. Skips empty slots, wraps around.
    public void CycleSlot(int direction)
    {
        if (ownerIsDead) return;
        if (direction == 0) return;
        int step = direction > 0 ? 1 : -1;
        for (int i = 1; i <= slots.Length; i++)
        {
            int candidate = ((currentSlot + step * i) % slots.Length + slots.Length) % slots.Length;
            if (slots[candidate] != null)
            {
                EquipSlot(candidate);
                return;
            }
        }
    }

    // --- Internals ---

    private void InitializeAllWeapons()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            WeaponBase w = slots[i];
            if (w == null) continue;

            // Each weapon gets its own context so MuzzleTransform mutations on one
            // can't bleed into another (defensive — currently only the equipped one ticks).
            WeaponContext ctx = new WeaponContext(owner, cameraTransform, hitMask, this);
            w.Initialize(ctx);
            w.gameObject.SetActive(false);
        }
    }

    private void OnOwnerDied()
    {
        ownerIsDead = true;
        if (currentWeapon != null) currentWeapon.TryStopFire();
    }

    private void ForwardAmmoChanged(WeaponBase weapon)
    {
        if (weapon == null) return;
        OnAmmoChanged?.Invoke(weapon, weapon.CurrentClipAmmo, weapon.CurrentReserveAmmo);
    }
}
