using UnityEngine;

[ExecuteAlways]
public class TerrainGenerator : MonoBehaviour {
    [Header("Terrain Settings")]
    public Terrain terrain;
    public float targetHeight = 60f;

    [Header("Noise Settings")]
    [Range(0.001f, 0.3f)] public float scale = 0.08f;
    [Range(1, 8)] public int octaves = 5;
    [Range(0.3f, 0.7f)] public float persistence = 0.55f;
    [Range(1.5f, 3f)] public float lacunarity = 2f;
    [Range(-0.5f, 0.5f)] public float heightOffset = -0.15f;

    [Header("Biome Preview")]
    public bool autoUpdate = false;
    public int seed = 666;

    [Header("Debug (Read-Only in Editor)")]
    public float maxNoise = 0f;
    public float minNoise = 1f;
    public float contrast = 0f;

    private void Start() {
        if (terrain == null) terrain = GetComponent<Terrain>();
        if (autoUpdate) GenerateTerrain();
    }

    [ContextMenu("Generate + Stretch Height")]
    public void GenerateTerrain() {
        if (terrain == null) {
            Debug.LogError("❌ Terrain не назначен!");
            return;
        }

        TerrainData data = terrain.terrainData;
        Vector3 oldSize = data.size;
        data.size = new Vector3(oldSize.x, 1f, oldSize.z);

        UnityEngine.Random.InitState(seed);
        int res = data.heightmapResolution;
        float[,] heights = new float[res, res];

        float worldSize = oldSize.x;

        float globalMin = 999f;
        float globalMax = -999f;

        for (int x = 0; x < res; x++) {
            for (int z = 0; z < res; z++) {
                float worldX = (float)x / (res - 1) * worldSize;
                float worldZ = (float)z / (res - 1) * worldSize;

                float rawNoise = FBM(worldX * scale, worldZ * scale);
                float normalized = rawNoise + heightOffset;
                normalized = Mathf.Clamp01(normalized);

                heights[x, z] = normalized;

                globalMin = Mathf.Min(globalMin, normalized);
                globalMax = Mathf.Max(globalMax, normalized);
            }
        }

        data.SetHeights(0, 0, heights);
        data.size = new Vector3(oldSize.x, targetHeight, oldSize.z);

        maxNoise = globalMax;
        minNoise = globalMin;
        contrast = globalMax - globalMin;

        Debug.Log($"✅ Terrain сгенерирован + растянут до {targetHeight}m! Seed: {seed} | Шум: {globalMin:F2}–{globalMax:F2} | Контраст: {contrast:F2}");
    }

    private float FBM(float x, float z) {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++) {
            total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

    [ContextMenu("Apply Textures")]
    public void ApplyTextures() {
        if (terrain == null) return;

        TerrainData data = terrain.terrainData;

        TerrainLayer[] layers = new TerrainLayer[4];
        layers[0] = CreateLayer("Grass", new Color(0.1f, 0.5f, 0.1f));
        layers[1] = CreateLayer("Sand", new Color(0.8f, 0.7f, 0.4f));
        layers[2] = CreateLayer("Rock", new Color(0.4f, 0.4f, 0.4f));
        layers[3] = CreateLayer("Snow", new Color(0.9f, 0.9f, 0.95f));

        data.terrainLayers = layers;

        int alphaRes = data.alphamapResolution;
        float[,,] splatmap = new float[alphaRes, alphaRes, 4];

        for (int x = 0; x < alphaRes; x++) {
            for (int z = 0; z < alphaRes; z++) {
                float normX = (float)x / alphaRes;
                float normZ = (float)z / alphaRes;
                float height = data.GetHeight(Mathf.RoundToInt(normX * (data.heightmapResolution - 1)),
                                            Mathf.RoundToInt(normZ * (data.heightmapResolution - 1))) / targetHeight;

                Vector3 normal = data.GetInterpolatedNormal(normX, normZ);
                float steepness = normal.y;

                float grass = Mathf.Clamp01(1f - height * 3f) * steepness;
                float sand = Mathf.Clamp01((height - 0.2f) * 5f) * (1f - Mathf.Abs(steepness - 0.8f));
                float rock = Mathf.Clamp01((height - 0.4f) * 3f) * (1f - steepness);
                float snow = Mathf.Clamp01((height - 0.7f) * 5f);

                float total = grass + sand + rock + snow + 0.001f;
                splatmap[z, x, 0] = grass / total;
                splatmap[z, x, 1] = sand / total;
                splatmap[z, x, 2] = rock / total;
                splatmap[z, x, 3] = snow / total;
            }
        }

        data.SetAlphamaps(0, 0, splatmap);
        Debug.Log("✅ Текстуры применены: Трава/Песок/Камень/Снег");
    }

    private TerrainLayer CreateLayer(string name, Color color) {
        TerrainLayer layer = new TerrainLayer();

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        tex.name = name + "_Tex";

        layer.diffuseTexture = tex;
        layer.tileSize = new Vector2(15, 15);
        layer.name = name;

        return layer;
    }
}