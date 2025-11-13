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
    public float mouseSensitivity = 1.5f;   // ← ИСПРАВЛЕНО
    public Transform cameraTransform;

    [Header("Shooting")]
    public float fireRate = 0.2f;
    public float damage = 25f;
    public LayerMask enemyLayer = -1; // Все слои
    public Transform firePoint;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 dashDirection;
    private float xRotation = 0f;
    private float nextFireTime;

    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool dashPressed;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Создаём камеру
        if (cameraTransform == null)
        {
            GameObject cam = new GameObject("PlayerCamera");
            cam.transform.SetParent(transform);
            cam.transform.localPosition = new Vector3(0.5f, 1.5f, -2f);
            cam.transform.localRotation = Quaternion.Euler(5f, 0, 0);
            cameraTransform = cam.transform;
            cam.AddComponent<Camera>();
        }

        // Создаём точку выстрела
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
        HandleShooting();
    }

    void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (dashPressed && !isDashing() && move.magnitude > 0.1f && isGrounded)
        {
            StartDash(move.normalized);
            dashPressed = false;
        }

        if (isDashing())
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

    void HandleShooting()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
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

    // --- INPUT CALLBACKS (Send Messages) ---
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>() * 0.01f;
    public void OnJump(InputValue value) => jumpPressed = value.isPressed;
    public void OnDash(InputValue value) => dashPressed = value.isPressed;

    // --- DASH HELPERS ---
    private bool isDashing() => dashDirection != Vector3.zero;
    private void StartDash(Vector3 dir)
    {
        dashDirection = dir;
        Invoke(nameof(StopDash), dashDuration);
    }
    private void StopDash() => dashDirection = Vector3.zero;
}