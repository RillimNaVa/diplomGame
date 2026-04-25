using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Build
{
    public static class BuildUtils
    {
        public static Vector3 CellCornerToWorld(Vector2Int cell, float m) =>
            new Vector3(cell.x * m, 0f, cell.y * m);

        public static GameObject SpawnBox(
            Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null && mat != null)
            {
                rend.sharedMaterial = mat;
                // Per-instance world-space UV tiling — kills "Roblox stretch"
                // on big floors/walls without changing the underlying mesh.
                go.AddComponent<WorldUVScaler>();
            }
            if (!collider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            return go;
        }
    }
}
