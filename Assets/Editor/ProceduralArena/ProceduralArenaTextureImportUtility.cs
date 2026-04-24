using System.IO;
using UnityEditor;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Editor
{
    public class ProceduralArenaTextureImportUtility : AssetPostprocessor
    {
        const string Root = "Assets/Resources/ProceduralArena/Biomes/";
        const string SessionKey = "VoidSurvivor.ProceduralArena.TextureReimported";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;

            var importer = (TextureImporter)assetImporter;
            string fileName = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();

            importer.isReadable = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;

            bool isNormal = fileName.EndsWith("_normal") || fileName.EndsWith("_norm");
            if (isNormal)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                return;
            }

            bool isLinearMask =
                fileName.EndsWith("_metallic") ||
                fileName.EndsWith("_roughness") ||
                fileName.EndsWith("_ambientocclusion") ||
                fileName.EndsWith("_occ") ||
                fileName.EndsWith("_height") ||
                fileName.EndsWith("_disp") ||
                fileName.EndsWith("_spec") ||
                fileName.EndsWith("_mask");

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = !isLinearMask;
        }

        [InitializeOnLoadMethod]
        static void EnsureBiomeTexturesAreReimported()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            if (!Directory.Exists(Root))
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            SessionState.SetBool(SessionKey, true);
        }
    }
}
