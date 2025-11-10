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
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    [Header("Debug")]
    public bool isGrounded;
    public bool isDashing;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 dashDirection;
    private float xRotation = 0f; // Фикс камеры!

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
            cam.transform.localPosition = new Vector3(0.5f, 1.5f, -2f); // Over-shoulder
            cam.transform.localRotation = Quaternion.Euler(5f, 0, 0);
            cameraTransform = cam.transform;
            cam.AddComponent<Camera>();
        }
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Движение
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Dash (только ОДИН раз)
        if (dashPressed && !isDashing)
        {
            StartDash(move.normalized);
            dashPressed = false; // ← КРИТИЧНЫЙ ФИКС
        }

        if (isDashing)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
        }

        // Прыжок (только ОДИН раз)
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpPressed = false; // ← КРИТИЧНЫЙ ФИКС
        }

        // Гравитация
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleLook()
    {
        // Поворот персонажа (Y-ось)
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        // Поворот камеры (X-ось) — ФИКС GIMBAL LOCK
        xRotation -= lookInput.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void StartDash(Vector3 direction)
    {
        isDashing = true;
        dashDirection = direction;
        Invoke(nameof(StopDash), dashDuration);
    }

    void StopDash()
    {
        isDashing = false;
    }

    // Input Callbacks (НЕ трогать)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>() * 0.01f; // Сглаживание мыши
    }

    public void OnJump(InputValue value)
    {
        jumpPressed = value.isPressed; // Нажатие
    }

    public void OnDash(InputValue value)
    {
        dashPressed = value.isPressed; // Нажатие
    }
}