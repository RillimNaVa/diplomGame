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

        [Header("Debug UI")]
        public Color debugTint = new Color(0.75f, 0.85f, 1f);
    }
}
