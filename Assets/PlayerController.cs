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

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isDashing;
    private Vector3 dashDirection;

    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool dashPressed;

    // Components
    private PlayerInput playerInput;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // Блокировка и сокрытие курсора
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Создаём камеру, если нет
        if (cameraTransform == null)
        {
            GameObject cam = new GameObject("PlayerCamera");
            cam.transform.SetParent(transform);
            cam.transform.localPosition = new Vector3(0, 1.5f, 0);
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

        // Dash
        if (dashPressed && !isDashing && move.magnitude > 0.1f)
        {
            StartDash(move.normalized);
        }

        if (isDashing)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
        }

        // Прыжок
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Гравитация
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleLook()
    {
        // Поворот персонажа
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        // Поворот камеры по вертикали
        float pitch = cameraTransform.localEulerAngles.x - lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -80f, 80f); // Ограничение вверх/вниз
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0, 0);
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

    // Input System callbacks
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        jumpPressed = value.isPressed;
    }

    public void OnDash(InputValue value)
    {
        dashPressed = value.isPressed;
    }
}