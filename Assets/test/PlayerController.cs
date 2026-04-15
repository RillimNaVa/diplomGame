using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]


public class PlayerController : MonoBehaviour
{
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int RecoilStateHash = Animator.StringToHash("recoil");

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    public float airControlMultiplier = 1f;

    [Header("Jump")]
    public int maxJumps = 2;

    [Header("Dash")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public int maxDashCharges = 2;
    public float dashChargeCooldown = 3f;

    [Header("Slide")]
    public float slideSpeed = 18f;
    public float slideDuration = 0.8f;
    public float slideCooldown = 1f;
    public float slideCameraHeight = 0.8f;
    public float slideCameraLerpSpeed = 12f;

    [Header("Momentum")]
    public float momentumDecayRate = 15f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 1.5f;
    public Transform cameraTransform;

    [Header("Weapon Sway")]
    public Transform weaponHolder;
    [Range(0f, 1f)] public float weaponTiltAmount = 0.35f;
    public float weaponSwaySmoothing = 10f;

    [Header("Shooting")]
    public float fireRate = 0.2f;
    public float damage = 25f;
    public LayerMask enemyLayer = -1;
    public Transform firePoint;
    public LineRenderer shotTracerPrefab;
    public ParticleSystem muzzleFlash;
    public ParticleSystem hitEffectPrefab;
    public float tracerDuration = 0.05f;
    public Projectile projectilePrefab;
    public Transform shootOrigin;
    [SerializeField] private Animator gunAnimator;

    [Header("Melee")]
    public float meleeDamage = 40f;
    public float meleeRange = 2f;
    public float meleeCooldown = 0.6f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation;
    private float nextFireTime;
    private float nextMeleeTime;

    // Jump
    private int jumpCount;

    // Dash
    private bool isDashing;
    private float dashTimer;
    private Vector3 dashDirection;
    private int dashCharges;
    private float dashRechargeTimer;

    // Slide
    private bool isSliding;
    private float slideTimer;
    private float slideCooldownTimer;
    private Vector3 slideDirection;
    private float originalHeight;
    private Vector3 originalCenter;
    private float defaultCameraY;

    // Momentum
    private float currentSpeed;

    // Weapon sway
    private Quaternion weaponBaseLocalRotation;

    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool dashPressed;
    private bool slideHeld;
    private bool fireRequested;
    private bool meleePressed;


    void Start()
    {
        controller = GetComponent<CharacterController>();

        originalHeight = controller.height;
        originalCenter = controller.center;

        dashCharges = maxDashCharges;
        currentSpeed = moveSpeed;

        if (gunAnimator == null)
        {
            gunAnimator = GetComponentInChildren<Animator>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null)
        {
            GameObject cam = new GameObject("PlayerCamera");
            cam.transform.SetParent(transform);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.identity;
            cameraTransform = cam.transform;
            cam.AddComponent<Camera>();
        }

        defaultCameraY = cameraTransform.localPosition.y;

        if (weaponHolder != null)
        {
            weaponBaseLocalRotation = weaponHolder.localRotation;
        }

        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(cameraTransform);
            fp.transform.localPosition = new Vector3(0.2f, -0.15f, 0.5f);
            firePoint = fp.transform;
        }

        if (shootOrigin == null)
        {
            shootOrigin = firePoint;
        }

    }

    void Update()
    {
        HandleMovement();
        HandleLook();
        HandleCombat();
    }

    void HandleMovement()
    {
        // 1. Ground check + reset
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            jumpCount = 0;
        }

        // 2. Cooldown ticks
        if (slideCooldownTimer > 0f) slideCooldownTimer -= Time.deltaTime;

        if (dashCharges < maxDashCharges)
        {
            dashRechargeTimer += Time.deltaTime;
            if (dashRechargeTimer >= dashChargeCooldown)
            {
                dashCharges++;
                dashRechargeTimer = 0f;
            }
        }

        // 3. Movement direction (camera-relative via player rotation)
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        // 4. Dash start
        if (dashPressed && !isDashing && dashCharges > 0)
        {
            dashCharges--;
            isDashing = true;
            dashTimer = dashDuration;
            dashDirection = move.magnitude > 0.1f ? move.normalized : transform.forward;
            velocity.y = 0f;
            currentSpeed = dashSpeed;
        }
        dashPressed = false;

