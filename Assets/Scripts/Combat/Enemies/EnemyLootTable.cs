using System;
using UnityEngine;

[Serializable]
public class LootEntry
{
    public GameObject prefab;
    [Range(0f, 1f)] public float chance = 1f;
    public int minCount = 1;
    public int maxCount = 1;
}

/// <summary>
/// Per-enemy loot table. Subscribes to its own Health.onDeath and rolls each
/// entry independently, spawning via PickupSpawner. Seam #2: drop chance /
/// ammo orbs / special-mob overrides all configured per-prefab in the inspector.
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyLootTable : MonoBehaviour
{
    [SerializeField] private LootEntry[] drops;
    [Tooltip("Vertical offset for the spawn point above the enemy pivot.")]
    [SerializeField] private float spawnHeightOffset = 0.5f;

    private Health health;
    private bool rolled;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health != null)
        {
            health.onDeath.AddListener(OnDeath);
        }
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.onDeath.RemoveListener(OnDeath);
        }
    }

    void OnDeath()
    {
        if (rolled) return;
        rolled = true;
        Roll();
    }

    void Roll()
    {
        if (drops == null) return;

        Vector3 origin = transform.position + Vector3.up * spawnHeightOffset;

        for (int i = 0; i < drops.Length; i++)
        {
            LootEntry entry = drops[i];
            if (entry == null || entry.prefab == null) continue;
            if (UnityEngine.Random.value > entry.chance) continue;

            int count = UnityEngine.Random.Range(entry.minCount, entry.maxCount + 1);
            PickupSpawner.Spawn(entry.prefab, origin, count);
        }
    }
}
