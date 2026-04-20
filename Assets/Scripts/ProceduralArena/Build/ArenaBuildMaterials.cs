using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Build
{
    public class ArenaBuildMaterials
    {
        public Material floor;
        public Material wall;
        public Material ceiling;
        public Material cover;
        public Material startMarker;
        public Material exitMarker;

        public static ArenaBuildMaterials CreateDefaults()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mats = new ArenaBuildMaterials
            {
                floor       = Make(shader, "ArenaFloorMat",   new Color(0.35f, 0.37f, 0.42f)),
                wall        = Make(shader, "ArenaWallMat",    new Color(0.28f, 0.30f, 0.34f)),
                ceiling     = Make(shader, "ArenaCeilingMat", new Color(0.18f, 0.19f, 0.22f)),
                cover       = Make(shader, "ArenaCoverMat",   new Color(0.45f, 0.40f, 0.35f)),
                startMarker = MakeEmissive(shader, "ArenaStartMat", new Color(0.3f, 1.0f, 0.5f), 1.5f),
                exitMarker  = MakeEmissive(shader, "ArenaExitMat",  new Color(1.0f, 0.3f, 0.3f), 1.8f),
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
