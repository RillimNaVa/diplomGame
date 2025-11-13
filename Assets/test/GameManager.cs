using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // ← Для Keyboard

public class GameManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int baseEnemiesPerWave = 3;
    public float timeBetweenWaves = 30f;
    public float timeBetweenSpawns = 2f;

    [Header("UI")]
    public UIManager uiManager; // ← DRAG HUDCanvas → UIManager (в Inspector)

    private int currentWave = 0;
    private float waveTimer;
    private bool waveActive = false;
    private int enemiesToSpawn;

    public static GameManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartNewWave();
    }

    void Update()
    {
        waveTimer -= Time.deltaTime;
        uiManager.UpdateTimer(waveTimer);

        if (waveTimer <= 0 && !waveActive)
        {
            StartNewWave();
        }

        // Рестарт на R
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    void StartNewWave()
    {
        currentWave++;
        enemiesToSpawn = baseEnemiesPerWave + (currentWave - 1) * 2; // Растёт сложность
        waveActive = true;
        waveTimer = timeBetweenWaves;

        uiManager.UpdateWave(currentWave);
        StartCoroutine(SpawnWave());
    }

    System.Collections.IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        waveActive = false;
        uiManager.waveText.text = $"Wave {currentWave} COMPLETE";
    }

    void SpawnEnemy()
    {
        if (spawnPoints.Length == 0 || enemyPrefab == null) return;

        Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);

        // Связываем здоровье врага с UI (опционально)
        Health enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth != null)
        {
            enemyHealth.onDeath.AddListener(() => CheckWaveEnd());
        }
    }

    void CheckWaveEnd()
    {
        // Можно добавить логику завершения волны
    }

    // Вызывается из Health.cs при уроне игроку
    public void UpdatePlayerHealth(float current, float max)
    {
        uiManager.UpdateHealthBar(current, max);
    }
}