using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Player-driven glory kill: while a staggered enemy is in range and the player
// is holding the Void Blade, pressing F triggers a short choreographed
// execution — slow-mo dip, camera lerp toward target, blade lunge, then the
// target takes lethal damage and the player heals.
//
// Side-channel: does not modify WeaponBase or the player movement script. Reads
// stagger / aim / weapon state, applies effects, and exposes a "ready target"
// query for the HUD prompt.
[RequireComponent(typeof(Health))]
public class GloryKillExecutor : MonoBehaviour
{
    public const string VoidBladeId = "void_blade";

    [Header("References (auto-resolved)")]
    public WeaponManager weaponManager;
    public PlayerStats playerStats;
    public Health playerHealth;
    public PlayerController playerController;
    public Transform cameraTransform;

    [Header("Detection")]
    [Tooltip("Max distance from player to enemy center for an execute to be valid.")]
    public float executeRange = 3.5f;
    [Tooltip("How forgiving the aim cone is, in dot product against camera.forward (1 = exact, 0.6 ≈ 53° cone).")]
    [Range(0f, 1f)] public float aimDot = 0.55f;
    public LayerMask hitMask = ~0;

    [Header("Choreography")]
    public float lockDuration = 0.55f;
    public float slowMoScale = 0.45f;
    public float slowMoDuration = 0.30f;
    public float cameraLerpStrength = 0.85f;
    [ColorUsage(true, true)]
    public Color flashColor = new Color(0.55f, 0.95f, 1f, 1f);

    public event Action<Health> OnGloryKill;

    // HUD reads this each frame to know whether to draw the prompt.
    public bool HasReadyTarget { get; private set; }
    public EnemyStagger ReadyTarget { get; private set; }

    bool executing;
    float prevTimeScale;
    float prevFixedDelta;

