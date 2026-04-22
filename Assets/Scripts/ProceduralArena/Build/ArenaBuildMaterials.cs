using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Build
{
    public class ArenaBuildMaterials
    {
        public Material floor;
        public Material wall;
        public Material ceiling;
        public Material cover;
        public Material platform;
        public Material ramp;
        public Material startMarker;
        public Material exitMarker;
        public Material barrier;

        public static ArenaBuildMaterials CreateDefaults(VoidSurvivor.ProceduralArena.Arena.BiomeDefinition biome = null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Color floorColor = biome != null ? biome.floorColor : new Color(0.35f, 0.37f, 0.42f);
            Color wallColor = biome != null ? biome.wallColor : new Color(0.28f, 0.30f, 0.34f);
            Color ceilingColor = biome != null ? biome.ceilingColor : new Color(0.18f, 0.19f, 0.22f);
            Color coverColor = biome != null ? biome.coverColor : new Color(0.45f, 0.40f, 0.35f);
            Color platformColor = biome != null ? biome.platformColor : new Color(0.38f, 0.42f, 0.48f);
            Color rampColor = biome != null ? biome.rampColor : new Color(0.48f, 0.44f, 0.39f);
            Color startColor = biome != null ? biome.startMarkerColor : new Color(0.3f, 1.0f, 0.5f);
            float startIntensity = biome != null ? biome.startMarkerIntensity : 1.5f;
            Color exitColor = biome != null ? biome.exitMarkerColor : new Color(1.0f, 0.3f, 0.3f);
            float exitIntensity = biome != null ? biome.exitMarkerIntensity : 1.8f;
            Color barrierColor = biome != null ? biome.barrierColor : new Color(1.0f, 0.75f, 0.15f);
            float barrierIntensity = biome != null ? biome.barrierIntensity : 2.2f;

            var mats = new ArenaBuildMaterials
            {
                floor       = Make(shader, "ArenaFloorMat", floorColor),
                wall        = Make(shader, "ArenaWallMat", wallColor),
                ceiling     = Make(shader, "ArenaCeilingMat", ceilingColor),
                cover       = Make(shader, "ArenaCoverMat", coverColor),
                platform    = Make(shader, "ArenaPlatformMat", platformColor),
                ramp        = Make(shader, "ArenaRampMat", rampColor),
                startMarker = MakeEmissive(shader, "ArenaStartMat", startColor, startIntensity),
                exitMarker  = MakeEmissive(shader, "ArenaExitMat", exitColor, exitIntensity),
                barrier     = MakeEmissive(shader, "ArenaBarrierMat", barrierColor, barrierIntensity),
            };
            return mats;
        }

        static Material Make(Shader shader, string name, Color c)
        {
            var m = new Material(shader) { name = name };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color"))     m.SetColor("_Color", c);
            return m;
        }

        static Material MakeEmissive(Shader shader, string name, Color c, float intensity)
        {
            var m = Make(shader, name, c);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * intensity);
            }
            return m;
        }
    }
}
