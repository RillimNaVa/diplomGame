using UnityEngine;

/// <summary>
/// Single entry point for spawning pickup prefabs at a world position.
/// Centralises scatter, future pooling, and VFX hooks so loot-table code
/// doesn't touch Instantiate directly.
/// </summary>
public static class PickupSpawner
{
    public static void Spawn(GameObject prefab, Vector3 position, int count = 1, float scatterRadius = 0.6f)
    {
        if (prefab == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset2D = count > 1 ? Random.insideUnitCircle * scatterRadius : Vector2.zero;
            Vector3 pos = position + new Vector3(offset2D.x, 0f, offset2D.y);
            Object.Instantiate(prefab, pos, Quaternion.identity);
        }
    }
}