        // 5. Dash tick
        if (isDashing)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }

        // 6. Slide start
        if (slideHeld && !isSliding && !isDashing && isGrounded
            && move.magnitude > 0.1f && slideCooldownTimer <= 0f)
        {
            StartSlide(move.normalized);
        }

        // 7. Slide tick
        if (isSliding)
        {
            controller.Move(slideDirection * currentSpeed * Time.deltaTime);
            slideTimer -= Time.deltaTime;

            // End conditions: timer expired or button released
            if (slideTimer <= 0f || !slideHeld)
            {
                EndSlide();
            }
        }

        // 8. Normal movement (skipped during dash; reduced during slide)
        if (!isDashing && !isSliding)
        {
            float airMul = isGrounded ? 1f : airControlMultiplier;
            controller.Move(move * currentSpeed * airMul * Time.deltaTime);
        }

        // 9. Momentum decay toward base speed (only when not actively boosted)
        if (!isDashing)
        {
            float target = isSliding ? slideSpeed : moveSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, momentumDecayRate * Time.deltaTime);
        }

        // 10. Jump (can cancel slide; works in air up to maxJumps)
        if (jumpPressed && jumpCount < maxJumps)
        {
            if (isSliding) EndSlide();
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;
            jumpPressed = false;
        }
        jumpPressed = false;

        // 11. Gravity + vertical movement
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 12. Camera Y lerp for slide feel
        if (cameraTransform != null)
        {
            float targetY = isSliding ? slideCameraHeight : defaultCameraY;
            Vector3 lp = cameraTransform.localPosition;
            lp.y = Mathf.Lerp(lp.y, targetY, slideCameraLerpSpeed * Time.deltaTime);
            cameraTransform.localPosition = lp;
        }
    }

    private void StartSlide(Vector3 dir)
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;
        slideDirection = dir;

        // Inherit momentum from dash; otherwise use slideSpeed
        if (currentSpeed < slideSpeed) currentSpeed = slideSpeed;

        // Note: CharacterController size is intentionally NOT shrunk during slide.
        // Repeatedly resizing the controller caused the player to fall through floors.
        // The visual crouch effect comes from the camera dip in HandleMovement.
        // When low-ceiling arenas are added (Phase 2), revisit this with proper handling.
    }

    private void EndSlide()
    {
        isSliding = false;
    }

    void HandleLook()
    {
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        xRotation -= lookInput.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Weapon tilts proportionally to camera pitch, smoothed toward target
        if (weaponHolder != null)
        {
            Quaternion targetTilt = weaponBaseLocalRotation *
                                    Quaternion.Euler(xRotation * weaponTiltAmount, 0f, 0f);
            weaponHolder.localRotation = Quaternion.Slerp(
                weaponHolder.localRotation,
                targetTilt,
                weaponSwaySmoothing * Time.deltaTime);
        }
    }

    void HandleCombat()
    {
        if (fireRequested && Time.time >= nextFireTime)
        {
            PlayShootAnim();
            Shoot();
            nextFireTime = Time.time + fireRate;

            fireRequested = false;
        }

        if (meleePressed && Time.time >= nextMeleeTime)
        {
            MeleeAttack();
            nextMeleeTime = Time.time + meleeCooldown;
            meleePressed = false;
        }
    }

    void Shoot()
    {
        const float maxDistance = 100f;
        Vector3 origin = firePoint != null ? firePoint.position : cameraTransform.position;

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Direction is always camera.forward — avoids parallax bug when firePoint
        // is offset from the camera (e.g., during fast slides/dashes, bullets curved
        // towards movement direction because direction was calculated from firePoint
        // to a camera-space hit point).
        Vector3 direction = cameraTransform.forward;
        Vector3 endPoint = origin + direction * maxDistance;

        Ray centerRay = new Ray(cameraTransform.position, direction);
        if (Physics.Raycast(centerRay, out RaycastHit centerHit, maxDistance))
        {
            endPoint = centerHit.point;
        }

        if (projectilePrefab != null && shootOrigin != null)
        {
            Quaternion shotRotation = Quaternion.LookRotation(direction);
            Projectile projectile = Instantiate(projectilePrefab, shootOrigin.position, shotRotation);
            projectile.Launch(direction, damage);
            return;
        }

        Debug.DrawRay(origin, direction * maxDistance, Color.red, 0.5f);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, enemyLayer))
        {
            endPoint = hit.point;

            Health target = hit.collider.GetComponent<Health>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }

            if (hitEffectPrefab != null)
            {
                ParticleSystem hitFx = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(hitFx.gameObject, hitFx.main.duration + hitFx.main.startLifetime.constantMax);
            }
        }

        if (shotTracerPrefab != null)
        {
            StartCoroutine(PlayTracer(origin, endPoint));
        }
    }

    private IEnumerator PlayTracer(Vector3 origin, Vector3 endPoint)
    {
        LineRenderer tracer = Instantiate(shotTracerPrefab, origin, Quaternion.identity);
        tracer.positionCount = 2;
        tracer.SetPosition(0, origin);
        tracer.SetPosition(1, endPoint);

        yield return new WaitForSeconds(tracerDuration);

        if (tracer != null)
        {
            Destroy(tracer.gameObject);
        }
    }

    void MeleeAttack()
    {
        if (Physics.SphereCast(cameraTransform.position, 0.4f, cameraTransform.forward, out RaycastHit hit, meleeRange, enemyLayer))
        {
            Health target = hit.collider.GetComponent<Health>();
            if (target != null)
            {
                target.TakeDamage(meleeDamage);
            }
        }
    }

    // --- INPUT CALLBACKS (Send Messages) ---
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>() * 0.01f;
    public void OnJump(InputValue value) { if (value.isPressed) jumpPressed = true; }
    public void OnDash(InputValue value) { if (value.isPressed) dashPressed = true; }
    public void OnSlide(InputValue value) => slideHeld = value.isPressed;

    public void OnFire(InputValue value)
    {
        if (value.isPressed)
        {
            fireRequested = true;
        }
    }

    public void OnMelee(InputValue value)
    {
        if (value.isPressed)
        {
            meleePressed = true;
        }
    }

    public void PlayShootAnim()
    {
        if (!gunAnimator) return;

        if (HasAnimatorParameter(gunAnimator, ShootHash, AnimatorControllerParameterType.Trigger))
        {
            gunAnimator.ResetTrigger(ShootHash);
            gunAnimator.SetTrigger(ShootHash);
            return;
        }

        gunAnimator.Play(RecoilStateHash, 0, 0f);
    }

    private static bool HasAnimatorParameter(Animator animator, int hash, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == hash && parameter.type == type)
            {
                return true;
            }
        }

        return false;
    }
}
