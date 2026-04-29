using System.Collections.Generic;
using UnityEngine;

// Phase 3 / PR 3.F — projectile pool for Spitter plasma shots (TZ §9 PR 3.F).
// Spitter cadence is ~2s with up to 4 alive → ~120 instantiates/min in long
// fights. Pooling avoids GC pressure and re-uses TrailRenderer / collider
// state via EnemyProjectile.ResetForPool.
public class EnemyProjectilePool : MonoBehaviour
{
    static EnemyProjectilePool s_instance;

    readonly Dictionary<GameObject, Stack<GameObject>> stacks =
        new Dictionary<GameObject, Stack<GameObject>>();

    public static EnemyProjectilePool Instance
    {
        get
        {
            if (s_instance != null) return s_instance;
            EnemyProjectilePool found =
#if UNITY_2023_1_OR_NEWER
                Object.FindFirstObjectByType<EnemyProjectilePool>();
#else
                Object.FindObjectOfType<EnemyProjectilePool>();
#endif
            if (found != null) { s_instance = found; return s_instance; }

            GameObject go = new GameObject("EnemyProjectilePool");
            s_instance = go.AddComponent<EnemyProjectilePool>();
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
            while (go == null && stack.Count > 0) go = stack.Pop();
            if (go != null)
            {
                go.transform.SetPositionAndRotation(position, rotation);
                EnemyProjectile p = go.GetComponent<EnemyProjectile>();
                if (p != null) p.ResetForPool();
                go.SetActive(true);
                return go;
            }
        }

        GameObject inst = Instantiate(prefab, position, rotation);
        EnemyProjectile proj = inst.GetComponent<EnemyProjectile>();
        if (proj != null) proj.BindPool(this, prefab);
        return inst;
    }

    public void Return(GameObject go, GameObject originPrefab)
    {
        if (go == null) return;
        if (originPrefab == null) { go.SetActive(false); return; }

        if (!stacks.TryGetValue(originPrefab, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            stacks[originPrefab] = stack;
        }
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        stack.Push(go);
    }
}
