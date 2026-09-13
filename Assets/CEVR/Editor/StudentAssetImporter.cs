using UnityEditor;
using UnityEngine;

namespace ChulaEarthquakeVR.Editor
{
    // Scoped to the licensed character assets; other scene imports are unaffected.
    public sealed class StudentAssetImporter : AssetPostprocessor
    {
        private const string SessionRepairKey = "CEVR.StudentAssetsChecked";
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
            EditorApplication.delayCall += RepairMissingAssetsOnce;
        }

        private static void RepairMissingAssetsOnce()
        {
            if (SessionState.GetBool(SessionRepairKey, false)) return;
            SessionState.SetBool(SessionRepairKey, true);
            bool repaired = false;
            foreach (string path in RequiredAssets)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                repaired = true;
            }
            if (repaired) Debug.Log("CEVR reimported missing student avatar resources.");
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
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }
        private void OnPreprocessAnimation()
        {
            if (!IsStudent) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips) { clip.loopTime = true; clip.wrapMode = WrapMode.Loop; }
            importer.clipAnimations = clips;
        }
        private void OnPreprocessTexture()
        {
            if (!IsStudent) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
