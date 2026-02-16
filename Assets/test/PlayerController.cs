using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]


public class PlayerController : MonoBehaviour
{
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int RecoilStateHash = Animator.StringToHash("recoil");

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
    private Vector3 dashDirection;
    private float xRotation;
    private float nextFireTime;
    private float nextMeleeTime;

    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool dashPressed;
    private bool fireRequested;
    private bool meleePressed;


    void Start()
    {
        controller = GetComponent<CharacterController>();

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
        Vector3 endPoint = origin + cameraTransform.forward * maxDistance;

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        Ray centerRay = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(centerRay, out RaycastHit centerHit, maxDistance))
        {
            endPoint = centerHit.point;
        }

        Vector3 direction = (endPoint - origin).normalized;

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
    public void OnJump(InputValue value) => jumpPressed = value.isPressed;
    public void OnDash(InputValue value) => dashPressed = value.isPressed;

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

    // --- DASH HELPERS ---
    private bool IsDashing() => dashDirection != Vector3.zero;

    private void StartDash(Vector3 dir)
    {
        dashDirection = dir;
        Invoke(nameof(StopDash), dashDuration);
    }

    private void StopDash() => dashDirection = Vector3.zero;

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
