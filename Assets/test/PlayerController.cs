using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpHeight = 2f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float gravity = -20f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 1.5f;
    public Transform cameraTransform;

    [Header("Shooting")]
    public float fireRate = 0.2f;
    public float damage = 25f;
    public LayerMask enemyLayer = -1;
    public Transform firePoint;

    [Header("Melee")]
    public float meleeDamage = 40f;
    public float meleeRange = 2f;
    public float meleeCooldown = 0.6f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 dashDirection;
    private float xRotation = 0f;
    private float nextFireTime;
    private float nextMeleeTime;

    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool dashPressed;
    private bool firePressed;
    private bool meleePressed;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null)
        {
            GameObject cam = new GameObject("PlayerCamera");
            cam.transform.SetParent(transform);
            cam.transform.localPosition = new Vector3(0.5f, 1.5f, -2f);
            cam.transform.localRotation = Quaternion.Euler(5f, 0, 0);
            cameraTransform = cam.transform;
            cam.AddComponent<Camera>();
        }

        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(cameraTransform);
            fp.transform.localPosition = new Vector3(0.3f, -0.2f, 1f);
            firePoint = fp.transform;
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
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (dashPressed && !IsDashing() && move.magnitude > 0.1f && isGrounded)
        {
            StartDash(move.normalized);
            dashPressed = false;
        }

        if (IsDashing())
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
        }

        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpPressed = false;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleLook()
    {
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        xRotation -= lookInput.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleCombat()
    {
        if (firePressed && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
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
        Debug.DrawRay(cameraTransform.position, cameraTransform.forward * 100f, Color.red, 0.5f);

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, 100f, enemyLayer))
        {
            Health target = hit.collider.GetComponent<Health>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
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
    public void OnJump(InputValue value) => jumpPressed = value.isPressed;
    public void OnDash(InputValue value) => dashPressed = value.isPressed;
    public void OnFire(InputValue value) => firePressed = value.isPressed;

    public void OnMelee(InputValue value)
    {
        if (value.isPressed)
        {
            meleePressed = true;
        }
    }

    // --- DASH HELPERS ---
    private bool IsDashing() => dashDirection != Vector3.zero;

    private void StartDash(Vector3 dir)
    {
        dashDirection = dir;
        Invoke(nameof(StopDash), dashDuration);
    }

    private void StopDash() => dashDirection = Vector3.zero;
}
