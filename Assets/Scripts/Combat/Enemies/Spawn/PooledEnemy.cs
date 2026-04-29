using UnityEngine;
using UnityEngine.AI;

// Phase 3 / PR 3.F — per-instance pool adapter.
// Captures the baseline Health.maxHealth post-EnemyBrainBase.Awake (so the
// `data.maxHealth` override is captured, not the prefab Inspector value), then
// restores Health / EnemyStagger / EnemyLootTable / NavMeshAgent state on each
// rent. Listens to its own Health.onDeath and schedules a return-to-pool after
// a short grace window (matches Health's existing 1s auto-disable so loot
// drops + glory-kill detector still get their tick).
//
// `[DefaultExecutionOrder(200)]` ensures Awake runs *after* EnemyBrainBase so
// `health.maxHealth = data.maxHealth` has already been applied.
[DefaultExecutionOrder(200)]
public class PooledEnemy : MonoBehaviour
{
    [Tooltip("Seconds between Health.onDeath and pool return. Health.Disable() runs at 1s; default 1.5s gives loot/glory-kill flows time to complete after disable.")]
    public float returnDelay = 1.5f;

    EnemyPool pool;
    GameObject originPrefab;
    float baselineMaxHealth;
    bool baselineCaptured;

    Health health;
    EnemyStagger stagger;
    EnemyLootTable lootTable;
    NavMeshAgent agent;
    EnemyDissolve dissolve;
    StaggerOutline outline;

    void Awake()
    {
        health = GetComponent<Health>();
        stagger = GetComponent<EnemyStagger>();
        lootTable = GetComponent<EnemyLootTable>();
        agent = GetComponent<NavMeshAgent>();
        dissolve = GetComponent<EnemyDissolve>();
        outline = GetComponent<StaggerOutline>();
        if (health != null)
        {
            baselineMaxHealth = health.maxHealth;
            baselineCaptured = true;
        }
    }

    public void BindPool(EnemyPool ownerPool, GameObject prefab)
    {
        pool = ownerPool;
        originPrefab = prefab;
    }

    void OnEnable()
    {
        if (health != null) health.onDeath.AddListener(OnDeath);
    }

    void OnDisable()
    {
        if (health != null) health.onDeath.RemoveListener(OnDeath);
        CancelInvoke();
    }

    void OnDeath()
    {
        if (pool == null) return;  // not pool-managed; let Health's own Disable() run
        // Don't let Health auto-disable — pool owns disable timing now.
        if (health != null) health.CancelAutoDisable();
        CancelInvoke(nameof(ReturnNow));
        Invoke(nameof(ReturnNow), returnDelay);
    }

    void ReturnNow()
    {
        if (pool != null) pool.Return(gameObject, originPrefab);
        else gameObject.SetActive(false);
    }

    /// <summary>
    /// Called by EnemyPool.Rent before SetActive(true). Restores per-component
    /// state to what a freshly Instantiated copy would look like.
    /// </summary>
    public void PrepareForReuse()
    {
        if (health != null)
        {
            if (baselineCaptured) health.maxHealth = baselineMaxHealth;
            health.ResetForPool();
        }
        if (stagger != null) stagger.ResetForPool();
        if (lootTable != null) lootTable.ResetForPool();
        // PR 5.A — restore original sharedMaterials so the next rent shows the
        // textured enemy, not a frozen 100%-dissolved or stagger-outlined GO.
        if (dissolve != null) dissolve.ResetForPool();
        if (outline != null) outline.ResetForPool();
        if (agent != null && agent.enabled)
        {
            // Warp to current position so the agent picks up the new spawn point;
            // ResetPath clears any leftover destination from the previous life.
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
            agent.Warp(transform.position);
        }
    }
}
