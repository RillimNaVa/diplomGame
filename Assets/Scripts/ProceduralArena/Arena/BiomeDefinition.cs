using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Arena
{
    [CreateAssetMenu(fileName = "BiomeDefinition", menuName = "VoidSurvivor/Arena/Biome Definition")]
    public class BiomeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string biomeId = "void-station";
        public string displayName = "Void Station";

        [Header("Geometry colors")]
        public Color floorColor = new Color(0.35f, 0.37f, 0.42f);
        public Color wallColor = new Color(0.28f, 0.30f, 0.34f);
        public Color ceilingColor = new Color(0.18f, 0.19f, 0.22f);
        public Color coverColor = new Color(0.45f, 0.40f, 0.35f);
        public Color platformColor = new Color(0.38f, 0.42f, 0.48f);
        public Color rampColor = new Color(0.48f, 0.44f, 0.39f);

        [Header("Emissive accents")]
        public Color startMarkerColor = new Color(0.3f, 1f, 0.5f);
        public float startMarkerIntensity = 1.5f;
        public Color exitMarkerColor = new Color(1f, 0.3f, 0.3f);
        public float exitMarkerIntensity = 1.8f;
        public Color barrierColor = new Color(1f, 0.75f, 0.15f);
        public float barrierIntensity = 2.2f;

        [Header("Debug UI")]
        public Color debugTint = new Color(0.75f, 0.85f, 1f);
    }
}
