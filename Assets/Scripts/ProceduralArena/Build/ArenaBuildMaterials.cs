using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.ProceduralArena.Build
{
    public class ArenaBuildMaterials
    {
        public BiomeDefinition sourceBiome;
        public Material floor;
        public Material floorAccent;
        public Material wall;
        public Material wallTrim;
        public Material ceiling;
        public Material cover;
        public Material platform;
        public Material ramp;
        public Material prop;
        public Material emissiveAccent;
        public Material contamination;
        public Material startMarker;
        public Material exitMarker;
        public Material barrier;
        public Material lampPanel;

        public static ArenaBuildMaterials CreateDefaults(BiomeDefinition biome = null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Color startColor = biome != null ? biome.startMarkerColor : new Color(0.3f, 1.0f, 0.5f);
            float startIntensity = biome != null ? biome.startMarkerIntensity : 1.5f;
            Color exitColor = biome != null ? biome.exitMarkerColor : new Color(1.0f, 0.3f, 0.3f);
            float exitIntensity = biome != null ? biome.exitMarkerIntensity : 1.8f;
            Color barrierColor = biome != null ? biome.barrierColor : new Color(1.0f, 0.75f, 0.15f);
            float barrierIntensity = biome != null ? biome.barrierIntensity : 2.2f;

            var mats = new ArenaBuildMaterials
            {
                sourceBiome = biome,
                floor       = CreateSurface(shader, biome != null ? biome.floorPrimary : null, "ArenaFloorMat", new Color(0.35f, 0.37f, 0.42f), 0.8f, 0.52f),
                floorAccent = CreateSurface(shader, biome != null ? biome.floorAccent : null, "ArenaFloorAccentMat", new Color(0.28f, 0.42f, 0.55f), 0.9f, 0.6f),
                wall        = CreateSurface(shader, biome != null ? biome.wallPrimary : null, "ArenaWallMat", new Color(0.28f, 0.30f, 0.34f), 0.75f, 0.45f),
                wallTrim    = CreateSurface(shader, biome != null ? biome.wallTrim : null, "ArenaWallTrimMat", new Color(0.46f, 0.48f, 0.55f), 0.85f, 0.55f),
                ceiling     = CreateSurface(shader, biome != null ? biome.ceilingPrimary : null, "ArenaCeilingMat", new Color(0.18f, 0.19f, 0.22f), 0.65f, 0.35f),
                cover       = CreateSurface(shader, biome != null ? biome.coverMaterial : null, "ArenaCoverMat", new Color(0.45f, 0.40f, 0.35f), 0.65f, 0.4f),
                platform    = CreateSurface(shader, biome != null ? PickFirst(biome.platformMaterial, biome.floorAccent, biome.floorPrimary) : null, "ArenaPlatformMat", new Color(0.38f, 0.42f, 0.48f), 0.8f, 0.6f),
                ramp        = CreateSurface(shader, biome != null ? PickFirst(biome.rampMaterial, biome.floorPrimary) : null, "ArenaRampMat", new Color(0.48f, 0.44f, 0.39f), 0.8f, 0.45f),
                prop        = CreateSurface(shader, biome != null ? PickFirst(biome.propMaterial, biome.wallTrim, biome.wallPrimary) : null, "ArenaPropMat", new Color(0.42f, 0.42f, 0.46f), 0.75f, 0.48f),
                emissiveAccent = CreateSurface(shader, biome != null ? biome.emissiveAccent : null, "ArenaEmissiveAccentMat", new Color(0.1f, 0.65f, 0.95f), 0f, 0.35f),
                contamination = CreateSurface(shader, biome != null ? biome.contaminationMaterial : null, "ArenaContaminationMat", new Color(0.45f, 0.18f, 0.28f), 0.15f, 0.72f),
                startMarker = MakeEmissive(shader, "ArenaStartMat", startColor, startIntensity),
                exitMarker  = MakeEmissive(shader, "ArenaExitMat", exitColor, exitIntensity),
                barrier     = MakeForceField(shader, "ArenaBarrierMat", barrierColor, barrierIntensity),
                // Ceiling-lamp panel: bright neutral-warm white, mostly biome-agnostic so
                // every arena reads as actually lit. Slight tint pull toward biome ambient
                // for cohesion.
                lampPanel   = MakeEmissive(shader, "ArenaLampPanelMat",
                    Color.Lerp(new Color(1f, 0.97f, 0.92f), biome != null ? biome.ambientTint : Color.white, 0.2f),
                    4.5f),
            };
            return mats;
        }

        static BiomeSurfaceDefinition PickFirst(params BiomeSurfaceDefinition[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) return slots[i];
            }
            return null;
        }

        static Material CreateSurface(
            Shader shader,
            BiomeSurfaceDefinition slot,
            string fallbackName,
            Color fallbackColor,
            float fallbackMetallic,
            float fallbackSmoothness)
        {
            var material = slot != null && slot.material != null
                ? new Material(slot.material)
                : new Material(shader);

            material.name = fallbackName;

            var tint = slot != null ? slot.tint : fallbackColor;
            if (slot != null && slot.material == null && !string.IsNullOrEmpty(slot.resourcePath))
            {
                ApplyTextureSet(material, slot, fallbackMetallic, fallbackSmoothness);
            }

            // Per-instance UV tiling (WorldUVScaler) overrides _BaseMap_ST at runtime
            // based on each box' lossyScale, so the material-level tile is identity.
            // We keep the slot.textureScale value as the *density* (tiles per meter)
            // pushed into the registry, instead of writing into the material's ST.
            float textureScale = slot != null ? Mathf.Max(0.1f, slot.textureScale) : 1f;
            float tilesPerMeter = textureScale * 0.25f; // textureScale=1 -> 1 tile per 4m, =4 -> 1 tile per 1m
            WorldUVDensityRegistry.Register(material, tilesPerMeter);

            // Enable GPU instancing where possible (markers/decor without textures
            // benefit; SRP batcher path takes over for textured slots when MPB
            // would otherwise break batching).
            material.enableInstancing = true;

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", slot != null ? slot.metallic : fallbackMetallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", slot != null ? slot.smoothness : fallbackSmoothness);

            float emissionIntensity = slot != null ? slot.emissionIntensity : 0f;
            if (material.HasProperty("_EmissionColor"))
            {
                if (emissionIntensity > 0.001f)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", slot.emissionColor * emissionIntensity);
                }
                else
                {
                    material.SetColor("_EmissionColor", Color.black);
                }
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", slot != null && slot.doubleSided ? 0f : 2f);
            }

            return material;
        }

        static void ApplyTextureSet(Material material, BiomeSurfaceDefinition slot, float fallbackMetallic, float fallbackSmoothness)
        {
            var set = BiomeTextureSetResolver.Resolve(slot.resourcePath);
            if (set.baseColor != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", set.baseColor);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", set.baseColor);
            }

            if (set.normal != null && material.HasProperty("_BumpMap"))
            {
                material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_BumpMap", set.normal);
                if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", Mathf.Max(0f, slot.bumpScale));
            }

            if (set.occlusion != null && material.HasProperty("_OcclusionMap"))
            {
                material.SetTexture("_OcclusionMap", set.occlusion);
                if (material.HasProperty("_OcclusionStrength")) material.SetFloat("_OcclusionStrength", 1f);
            }

            if (set.height != null && slot.parallaxStrength > 0.0001f && material.HasProperty("_ParallaxMap"))
            {
                material.EnableKeyword("_PARALLAXMAP");
                material.SetTexture("_ParallaxMap", set.height);
                if (material.HasProperty("_Parallax")) material.SetFloat("_Parallax", Mathf.Clamp(slot.parallaxStrength, 0.005f, 0.08f));
            }

            if (!string.IsNullOrEmpty(slot.detailAlbedoResourcePath))
            {
                var detail = Resources.Load<Texture2D>(slot.detailAlbedoResourcePath);
                if (detail != null)
                {
                    if (material.HasProperty("_DetailAlbedoMap"))
                    {
                        material.EnableKeyword("_DETAIL_MULX2");
                        material.SetTexture("_DetailAlbedoMap", detail);
                        float ts = Mathf.Max(1f, slot.detailTextureScale);
                        material.SetTextureScale("_DetailAlbedoMap", new Vector2(ts, ts));
                        if (material.HasProperty("_DetailAlbedoMapScale"))
                            material.SetFloat("_DetailAlbedoMapScale", slot.detailStrength);
                    }
                }
            }

            var glossMap = BiomeTextureSetResolver.BuildMetallicGlossMap(slot.resourcePath, set,
                slot != null ? slot.metallic : fallbackMetallic,
                slot != null ? slot.smoothness : fallbackSmoothness);
            if (glossMap != null && material.HasProperty("_MetallicGlossMap"))
            {
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetTexture("_MetallicGlossMap", glossMap);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f);
            }

            var emissionMap = BiomeTextureSetResolver.BuildEmissionMap(slot.resourcePath, set);
            if (emissionMap != null && material.HasProperty("_EmissionMap"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", emissionMap);
            }
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
            m.enableInstancing = true;
            // Markers are usually small; default density keeps them from looking weird
            // if a future biome attaches a real texture to them.
            WorldUVDensityRegistry.Register(m, 0.5f);
            return m;
        }

        /// <summary>
        /// PR 5.A — animated force-field material for soft-lock barriers.
        /// Falls back to MakeEmissive if VoidSurvivor/ForceField shader is
        /// missing (so a stripped build still gets a visible barrier).
        /// </summary>
        static Material MakeForceField(Shader fallbackShader, string name, Color c, float intensity)
        {
            Shader ff = Shader.Find("VoidSurvivor/ForceField");
            if (ff == null) return MakeEmissive(fallbackShader, name, c, intensity);
            var m = new Material(ff);
            m.name = name;
            // _BaseColor uses RGB tint with alpha = base transparency.
            Color tint = c;
            tint.a = 0.55f;
            m.SetColor("_BaseColor", tint * intensity);
            m.SetColor("_RimColor", c * (intensity * 1.6f));
            m.enableInstancing = false;  // transparent shader, batching not relevant
            return m;
        }
    }
}
