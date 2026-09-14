using System;
using UnityEditor;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>Keeps the small team-authored furniture FBX imports deterministic and gameplay-neutral.</summary>
    public sealed class TeamFurnitureAssetImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/CEVR/Resources/PlengFurniture/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder, StringComparison.Ordinal) ||
                !assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = false;
        }
    }
}
