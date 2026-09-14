using UnityEditor;
using UnityEngine;

namespace ChulaEarthquakeVR.Editor
{
    // Scoped to the licensed character assets; other scene imports are unaffected.
    public sealed class StudentAssetImporter : AssetPostprocessor
    {
        private const string SessionRepairKey = "CEVR.StudentAssetsChecked.v2";
        private static readonly string[] RequiredAssets =
        {
            "Assets/CEVR/Resources/Student/Student.fbx",
            "Assets/CEVR/Resources/Student/StudentUniform.png",
            "Assets/CEVR/Resources/Student/Idle.fbx",
            "Assets/CEVR/Resources/Student/Run.fbx"
        };

        [InitializeOnLoadMethod]
        private static void ScheduleMissingAssetRepair()
        {
            EditorApplication.delayCall += RepairStudentAssetsOnce;
        }

        private static void RepairStudentAssetsOnce()
        {
            if (SessionState.GetBool(SessionRepairKey, false)) return;
            SessionState.SetBool(SessionRepairKey, true);

            bool repaired = false;
            foreach (string path in RequiredAssets)
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                bool force = asset == null;

                if (path.EndsWith("StudentUniform.png"))
                {
                    TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                    // The repository previously carried only a minimal .meta file. Reimport the
                    // texture if Unity has not produced a proper Texture2D importer yet.
                    force |= textureImporter == null || AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null;
                }
                else if (path.EndsWith(".fbx"))
                {
                    ModelImporter modelImporter = AssetImporter.GetAtPath(path) as ModelImporter;
                    force |= modelImporter == null;
                }

                if (!force) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                repaired = true;
            }

            if (repaired)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("CEVR repaired/reimported student avatar resources.");
            }
        }

        private bool IsStudent => assetPath.StartsWith("Assets/CEVR/Resources/Student/");

        private void OnPreprocessModel()
        {
            if (!IsStudent) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.addCollider = false;
            importer.importCameras = false;
            importer.importLights = false;

            // Keep material descriptions from the FBX available as a visual fallback. StudentAvatar
            // still supplies the authored university uniform texture when it is available.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsStudent) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = true;
                clip.wrapMode = WrapMode.Loop;
            }
            importer.clipAnimations = clips;
        }

        private void OnPreprocessTexture()
        {
            if (!IsStudent) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