    void Awake()
    {
        if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
        if (weaponManager == null) weaponManager = GetComponentInChildren<WeaponManager>(true);
        if (weaponManager == null) weaponManager = FindAnyObjectByType<WeaponManager>();
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerHealth == null) playerHealth = GetComponent<Health>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        Debug.Log($"[GloryKillExecutor] Awake on {name}. weaponManager={(weaponManager!=null)}, playerHealth={(playerHealth!=null)}, camera={(cameraTransform!=null)}");
    }

    void Update()
    {
        if (executing) return;

        // Refresh "ready target" each frame for the HUD prompt.
        ReadyTarget = FindExecuteTarget();
        HasReadyTarget = ReadyTarget != null;

        // F to execute. Polled directly so we don't have to edit the
        // PlayerInputActions asset; matches GameManager's F5 reload pattern.
        if (HasReadyTarget && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            StartCoroutine(ExecuteRoutine(ReadyTarget));
        }
    }

    [Header("Debug")]
    public bool debugLog = true;
    float nextDebugLogTime;

    void DebugThrottled(string msg)
    {
        if (!debugLog) return;
        if (Time.unscaledTime < nextDebugLogTime) return;
        nextDebugLogTime = Time.unscaledTime + 0.5f;
        Debug.Log("[GloryKillExecutor] " + msg);
    }

    EnemyStagger FindExecuteTarget()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform == null && playerController != null)
        {
            var cam = playerController.GetComponentInChildren<Camera>(true);
            if (cam != null) cameraTransform = cam.transform;
        }
        if (cameraTransform == null) { DebugThrottled("cameraTransform null"); return null; }
        if (weaponManager == null) { DebugThrottled("weaponManager null"); return null; }
        if (weaponManager.CurrentWeapon == null) { DebugThrottled("CurrentWeapon null"); return null; }

        var def = weaponManager.CurrentWeapon.Definition;
        if (def == null) { DebugThrottled("Definition null"); return null; }
        if (def.weaponId != VoidBladeId) { DebugThrottled($"weaponId='{def.weaponId}' (need '{VoidBladeId}')"); return null; }

        Vector3 origin = cameraTransform.position;
        Vector3 fwd = cameraTransform.forward;

        // Sphere overlap centered slightly ahead so the player doesn't have to
        // be hugging the enemy to execute.
        Vector3 center = origin + fwd * (executeRange * 0.5f);
        var hits = Physics.OverlapSphere(center, executeRange * 0.5f + 0.5f, hitMask, QueryTriggerInteraction.Ignore);

        EnemyStagger best = null;
        float bestScore = float.NegativeInfinity;
        int staggeredSeen = 0;
        string rejectReason = "no enemies in sphere";
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col.transform.IsChildOf(transform)) continue;

            var stagger = col.GetComponentInParent<EnemyStagger>();
            if (stagger == null) continue;
            if (!stagger.IsStaggered) { rejectReason = $"enemy {stagger.name} not staggered"; continue; }
            staggeredSeen++;
            var h = stagger.GetComponent<Health>();
            if (h == null || h.currentHealth <= 0f) { rejectReason = $"enemy {stagger.name} dead/no Health"; continue; }

            Vector3 toEnemy = stagger.transform.position - origin;
            float dist = toEnemy.magnitude;
            if (dist > executeRange) { rejectReason = $"dist {dist:F2} > {executeRange}"; continue; }
            float dot = Vector3.Dot(fwd, toEnemy.normalized);
            if (dot < aimDot) { rejectReason = $"dot {dot:F2} < {aimDot}"; continue; }

            // Prefer closest + most directly aimed-at.
            float score = dot * 2f - dist * 0.5f;
            if (score > bestScore) { bestScore = score; best = stagger; }
        }
        if (best == null && staggeredSeen > 0) DebugThrottled($"saw {staggeredSeen} staggered, rejected: {rejectReason}");
        else if (best == null && hits.Length > 0) DebugThrottled($"hits={hits.Length}, no staggered enemies in sphere");
        return best;
    }

    IEnumerator ExecuteRoutine(EnemyStagger target)
    {
        if (target == null) yield break;
        executing = true;

        var targetHealth = target.GetComponent<Health>();
        var targetTransform = target.transform;
        Vector3 enemyCenter = targetTransform.position + Vector3.up * 0.9f;

        // Suppress fire so a held LMB doesn't keep swinging during slow-mo.
        if (weaponManager != null) weaponManager.SetFireHeld(false);

        // 1) Hit-stop: freeze for a few frames at scale 0 to sell the impact.
        prevTimeScale = Time.timeScale;
        prevFixedDelta = Time.fixedDeltaTime;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = prevFixedDelta;
        if (CameraShake.Instance != null) CameraShake.Instance.AddTrauma(0.45f);

        // 2) Spawn the slice + flash. Axis = perpendicular to camera-forward,
        //    rolled slightly so each kill has a different angle.
        Vector3 camFwd = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 camRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
        float roll = (Time.unscaledTime * 137.13f) % Mathf.PI;
        Vector3 axis = Quaternion.AngleAxis(Mathf.Sin(roll) * 25f, camFwd) * camRight;
        SpaceSliceFX.Spawn(enemyCenter, axis, length: 7f, lifetime: 0.40f);
        WhiteFlashFX.Spawn(peakAlpha: 0.55f, duration: 0.20f);

        // Hold the freeze briefly.
        float holdT = 0f;
        while (holdT < 0.06f) { holdT += Time.unscaledDeltaTime; yield return null; }

        // 3) Drop into slow-mo for the remainder of the choreography.
        Time.timeScale = slowMoScale;
        Time.fixedDeltaTime = prevFixedDelta * slowMoScale;

        // 4) Apply lethal damage + heal. EnemyDissolve / EnemyDeathBurst handle
        //    the on-death VFX so we don't need to drive them here.
        if (targetHealth != null && targetHealth.currentHealth > 0f)
        {
            targetHealth.TakeDamage(targetHealth.maxHealth + 9999f);
        }
        if (playerHealth != null && playerStats != null)
        {
            playerHealth.Heal(playerStats.GetGloryHealAmount());
        }
        if (targetHealth != null)
        {
            UpgradeSystem.Instance?.NotifyGloryKill(targetHealth.gameObject);
        }
        OnGloryKill?.Invoke(targetHealth);

        // 5) Linger in slow-mo so the player reads the kill, then restore.
        float linger = 0f;
        while (linger < slowMoDuration) { linger += Time.unscaledDeltaTime; yield return null; }

        Time.timeScale = prevTimeScale;
        Time.fixedDeltaTime = prevFixedDelta;
        executing = false;
    }

    void OnDisable()
    {
        // Safety: if we get disabled mid-execution, restore time scale and
        // controller so the player isn't stuck in slow-mo without input.
        if (executing)
        {
            if (prevTimeScale > 0f) Time.timeScale = prevTimeScale;
            if (prevFixedDelta > 0f) Time.fixedDeltaTime = prevFixedDelta;
            executing = false;
        }
    }
}
