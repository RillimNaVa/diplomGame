using System.Collections.Generic;
using UnityEngine;

// Phase 3 / PR 3.F — per-prefab enemy pool (TZ §9 PR 3.F + §10 acceptance).
// Long runs accumulate disabled enemy GameObjects (Health.Disable() does not
// destroy them, just SetActive(false)), so we recycle by stack-of-instances
// keyed on the source prefab. Each rent restores Health, brain state,
// EnemyStagger, NavMeshAgent, loot lifecycle via PooledEnemy.PrepareForReuse.
//
// Auto-creates a scene-singleton GameObject on first access so callers do not
// need explicit wiring (mirrors ActiveAttackSlotManager pattern).
public class EnemyPool : MonoBehaviour
{
    static EnemyPool s_instance;

    readonly Dictionary<GameObject, Stack<GameObject>> stacks =
        new Dictionary<GameObject, Stack<GameObject>>();

    public static EnemyPool Instance
    {
        get
        {
            if (s_instance != null) return s_instance;
            EnemyPool found =
#if UNITY_2023_1_OR_NEWER
                Object.FindFirstObjectByType<EnemyPool>();
#else
                Object.FindObjectOfType<EnemyPool>();
#endif
            if (found != null) { s_instance = found; return s_instance; }

            GameObject go = new GameObject("EnemyPool");
            s_instance = go.AddComponent<EnemyPool>();
            return s_instance;
        }
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this) { Destroy(this); return; }
        s_instance = this;
    }

    void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    public GameObject Rent(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (stacks.TryGetValue(prefab, out Stack<GameObject> stack) && stack.Count > 0)
        {
            GameObject go = stack.Pop();
            // Skip null entries (scene reload, manual destroy, etc.).
            while (go == null && stack.Count > 0) go = stack.Pop();
            if (go != null)
            {
                go.transform.SetPositionAndRotation(position, rotation);
                PooledEnemy pe = go.GetComponent<PooledEnemy>();
                if (pe != null) pe.PrepareForReuse();
                go.SetActive(true);
                if (pe != null) pe.FinishReuseAfterEnable();
                return go;
            }
        }

        GameObject inst = Instantiate(prefab, position, rotation);
        PooledEnemy pooled = inst.GetComponent<PooledEnemy>();
        if (pooled == null) pooled = inst.AddComponent<PooledEnemy>();
        pooled.BindPool(this, prefab);
        return inst;
    }

    public void Return(GameObject go, GameObject originPrefab)
    {
        if (go == null || originPrefab == null) { if (go != null) go.SetActive(false); return; }

        if (!stacks.TryGetValue(originPrefab, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            stacks[originPrefab] = stack;
        }
        go.SetActive(false);
        // Re-parent under the pool to keep the Hierarchy tidy and avoid
        // surprises when an arena that owned the enemy is destroyed.
        go.transform.SetParent(transform, false);
        stack.Push(go);
    }
}
