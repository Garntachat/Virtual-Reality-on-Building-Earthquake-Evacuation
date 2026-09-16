using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>
    /// Keeps the editable House and Tutorial scenes free of magenta materials.
    /// Runtime material repair cannot affect Scene view, so this editor hook replaces
    /// only missing or unsupported material slots with project-owned Built-in RP assets.
    /// </summary>
    [InitializeOnLoad]
    internal static class EditableSceneMaterialRepair
    {
        private const string WallMaterialPath = "Assets/CEVR/Generated/Materials/WallWhite.mat";
        private const string FloorMaterialPath = "Assets/CEVR/Generated/Materials/Concrete.mat";
        private static bool repairQueued;

        static EditableSceneMaterialRepair()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.hierarchyChanged -= QueueRepair;
            EditorApplication.hierarchyChanged += QueueRepair;
            QueueRepair();
        }

        [MenuItem("CEVR/Repair Pink Materials In Open Scenes")]
        private static void RepairFromMenu()
        {
            int repaired = RepairLoadedGameplayScenes();
            if (repaired == 0)
                Debug.Log("CEVR found no unsupported material slots in the open House or Tutorial scene.");
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (IsGameplayScene(scene)) QueueRepair();
        }

        private static void QueueRepair()
        {
            if (repairQueued || EditorApplication.isPlayingOrWillChangePlaymode) return;
            repairQueued = true;
            EditorApplication.delayCall += RunQueuedRepair;
        }

        private static void RunQueuedRepair()
        {
            repairQueued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            RepairLoadedGameplayScenes();
        }

        private static int RepairLoadedGameplayScenes()
        {
            Material wall = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);
            Material floor = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
            if (!IsUsable(wall) || !IsUsable(floor))
            {
                Debug.LogError("CEVR cannot repair Scene-view materials because WallWhite.mat or Concrete.mat is missing or unsupported.");
                return 0;
            }

            int repairedSlots = 0;
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!IsGameplayScene(scene)) continue;

                int sceneRepairs = RepairScene(scene, wall, floor);
                repairedSlots += sceneRepairs;
                if (sceneRepairs > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"CEVR repaired {sceneRepairs} unsupported material slot(s) in the editable scene '{scene.name}'. Save the scene once with Cmd/Ctrl+S to persist the references.");
                }
            }

            if (repairedSlots > 0) SceneView.RepaintAll();
            return repairedSlots;
        }

        private static int RepairScene(Scene scene, Material wall, Material floor)
        {
            int repairedSlots = 0;
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.scene != scene) continue;

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                Material fallback = SelectFallback(renderer, wall, floor);
                for (int i = 0; i < materials.Length; i++)
                {
                    if (!NeedsRepair(materials[i])) continue;
                    if (!changed) Undo.RecordObject(renderer, "Repair Pink CEVR Material");
                    materials[i] = fallback;
                    repairedSlots++;
                    changed = true;
                }

                if (!changed) continue;
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }

            return repairedSlots;
        }

        private static Material SelectFallback(Renderer renderer, Material wall, Material floor)
        {
            string objectName = renderer.name ?? string.Empty;
            string parentName = renderer.transform.parent == null
                ? string.Empty
                : renderer.transform.parent.name ?? string.Empty;
            string combined = objectName + " " + parentName;

            return Contains(combined, "floor") ||
                   Contains(combined, "ground") ||
                   Contains(combined, "plane") ||
                   Contains(combined, "stair") ||
                   Contains(combined, "concrete")
                ? floor
                : wall;
        }

        private static bool NeedsRepair(Material material)
        {
            if (material == null || material.shader == null) return true;
            if (!material.shader.isSupported) return true;

            string shaderName = material.shader.name ?? string.Empty;
            return Contains(shaderName, "InternalErrorShader") ||
                   Contains(shaderName, "Hidden/InternalError");
        }

        private static bool IsUsable(Material material)
        {
            return material != null && material.shader != null && material.shader.isSupported &&
                   !NeedsRepair(material);
        }

        private static bool IsGameplayScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(scene.path)) return false;
            string path = scene.path.Replace('\\', '/');
            return path.EndsWith("/House.unity", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith("/CEVR_ChulaEngineering_Tutorial.unity", StringComparison.OrdinalIgnoreCase);
        }

        private static bool Contains(string value, string fragment)
        {
            return value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
