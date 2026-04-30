using System;
using System.Collections;
using UnityEngine;

// Abstract weapon component. Lives on the same GameObject as the viewmodel
// so SetActive(false) hides both the script and the visual at once.
//
// Holds runtime state directly (no separate WeaponRuntimeState class) — keeps
// the call graph short and inspector debugging trivial.
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] protected WeaponDefinition definition;

    [Header("Per-Instance References")]
    [Tooltip("Where tracers, muzzle flashes, and projectiles spawn from.")]
    [SerializeField] protected Transform muzzlePoint;
    [Tooltip("Optional scene-attached muzzle flash particle system, played on every shot.")]
    [SerializeField] protected ParticleSystem muzzleFlash;
    [Tooltip("Optional animator on the viewmodel; auto-resolved from children if null.")]
    [SerializeField] protected Animator animator;

    protected WeaponContext context;
    protected float nextFireTime;
    protected int currentClipAmmo;
    protected int currentReserveAmmo;
    protected bool isReloading;
    protected bool isEquipped;
    protected bool isTriggerHeld;

    public WeaponDefinition Definition => definition;
    public bool IsEquipped => isEquipped;
    public int CurrentClipAmmo => currentClipAmmo;
    public int CurrentReserveAmmo => currentReserveAmmo;
    public bool IsReloading => isReloading;
    public Transform MuzzlePoint => muzzlePoint;

    public event Action<WeaponBase> OnFired;
    public event Action<WeaponBase> OnAmmoChanged;
    public event Action<WeaponBase> OnReloadStarted;
    public event Action<WeaponBase> OnReloadCompleted;

    public virtual void Initialize(WeaponContext ctx)
    {
        context = ctx;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (definition != null && definition.usesAmmo)
        {
            currentClipAmmo = Mathf.Clamp(definition.clipSize, 0, definition.clipSize);
            currentReserveAmmo = Mathf.Clamp(definition.maxReserveAmmo, 0, definition.maxReserveAmmo);
        }
        else
        {
            currentClipAmmo = 0;
            currentReserveAmmo = 0;
        }

        nextFireTime = 0f;
        isReloading = false;
        isTriggerHeld = false;
    }

    public virtual void OnEquip()
    {
        isEquipped = true;
        gameObject.SetActive(true);
        isTriggerHeld = false;
    }

    public virtual void OnUnequip()
    {
        isEquipped = false;
        isTriggerHeld = false;
        if (isReloading)
        {
            CancelReload();
        }
        gameObject.SetActive(false);
    }

    // Called every frame by WeaponManager while equipped.
    public virtual void Tick(float deltaTime)
    {
        if (!isEquipped || definition == null) return;
        if (isReloading) return;

        // Semi-auto fires once on TryStartFire; only auto needs per-tick logic.
        if (definition.isAutomatic && isTriggerHeld)
        {
            TryFireOnce();
        }
    }

    public virtual void TryStartFire()
    {
        if (!isEquipped || definition == null) return;
        isTriggerHeld = true;

        // Semi-auto: fire immediately on press; further presses needed for next shot.
        if (!definition.isAutomatic)
        {
            TryFireOnce();
        }
    }

    public virtual void TryStopFire()
    {
        isTriggerHeld = false;
    }

    public virtual void TryReload()
    {
        if (!isEquipped || definition == null) return;
        if (!definition.usesAmmo) return;
        if (isReloading) return;
        if (currentClipAmmo >= definition.clipSize) return;
        if (currentReserveAmmo <= 0) return;

        if (context != null && context.CoroutineHost != null && definition.reloadDuration > 0f)
        {
            context.CoroutineHost.StartCoroutine(ReloadCoroutine());
        }
        else
        {
            FinishReload();
        }
    }

    // Adds ammo to reserve. Called by pickups (Phase 3).
    public virtual void AddAmmo(int amount)
    {
        if (definition == null || !definition.usesAmmo) return;
        currentReserveAmmo = Mathf.Min(currentReserveAmmo + amount, definition.maxReserveAmmo);
        OnAmmoChanged?.Invoke(this);
    }

    protected bool TryFireOnce()
    {
        if (Time.time < nextFireTime) return false;
        if (definition.fireMode == null) return false;

        if (definition.usesAmmo)
        {
            if (currentClipAmmo <= 0)
            {
                return false;
            }
            currentClipAmmo--;
            OnAmmoChanged?.Invoke(this);
        }

        if (context != null)
        {
            context.MuzzleTransform = muzzlePoint;
            definition.fireMode.ExecuteFire(context, definition, this);
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        else if (muzzlePoint != null)
        {
            // PR 5.C — runtime fallback so weapons without an authored Particle
            // System still get a muzzle flash burst. ImpactFXSystem auto-creates.
            ImpactFXSystem.Instance.SpawnMuzzleFlash(muzzlePoint.position, muzzlePoint.rotation);
        }

        TriggerFireAnim();
        nextFireTime = Time.time + definition.FireCooldown;
        OnFired?.Invoke(this);
        return true;
    }

    protected void TriggerFireAnim()
    {
        if (animator == null || string.IsNullOrEmpty(definition.animTriggerName)) return;
        animator.ResetTrigger(definition.animTriggerName);
        animator.SetTrigger(definition.animTriggerName);
    }

    protected IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        OnReloadStarted?.Invoke(this);
        yield return new WaitForSeconds(definition.reloadDuration);
        if (isReloading)
        {
            FinishReload();
        }
    }

    protected void FinishReload()
    {
        int needed = definition.clipSize - currentClipAmmo;
        int taken = Mathf.Min(needed, currentReserveAmmo);
        currentClipAmmo += taken;
        currentReserveAmmo -= taken;
        isReloading = false;
        OnAmmoChanged?.Invoke(this);
        OnReloadCompleted?.Invoke(this);
    }

    protected void CancelReload()
    {
        isReloading = false;
    }
}
