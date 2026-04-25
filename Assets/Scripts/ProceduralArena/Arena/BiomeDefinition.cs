using System;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Arena
{
    [Serializable]
    public class BiomeSurfaceDefinition
    {
        [Tooltip("Preferred authored material. If missing, CreateDefaults builds one from the optional Resources texture path.")]
        public Material material;
        [Tooltip("Optional Resources path without extension, relative to Assets/Resources.")]
        public string resourcePath = string.Empty;
        public Color tint = Color.white;
        [Min(0.1f)] public float textureScale = 1f;
        [Range(0f, 1f)] public float metallic = 0f;
        [Range(0f, 1f)] public float smoothness = 0.5f;
        public Color emissionColor = Color.black;
        [Min(0f)] public float emissionIntensity = 0f;
        public bool doubleSided = false;

        [Header("PR 2.F — depth / detail")]
        [Tooltip("Normal map intensity. 1 = default, 1.5–2.0 for stronger relief on walls.")]
        [Range(0f, 3f)] public float bumpScale = 1f;
        [Tooltip("Parallax strength (URP _Parallax, 0–0.08 effective range). 0 disables.")]
        [Range(0f, 0.08f)] public float parallaxStrength = 0f;
        [Tooltip("Optional detail albedo map Resources path (second UV layer). Leave empty to disable.")]
        public string detailAlbedoResourcePath = string.Empty;
        [Tooltip("Tiling of the detail map relative to the base map.")]
        [Min(1f)] public float detailTextureScale = 8f;
        [Tooltip("Detail map blend strength (0 = off, 1 = full). Applied via _DetailAlbedoMapScale.")]
        [Range(0f, 2f)] public float detailStrength = 0.8f;
    }

    [Serializable]
    public class BiomePostProcessing
    {
        [Header("Enable flags — leave off to keep defaults from SampleSceneProfile")]
        public bool overrideBloom = true;
        public bool overrideColor = true;
        public bool overrideVignette = true;

        [Header("Bloom")]
        [Range(0f, 3f)] public float bloomIntensity = 0.6f;
        [Range(0f, 2f)] public float bloomThreshold = 0.9f;
        [Range(0f, 1f)] public float bloomScatter = 0.7f;
        public Color bloomTint = Color.white;

        [Header("Color Adjustments")]
        [Range(-3f, 3f)] public float postExposure = 0f;
        [Range(-100f, 100f)] public float contrast = 6f;
        public Color colorFilter = Color.white;
        [Range(-180f, 180f)] public float hueShift = 0f;
        [Range(-100f, 100f)] public float saturation = 4f;

        [Header("Vignette")]
        public Color vignetteColor = Color.black;
        [Range(0f, 1f)] public float vignetteIntensity = 0.24f;
        [Range(0.01f, 1f)] public float vignetteSmoothness = 0.35f;
    }

    [CreateAssetMenu(fileName = "BiomeDefinition", menuName = "VoidSurvivor/Arena/Biome Definition")]
    public class BiomeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string biomeId = "void-station";
        public string displayName = "Void Station";

        [Header("Primary Surface Materials")]
        public BiomeSurfaceDefinition floorPrimary = new BiomeSurfaceDefinition();
        public BiomeSurfaceDefinition floorAccent = new BiomeSurfaceDefinition();
        public BiomeSurfaceDefinition wallPrimary = new BiomeSurfaceDefinition();
        public BiomeSurfaceDefinition wallTrim = new BiomeSurfaceDefinition();
        public BiomeSurfaceDefinition ceilingPrimary = new BiomeSurfaceDefinition();

        [Header("Gameplay Geometry Materials")]
        public BiomeSurfaceDefinition coverMaterial = new BiomeSurfaceDefinition();
        public BiomeSurfaceDefinition platformMaterial = new BiomeSurfaceDefinition();
        public BiomeSurfaceDefinition rampMaterial = new BiomeSurfaceDefinition();
        public BiomeSurfaceDefinition propMaterial = new BiomeSurfaceDefinition();

        [Header("Atmosphere / Accent Materials")]
        public BiomeSurfaceDefinition emissiveAccent = new BiomeSurfaceDefinition();
        public bool useContaminationLayer = false;
        public BiomeSurfaceDefinition contaminationMaterial = new BiomeSurfaceDefinition();
        [Range(0f, 1f)] public float contaminationStrength = 0f;
        [Range(0f, 1f)] public float perimeterContaminationBias = 0.5f;
        [Range(0f, 1f)] public float centerCleanBias = 1f;

        [Header("Marker Emissive")]
        public Color startMarkerColor = new Color(0.3f, 1f, 0.5f);
        public float startMarkerIntensity = 1.5f;
        public Color exitMarkerColor = new Color(1f, 0.3f, 0.3f);
        public float exitMarkerIntensity = 1.8f;
        public Color barrierColor = new Color(1f, 0.75f, 0.15f);
        public float barrierIntensity = 2.2f;

        [Header("Atmosphere")]
        public Color fogColor = Color.black;
        [Range(0f, 1f)] public float fogStrength = 0f;
        public Color ambientTint = Color.white;

        [Header("PR 2.F — Runtime Point Lights")]
        [Tooltip("Extra point lights on emissive accents / exit markers. 0 disables.")]
        [Range(0f, 8f)] public float accentLightIntensity = 2.2f;
        [Tooltip("Point light range on emissive accents (meters).")]
        [Min(0f)] public float accentLightRange = 8f;
        [Tooltip("Exit marker point light color override. If alpha=0, falls back to exitMarkerColor.")]
        public Color exitLightColor = new Color(0f, 0f, 0f, 0f);

        [Header("PR 2.F — Post-Processing")]
        public BiomePostProcessing postProcessing = new BiomePostProcessing();

        [Header("Debug UI")]
        public Color debugTint = new Color(0.75f, 0.85f, 1f);
    }
}
