using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
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

    [Header("Weapon System")]
    [Tooltip("Weapon manager that receives all combat input. Auto-resolved from this GameObject if not set.")]
    public WeaponManager weaponManager;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation;

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

    // External multiplier applied to walk/air speed only (see HandleMovement).
    // Dash / slide are authored speeds; leaving them unscaled avoids exponential
    // blowouts when multiple systems stack on top of the streak boost.
    private float speedMultiplier = 1f;
    public float SpeedMultiplier => speedMultiplier;
    public void SetSpeedMultiplier(float value) => speedMultiplier = Mathf.Max(0.01f, value);

    // Weapon sway
    private Quaternion weaponBaseLocalRotation;

    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool dashPressed;
    private bool slideHeld;


    void Start()
    {
        controller = GetComponent<CharacterController>();

        originalHeight = controller.height;
        originalCenter = controller.center;

        dashCharges = maxDashCharges;
        currentSpeed = moveSpeed;

        if (weaponManager == null)
        {
            weaponManager = GetComponent<WeaponManager>();
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
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
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
            controller.Move(move * currentSpeed * speedMultiplier * airMul * Time.deltaTime);
        }

        // 9. Momentum decay toward base speed (only when not actively boosted)
        // Dash/slide target speeds stay unscaled on purpose — the multiplier
        // only affects authored walk/air movement (see SetSpeedMultiplier).
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

    // --- INPUT CALLBACKS (Send Messages) ---
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>() * 0.01f;
    public void OnJump(InputValue value) { if (value.isPressed) jumpPressed = true; }
    public void OnDash(InputValue value) { if (value.isPressed) dashPressed = true; }
    public void OnSlide(InputValue value) => slideHeld = value.isPressed;

    // Fire action is now Value-typed: this fires on both press and release with
    // value.isPressed reflecting the new trigger state. WeaponManager handles
    // semi-auto vs full-auto internally.
    public void OnFire(InputValue value)
    {
        if (weaponManager != null)
        {
            weaponManager.SetFireHeld(value.isPressed);
        }
    }

    public void OnReload(InputValue value)
    {
        if (value.isPressed && weaponManager != null)
        {
            weaponManager.Reload();
        }
    }

    public void OnSlotSelect1(InputValue value) { if (value.isPressed) weaponManager?.EquipSlot(0); }
    public void OnSlotSelect2(InputValue value) { if (value.isPressed) weaponManager?.EquipSlot(1); }
    public void OnSlotSelect3(InputValue value) { if (value.isPressed) weaponManager?.EquipSlot(2); }
    public void OnSlotSelect4(InputValue value) { if (value.isPressed) weaponManager?.EquipSlot(3); }
    public void OnSlotSelect5(InputValue value) { if (value.isPressed) weaponManager?.EquipSlot(4); }

    public void OnSwitchScroll(InputValue value)
    {
        if (weaponManager == null) return;
        Vector2 scroll = value.Get<Vector2>();
        if (scroll.y > 0f) weaponManager.CycleSlot(+1);
        else if (scroll.y < 0f) weaponManager.CycleSlot(-1);
    }
}
