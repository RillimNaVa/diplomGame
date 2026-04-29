using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Fires once per enemy death (wave counting, kill-streak tracker, future HUD).
    public event Action OnEnemyKilled;

    [Header("Wave Settings")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int baseEnemiesPerWave = 3;
    public float timeBetweenWaves = 30f;
    public float timeBetweenSpawns = 2f;

    [Header("Encounter Mode (PR 2.C)")]
    [Tooltip("When true, the legacy wave loop is disabled and encounters are driven by EncounterController via BeginEncounter/EndEncounter.")]
    public bool useEncounterMode = false;

    [Header("Spawn Telegraph (PR 5.A)")]
    [Tooltip("Seconds the spawn-point telegraph (floor circle + vertical beam) plays before the enemy is actually rented. 0 disables.")]
    public float spawnTelegraphDuration = 0.7f;

    [Header("References")]
    public UIManager uiManager;
    public Transform playerTransform;
    public Health playerHealth;

    private int currentWave;
    private float waveTimer;
    private bool waveInProgress;
    private int enemiesToSpawn;
    private int enemiesSpawned;
    private int enemiesAlive;

    // ---- Encounter mode state (PR 2.C) ----
    Transform[] encounterSpawnPoints;
    Action encounterOnEnemyKilled;
    bool encounterActive;
    float encounterEnemyHealthMultiplier = 1f;
    // PR 3.D — when set, SpawnEncounter pulls per-enemy prefab/data from this
    // roster instead of repeating the global enemyPrefab. Cleared by EndEncounter.
    List<EnemySpawnEntry> encounterRoster;

    public static GameManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Duplicate GameManager found. Destroying duplicate instance.");
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void Start()
    {
        ResolveReferences();
        if (!useEncounterMode) StartNewWave();
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged.RemoveListener(UpdatePlayerHealth);
            playerHealth.onDeath.RemoveListener(OnPlayerDied);
        }
    }

    void Update()
    {
        if (!useEncounterMode && !waveInProgress)
        {
            waveTimer -= Time.deltaTime;
            uiManager?.UpdateTimer(waveTimer);

            if (waveTimer <= 0)
            {
                StartNewWave();
            }
        }

        // Scene restart moved from R (which now reloads the current weapon)
        // to F5 to avoid conflicting with the weapon reload input.
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            ReloadCurrentScene();
        }
    }

    public void ReloadCurrentScene()
    {
        // Reload the currently active scene by its build index. Previously LoadScene(0)
        // was hardcoded, which pointed at SampleScene in Build Settings (not test.unity),
        // causing the yellow-screen / missing-objects bug on R press.
        // Requires the active scene to be in Build Settings.
        Scene active = SceneManager.GetActiveScene();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(active.buildIndex);
    }

    void OnPlayerDied()
    {
        uiManager?.ShowWaveState("YOU DIED — reloading...");
        Invoke(nameof(ReloadCurrentScene), 2f);
    }

    void ResolveReferences()
    {
        if (playerTransform == null)
        {
            GameObject taggedPlayer = GameObject.FindWithTag("Player");
            playerTransform = taggedPlayer != null ? taggedPlayer.transform : null;
        }

        if (playerHealth == null && playerTransform != null)
        {
            playerHealth = playerTransform.GetComponent<Health>();
        }

        if (playerHealth != null)
        {
            playerHealth.onHealthChanged.AddListener(UpdatePlayerHealth);
            playerHealth.onDeath.AddListener(OnPlayerDied);
            UpdatePlayerHealth(playerHealth.currentHealth, playerHealth.maxHealth);

            // PR 4.A — auto-attach screen-shake + damage-vignette feedback so
            // the player gets hit feedback without manual Editor wiring.
            if (playerHealth.GetComponent<PlayerHitFeedback>() == null)
            {
                playerHealth.gameObject.AddComponent<PlayerHitFeedback>();
            }
        }
    }

    void StartNewWave()
    {
        currentWave++;
        enemiesToSpawn = baseEnemiesPerWave + (currentWave - 1) * 2;
        enemiesSpawned = 0;
        enemiesAlive = 0;
        waveInProgress = true;
        waveTimer = timeBetweenWaves;

        uiManager?.UpdateWave(currentWave);
        uiManager?.ShowWaveState($"Wave {currentWave} STARTED");
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            yield return StartCoroutine(SpawnEnemyWithTelegraph());
            enemiesSpawned++;
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        uiManager?.ShowWaveState($"Wave {currentWave}: eliminate remaining enemies");
        EvaluateWaveEnd();
    }

    // PR 5.A — pre-spawn telegraph wrapper for the legacy wave loop.
    IEnumerator SpawnEnemyWithTelegraph()
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || enemyPrefab == null) yield break;
        Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        if (point == null) yield break;
        if (spawnTelegraphDuration > 0f)
        {
            SpawnTelegraph.SpawnAt(point.position, spawnTelegraphDuration);
            yield return new WaitForSeconds(spawnTelegraphDuration);
        }
        SpawnEnemyAt(point);
    }

    void SpawnEnemy()
    {
        if (spawnPoints.Length == 0 || enemyPrefab == null) return;
        Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        SpawnEnemyAt(point);
    }

    void SpawnEnemyAt(Transform point)
    {
        if (point == null || enemyPrefab == null) return;
        // PR 3.F: rent through EnemyPool. Pool restores Health / EnemyStagger /
        // EnemyLootTable / NavMeshAgent state via PooledEnemy.PrepareForReuse.
        GameObject enemy = EnemyPool.Instance.Rent(enemyPrefab, point.position, point.rotation);
        if (enemy == null) return;
        enemiesAlive++;

        // PR 3.A: talk to IEnemyTargetReceiver so legacy SimpleEnemyAI and
        // new EnemyBrainBase subclasses are both supported with no fallback.
        IEnemyTargetReceiver receiver = enemy.GetComponent<IEnemyTargetReceiver>();
        if (receiver != null && playerTransform != null)
        {
            receiver.SetTarget(playerTransform);
        }

        Health enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth != null)
        {
            // Remove+Add so the same recycled instance does not accumulate
            // duplicate OnEnemyDied subscriptions across pool rents (PR 3.F).
            enemyHealth.onDeath.RemoveListener(OnEnemyDied);
            enemyHealth.onDeath.AddListener(OnEnemyDied);
        }
    }

    void OnEnemyDied()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        OnEnemyKilled?.Invoke();
        EvaluateWaveEnd();
    }

    void EvaluateWaveEnd()
    {
        bool allEnemiesSpawned = enemiesSpawned >= enemiesToSpawn;
        bool noAliveEnemies = enemiesAlive == 0;

        if (allEnemiesSpawned && noAliveEnemies)
        {
            waveInProgress = false;
            waveTimer = timeBetweenWaves;
            uiManager?.ShowWaveState($"Wave {currentWave} COMPLETE");
        }
    }

    public void UpdatePlayerHealth(float current, float max)
    {
        uiManager?.UpdateHealthBar(current, max);
    }

    // =====================================================================
    // Encounter API (PR 2.C)
    // Drives per-arena combat when useEncounterMode is true. EncounterController
    // calls BeginEncounter with the arena's spawn points and a death callback;
    // the controller decides when the encounter is cleared.
    // =====================================================================

    public void SetSpawnPoints(IReadOnlyList<Transform> points)
    {
        if (points == null || points.Count == 0)
        {
            spawnPoints = new Transform[0];
            return;
        }
        spawnPoints = new Transform[points.Count];
        for (int i = 0; i < points.Count; i++) spawnPoints[i] = points[i];
    }

    public void BeginEncounter(int count, Transform[] spawns, Action onEnemyKilledCallback, float healthMultiplier = 1f)
    {
        if (!useEncounterMode)
        {
            Debug.LogWarning("[GameManager] BeginEncounter called but useEncounterMode is false.");
            return;
        }
        if (encounterActive)
        {
            Debug.LogWarning("[GameManager] BeginEncounter called while another encounter is active. Ending previous.");
            EndEncounter();
        }

        if (spawns != null && spawns.Length > 0) encounterSpawnPoints = spawns;
        else encounterSpawnPoints = spawnPoints;

        encounterOnEnemyKilled = onEnemyKilledCallback;
        encounterEnemyHealthMultiplier = Mathf.Max(0.1f, healthMultiplier);
        encounterActive = true;
        enemiesSpawned = 0;
        enemiesAlive = 0;
        enemiesToSpawn = Mathf.Max(0, count);

        uiManager?.ShowWaveState($"Encounter: {enemiesToSpawn} enemies");
        StartCoroutine(SpawnEncounter());
    }

    /// <summary>
    /// PR 3.D — composer-driven entry. Roster comes already resolved (one entry
    /// per enemy to spawn). enemyCount is derived from roster.Count. Falls back
    /// to the legacy count-based overload if roster is null/empty.
    /// </summary>
    public void BeginEncounter(IList<EnemySpawnEntry> roster, Transform[] spawns, Action onEnemyKilledCallback, float healthMultiplier = 1f)
    {
        if (roster == null || roster.Count == 0)
        {
            BeginEncounter(0, spawns, onEnemyKilledCallback, healthMultiplier);
            return;
        }

        if (encounterActive)
        {
            Debug.LogWarning("[GameManager] BeginEncounter(roster) called while another encounter is active. Ending previous.");
            EndEncounter();
        }

        encounterRoster = new List<EnemySpawnEntry>(roster);
        BeginEncounter(roster.Count, spawns, onEnemyKilledCallback, healthMultiplier);
    }

    public void EndEncounter()
    {
        encounterActive = false;
        encounterOnEnemyKilled = null;
        encounterSpawnPoints = null;
        encounterEnemyHealthMultiplier = 1f;
        encounterRoster = null;
        uiManager?.ShowWaveState("Encounter cleared");
    }

    IEnumerator SpawnEncounter()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (!encounterActive) yield break;
            yield return StartCoroutine(SpawnEncounterEnemyWithTelegraph());
            enemiesSpawned++;
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    // PR 5.A — telegraph wrapper for encounter mode. Picks the spawn point up
    // front so the floor circle / vertical beam appears at the exact spot the
    // enemy will materialize after the delay.
    IEnumerator SpawnEncounterEnemyWithTelegraph()
    {
        Transform[] pool = encounterSpawnPoints != null && encounterSpawnPoints.Length > 0
            ? encounterSpawnPoints : spawnPoints;
        if (pool == null || pool.Length == 0) yield break;
        Transform point = pool[UnityEngine.Random.Range(0, pool.Length)];
        if (point == null) yield break;

        if (spawnTelegraphDuration > 0f)
        {
            SpawnTelegraph.SpawnAt(point.position, spawnTelegraphDuration);
            yield return new WaitForSeconds(spawnTelegraphDuration);
            if (!encounterActive) yield break;  // re-check after wait
        }
        SpawnEncounterEnemyAt(point);
    }

    void SpawnEncounterEnemyAt(Transform point)
    {
        if (point == null) return;
        // PR 3.D — pick prefab from roster if available. enemiesSpawned is
        // pre-increment in SpawnEncounter, so it points at the next slot.
        GameObject prefabToSpawn = enemyPrefab;
        if (encounterRoster != null && enemiesSpawned < encounterRoster.Count)
        {
            var entry = encounterRoster[enemiesSpawned];
            if (entry != null && entry.prefab != null) prefabToSpawn = entry.prefab;
        }
        if (prefabToSpawn == null) return;
        // PR 3.F: rent through EnemyPool. PooledEnemy.PrepareForReuse already
        // restored hp.maxHealth to the baseline (data.maxHealth), so the
        // multiplier below applies cleanly without compounding across rents.
        GameObject enemy = EnemyPool.Instance.Rent(prefabToSpawn, point.position, point.rotation);
        if (enemy == null) return;
        enemiesAlive++;

        IEnemyTargetReceiver receiver = enemy.GetComponent<IEnemyTargetReceiver>();
        if (receiver != null && playerTransform != null) receiver.SetTarget(playerTransform);

        Health hp = enemy.GetComponent<Health>();
        if (hp != null)
        {
            if (Mathf.Abs(encounterEnemyHealthMultiplier - 1f) > 0.001f)
            {
                hp.maxHealth *= encounterEnemyHealthMultiplier;
                hp.currentHealth = hp.maxHealth;
                hp.onHealthChanged?.Invoke(hp.currentHealth, hp.maxHealth);
            }
            // Remove+Add so a recycled instance does not accumulate duplicate
            // OnEncounterEnemyDied subscriptions across rents (PR 3.F).
            hp.onDeath.RemoveListener(OnEncounterEnemyDied);
            hp.onDeath.AddListener(OnEncounterEnemyDied);
        }
    }

    void OnEncounterEnemyDied()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        OnEnemyKilled?.Invoke();
        encounterOnEnemyKilled?.Invoke();
    }
}
