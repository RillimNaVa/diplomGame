using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int baseEnemiesPerWave = 3;
    public float timeBetweenWaves = 30f;
    public float timeBetweenSpawns = 2f;

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
        StartNewWave();
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged.RemoveListener(UpdatePlayerHealth);
        }
    }

    void Update()
    {
        if (!waveInProgress)
        {
            waveTimer -= Time.deltaTime;
            uiManager?.UpdateTimer(waveTimer);

            if (waveTimer <= 0)
            {
                StartNewWave();
            }
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
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
            if (playerHealth.onHealthChanged == null)
            {
                playerHealth.onHealthChanged = new HealthChangedEvent();
            }

            playerHealth.onHealthChanged.AddListener(UpdatePlayerHealth);
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

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
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
}
