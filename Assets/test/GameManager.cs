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
            SpawnEnemy();
            enemiesSpawned++;
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        uiManager?.ShowWaveState($"Wave {currentWave}: eliminate remaining enemies");
        EvaluateWaveEnd();
    }

    void SpawnEnemy()
    {
        if (spawnPoints.Length == 0 || enemyPrefab == null) return;

        Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
        enemiesAlive++;

        SimpleEnemyAI enemyAI = enemy.GetComponent<SimpleEnemyAI>();
        if (enemyAI != null && playerTransform != null)
        {
            enemyAI.SetTarget(playerTransform);
        }

        Health enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth != null)
        {
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

    public void BeginEncounter(int count, Transform[] spawns, Action onEnemyKilledCallback)
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
        encounterActive = true;
        enemiesSpawned = 0;
        enemiesAlive = 0;
        enemiesToSpawn = Mathf.Max(0, count);

        uiManager?.ShowWaveState($"Encounter: {enemiesToSpawn} enemies");
        StartCoroutine(SpawnEncounter());
    }

    public void EndEncounter()
    {
        encounterActive = false;
        encounterOnEnemyKilled = null;
        encounterSpawnPoints = null;
        uiManager?.ShowWaveState("Encounter cleared");
    }

    IEnumerator SpawnEncounter()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (!encounterActive) yield break;
            SpawnEncounterEnemy();
            enemiesSpawned++;
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    void SpawnEncounterEnemy()
    {
        if (enemyPrefab == null) return;
        Transform[] pool = encounterSpawnPoints != null && encounterSpawnPoints.Length > 0
            ? encounterSpawnPoints : spawnPoints;
        if (pool == null || pool.Length == 0) return;

        Transform point = pool[UnityEngine.Random.Range(0, pool.Length)];
        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
        enemiesAlive++;

        SimpleEnemyAI ai = enemy.GetComponent<SimpleEnemyAI>();
        if (ai != null && playerTransform != null) ai.SetTarget(playerTransform);

        Health hp = enemy.GetComponent<Health>();
        if (hp != null) hp.onDeath.AddListener(OnEncounterEnemyDied);
    }

    void OnEncounterEnemyDied()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        OnEnemyKilled?.Invoke();
        encounterOnEnemyKilled?.Invoke();
    }
}
