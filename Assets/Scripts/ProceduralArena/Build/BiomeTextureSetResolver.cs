using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Build
{
    public struct BiomeTextureSet
    {
        public Texture2D baseColor;
        public Texture2D normal;
        public Texture2D metallic;
        public Texture2D roughness;
        public Texture2D occlusion;
        public Texture2D height;
        public Texture2D specular;
        public Texture2D emissiveColor;
        public Texture2D emissiveMask;
    }

    public static class BiomeTextureSetResolver
    {
        static readonly Dictionary<string, BiomeTextureSet> SetCache = new Dictionary<string, BiomeTextureSet>(StringComparer.Ordinal);
        static readonly Dictionary<string, Texture2D> GeneratedCache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        public static BiomeTextureSet Resolve(string basePath)
        {
            if (string.IsNullOrEmpty(basePath)) return default;
            if (SetCache.TryGetValue(basePath, out var cached)) return cached;

            var set = new BiomeTextureSet
            {
                baseColor = Resources.Load<Texture2D>(basePath),
                normal = LoadVariant(basePath, "_basecolor", "_normal", "_COLOR", "_NORM", "_color", "_norm"),
                metallic = LoadVariant(basePath, "_basecolor", "_metallic", "_COLOR", "_METALLIC", "_color", "_metallic"),
                roughness = LoadVariant(basePath, "_basecolor", "_roughness", "_COLOR", "_ROUGH", "_color", "_roughness"),
                occlusion = LoadVariant(basePath, "_basecolor", "_ambientOcclusion", "_COLOR", "_OCC", "_color", "_occ"),
                height = LoadVariant(basePath, "_basecolor", "_height", "_COLOR", "_DISP", "_color", "_disp"),
                specular = LoadVariant(basePath, "_basecolor", "_spec", "_COLOR", "_SPEC", "_color", "_spec"),
                emissiveColor = LoadVariant(basePath, "_basecolor", "_Emissive_01_Color"),
                emissiveMask = LoadVariant(basePath, "_basecolor", "_Emissive_01_Mask"),
            };

            SetCache[basePath] = set;
            return set;
        }

        public static Texture2D BuildMetallicGlossMap(string basePath, BiomeTextureSet set, float defaultMetallic, float defaultSmoothness)
        {
            string cacheKey = $"metallic:{basePath}:{defaultMetallic:F3}:{defaultSmoothness:F3}";
            if (GeneratedCache.TryGetValue(cacheKey, out var cached)) return cached;

            Texture2D reference = FirstNonNull(set.baseColor, set.metallic, set.roughness, set.specular, set.occlusion);
            if (reference == null) return null;

            if (!TryGetPixels(set.metallic, reference.width, reference.height, out var metallicPixels))
                metallicPixels = CreateConstantPixels(reference.width, reference.height, defaultMetallic);

            Color[] smoothnessPixels;
            if (TryGetPixels(set.roughness, reference.width, reference.height, out var roughnessPixels))
            {
                smoothnessPixels = new Color[roughnessPixels.Length];
                for (int i = 0; i < roughnessPixels.Length; i++)
                {
                    float smooth = 1f - roughnessPixels[i].grayscale;
                    smoothnessPixels[i] = new Color(smooth, smooth, smooth, smooth);
                }
            }
            else if (TryGetPixels(set.specular, reference.width, reference.height, out var specPixels))
            {
                smoothnessPixels = new Color[specPixels.Length];
                for (int i = 0; i < specPixels.Length; i++)
                {
                    float smooth = Mathf.Lerp(defaultSmoothness * 0.65f, defaultSmoothness, specPixels[i].grayscale);
                    smoothnessPixels[i] = new Color(smooth, smooth, smooth, smooth);
                }
            }
            else
            {
                smoothnessPixels = CreateConstantPixels(reference.width, reference.height, defaultSmoothness);
            }

            var outPixels = new Color[reference.width * reference.height];
            for (int i = 0; i < outPixels.Length; i++)
            {
                float metallic = metallicPixels[i].grayscale;
                float smoothness = smoothnessPixels[i].grayscale;
                outPixels[i] = new Color(metallic, metallic, metallic, smoothness);
            }

            var map = new Texture2D(reference.width, reference.height, TextureFormat.RGBA32, false, true)
            {
                name = $"{Sanitize(basePath)}_MetallicGloss",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            map.SetPixels(outPixels);
            map.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            GeneratedCache[cacheKey] = map;
            return map;
        }

        public static Texture2D BuildEmissionMap(string basePath, BiomeTextureSet set)
        {
            if (set.emissiveColor == null) return null;

            string cacheKey = $"emissive:{basePath}";
            if (GeneratedCache.TryGetValue(cacheKey, out var cached)) return cached;

            if (!TryGetPixels(set.emissiveColor, set.emissiveColor.width, set.emissiveColor.height, out var colorPixels))
                return null;

            Color[] maskPixels = null;
            if (set.emissiveMask != null)
            {
                if (!TryGetPixels(set.emissiveMask, set.emissiveColor.width, set.emissiveColor.height, out maskPixels))
                    maskPixels = null;
            }

            var outPixels = new Color[colorPixels.Length];
            for (int i = 0; i < colorPixels.Length; i++)
            {
                float mask = maskPixels != null ? maskPixels[i].grayscale : 1f;
                outPixels[i] = colorPixels[i] * mask;
                outPixels[i].a = mask;
            }

            var map = new Texture2D(set.emissiveColor.width, set.emissiveColor.height, TextureFormat.RGBA32, false, false)
            {
                name = $"{Sanitize(basePath)}_Emission",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            map.SetPixels(outPixels);
            map.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            GeneratedCache[cacheKey] = map;
            return map;
        }

        static Texture2D LoadVariant(string basePath, params string[] replacements)
        {
            for (int i = 0; i + 1 < replacements.Length; i += 2)
            {
                string candidate = ReplaceSuffix(basePath, replacements[i], replacements[i + 1]);
                if (string.IsNullOrEmpty(candidate)) continue;

                var texture = Resources.Load<Texture2D>(candidate);
                if (texture != null) return texture;
            }

            return null;
        }

        static string ReplaceSuffix(string path, string from, string to)
        {
            if (string.IsNullOrEmpty(path)) return null;
            int idx = path.LastIndexOf(from, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            return path.Substring(0, idx) + to + path.Substring(idx + from.Length);
        }

        static bool TryGetPixels(Texture2D texture, int width, int height, out Color[] pixels)
        {
            pixels = null;
            if (texture == null) return false;

            try
            {
                if (texture.width == width && texture.height == height)
                {
                    pixels = texture.GetPixels();
                    return pixels != null && pixels.Length > 0;
                }

                pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                {
                    float v = height == 1 ? 0f : (float)y / (height - 1);
                    for (int x = 0; x < width; x++)
                    {
                        float u = width == 1 ? 0f : (float)x / (width - 1);
                        pixels[y * width + x] = texture.GetPixelBilinear(u, v);
                    }
                }
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        static Color[] CreateConstantPixels(int width, int height, float value)
        {
            var pixels = new Color[width * height];
            var color = new Color(value, value, value, value);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            return pixels;
        }

        static Texture2D FirstNonNull(params Texture2D[] textures)
        {
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] != null) return textures[i];
            }
            return null;
        }

        static string Sanitize(string value)
        {
            return value.Replace('/', '_').Replace('\\', '_');
        }
    }
}
