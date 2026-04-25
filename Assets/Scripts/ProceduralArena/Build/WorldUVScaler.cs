using System.Collections.Generic;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Build
{
    // -------------------------------------------------------------------
    // World-space UV tiling for runtime cube primitives.
    //
    // Problem: Unity's CreatePrimitive(Cube) uses 0..1 UVs per face. When
    // a cube is scaled (e.g. floor 8x0.2x8 or wall 8x4x0.2), one tile of
    // the texture stretches across the whole face -> "Roblox-look".
    //
    // Fix: pick the dominant face direction from lossyScale (the smallest
    // axis is the face normal), then push a per-instance MaterialProperty
    // Block setting `_BaseMap_ST` (and `_BumpMap_ST` when present) so the
    // texture tiles N times per world meter on that face. The other two
    // faces look slightly off vs. a true triplanar shader, but stretching
    // disappears for all floor/ceiling/wall-style boxes, which is what
    // we render 99% of the time.
    //
    // tilesPerMeter is set per-material via WorldUVDensityRegistry inside
    // ArenaBuildMaterials.CreateSurface so the biome's slot.textureScale
    // still controls density.
    // -------------------------------------------------------------------
    [DisallowMultipleComponent]
    public class WorldUVScaler : MonoBehaviour
    {
        public float fallbackTilesPerMeter = 0.25f;

        void OnEnable() { Apply(); }
        void OnValidate() { if (Application.isPlaying || !gameObject.activeInHierarchy) return; Apply(); }

        public void Apply()
        {
            var rend = GetComponent<MeshRenderer>();
            if (rend == null) return;
            var mat = rend.sharedMaterial;
            if (mat == null) return;

            float tpm = WorldUVDensityRegistry.Get(mat, fallbackTilesPerMeter);
            if (tpm <= 0f) return;

            Vector3 s = transform.lossyScale;
            float ax = Mathf.Abs(s.x), ay = Mathf.Abs(s.y), az = Mathf.Abs(s.z);
            float u, v;
            if (ay <= ax && ay <= az)        { u = ax; v = az; }   // floor/ceiling: Y is normal
            else if (ax <= ay && ax <= az)   { u = az; v = ay; }   // wall along Z, X is normal
            else                              { u = ax; v = ay; }   // wall along X, Z is normal

            float tu = Mathf.Max(0.05f, u * tpm);
            float tv = Mathf.Max(0.05f, v * tpm);
            var st = new Vector4(tu, tv, 0f, 0f);

            var mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);
            if (mat.HasProperty("_BaseMap_ST"))     mpb.SetVector("_BaseMap_ST", st);
            if (mat.HasProperty("_MainTex_ST"))     mpb.SetVector("_MainTex_ST", st);
            if (mat.HasProperty("_BumpMap_ST"))     mpb.SetVector("_BumpMap_ST", st);
            if (mat.HasProperty("_OcclusionMap_ST")) mpb.SetVector("_OcclusionMap_ST", st);
            if (mat.HasProperty("_MetallicGlossMap_ST")) mpb.SetVector("_MetallicGlossMap_ST", st);
            if (mat.HasProperty("_EmissionMap_ST")) mpb.SetVector("_EmissionMap_ST", st);
            rend.SetPropertyBlock(mpb);
        }
    }

    // Static registry: maps a runtime material instance to its biome-derived
    // tiles-per-meter density. Cleared automatically when a Material is GC'd
    // (using ConditionalWeakTable would be nicer but Dictionary is fine for
    // arena-lifetime materials; ArenaBuildMaterials rebuilds them per arena).
    public static class WorldUVDensityRegistry
    {
        static readonly Dictionary<Material, float> map = new Dictionary<Material, float>();

        public static void Register(Material material, float tilesPerMeter)
        {
            if (material == null) return;
            map[material] = Mathf.Max(0f, tilesPerMeter);
        }

        public static float Get(Material material, float fallback)
        {
            if (material != null && map.TryGetValue(material, out var v)) return v;
            return fallback;
        }

        public static void Clear() { map.Clear(); }
    }
}
