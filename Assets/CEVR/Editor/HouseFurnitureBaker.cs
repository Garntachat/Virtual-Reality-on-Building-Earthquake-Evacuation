using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>
    /// Converts the shared House visual layout into NORMAL serialized scene objects.
    ///
    /// After this runs, House.unity contains:
    ///   HouseFurniture
    ///     HouseLayout_DiningTable
    ///     HouseLayout_DiningChair x4
    ///     HouseLayout_Sofa
    ///     HouseLayout_Bed
    ///     ...
    ///
    /// These are the SAME objects Play mode uses. There is no editor-only furniture preview anymore.
    /// </summary>
    [InitializeOnLoad]
    public static class HouseFurnitureBaker
    {
        private const string RootName = "HouseFurniture";
        private const string OldPreviewRoot = "CEVR_HouseEditorFurniturePreview";
        private const string GeneratedAssetFolder = "Assets/CEVR/Generated/HouseBakedAssets";

        private static bool baking;

        static HouseFurnitureBaker()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureActiveHouseIsBaked;
        }

        [MenuItem("CEVR/House/Bake Play Layout Into House Scene", priority = 10)]
        public static void BakeMenu()
        {
            BakeActiveHouseScene(true);
        }

        [MenuItem("CEVR/House/Rebuild House Furniture From Play Layout", priority = 11)]
        public static void RebuildMenu()
        {
            BakeActiveHouseScene(true, true);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!IsHouse(scene)) return;
            EditorApplication.delayCall += EnsureActiveHouseIsBaked;
        }

        private static void EnsureActiveHouseIsBaked()
        {
            if (baking || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!IsHouse(scene)) return;

            GameObject root = FindRoot(scene, RootName);
            HouseBakedLayoutMarker marker = root == null ? null : root.GetComponent<HouseBakedLayoutMarker>();
            if (root != null && marker != null &&
                marker.LayoutVersion == HouseBakedLayoutMarker.CurrentLayoutVersion)
                return;

            Debug.Log("CEVR House scene has no current serialized furniture layout. Baking the exact Play layout into House.unity now.");
            BakeActiveHouseScene(true, true);
        }

        private static void BakeActiveHouseScene(bool saveScene, bool force = false)
        {
            if (baking || EditorApplication.isPlayingOrWillChangePlaymode) return;

            Scene scene = SceneManager.GetActiveScene();
            if (!IsHouse(scene))
            {
                Debug.LogError("CEVR House bake: open the House scene first.");
                return;
            }

            GameObject existing = FindRoot(scene, RootName);
            HouseBakedLayoutMarker current = existing == null ? null : existing.GetComponent<HouseBakedLayoutMarker>();
            if (!force && existing != null && current != null &&
                current.LayoutVersion == HouseBakedLayoutMarker.CurrentLayoutVersion)
            {
                Debug.Log("CEVR HouseFurniture is already baked at the current layout version.");
                return;
            }

            baking = true;
            BackupSceneFile(scene);
            try
            {
                RemoveRoot(scene, OldPreviewRoot);
                RemoveRoot(scene, "CEVR_HouseRuntimeLayout");
                RemoveRoot(scene, RootName);

                if (AssetDatabase.IsValidFolder(GeneratedAssetFolder))
                    AssetDatabase.DeleteAsset(GeneratedAssetFolder);
                EnsureFolder("Assets/CEVR/Generated");
                AssetDatabase.CreateFolder("Assets/CEVR/Generated", "HouseBakedAssets");

                GameObject root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                HouseBakedLayoutMarker marker = root.AddComponent<HouseBakedLayoutMarker>();
                marker.StampCurrentVersion();

                var context = new HouseLayoutSharedBuilder.BuildContext
                {
                    scene = scene,
                    root = root.transform,
                    runtime = false,
                    groundMotion = null,
                    logger = null
                };
                HouseLayoutSharedBuilder.Build(context);

                // Shared builder makes temporary templates for procedural furniture generation.
                // They must never become visible or serialized furniture.
                RemoveTemplateChildren(root.transform);

                PersistTransientMeshesAndMaterials(root);

                bool valid = HouseLayoutSharedBuilder.ValidateBuiltLayout(scene, root.transform, out string report);
                bool persistent = ValidatePersistentAssets(root, out string persistenceReport);
                string signature = HouseLayoutSharedBuilder.ComputeVisualSignature(root.transform);

                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();

                if (saveScene)
                    EditorSceneManager.SaveScene(scene);

                Selection.activeGameObject = root;
                SceneView.lastActiveSceneView?.FrameSelected();
                EditorApplication.RepaintHierarchyWindow();
                SceneView.RepaintAll();

                if (valid && persistent)
                {
                    Debug.Log(
                        "CEVR HOUSE BAKE PASS: HouseFurniture is now normal saved scene furniture. " +
                        "Play mode reuses these exact objects. visual-signature=" + signature);
                }
                else
                {
                    Debug.LogError(
                        "CEVR HOUSE BAKE VALIDATION FAILED: layout=[" + report + "] persistence=[" +
                        persistenceReport + "]; visual-signature=" + signature);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                AssetDatabase.Refresh();
                baking = false;
            }
        }

        private static bool ValidatePersistentAssets(GameObject root, out string report)
        {
            var problems = new List<string>();

            if (root.hideFlags != HideFlags.None)
                problems.Add("HouseFurniture root has non-zero HideFlags");
            if (root.CompareTag("EditorOnly"))
                problems.Add("HouseFurniture is tagged EditorOnly");

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null && !EditorUtility.IsPersistent(filter.sharedMesh))
                    problems.Add("transient mesh on " + filter.gameObject.name);
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && !EditorUtility.IsPersistent(material))
                        problems.Add("transient material on " + renderer.gameObject.name);
                }
            }

            report = problems.Count == 0 ? "PASS" : string.Join("; ", problems);
            return problems.Count == 0;
        }

        private static void BackupSceneFile(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.path) || !File.Exists(scene.path)) return;

            string backupDirectory = Path.Combine("Library", "CEVRBackups");
            Directory.CreateDirectory(backupDirectory);
            string backupName =
                $"House_before_bake_{DateTime.Now:yyyyMMdd_HHmmss}.unity";
            string backupPath = Path.Combine(backupDirectory, backupName);
            File.Copy(scene.path, backupPath, true);
            Debug.Log("CEVR House bake backup created at " + backupPath);
        }

        private static void PersistTransientMeshesAndMaterials(GameObject root)
        {
            int meshIndex = 0;
            int materialIndex = 0;
            var materialMap = new Dictionary<Material, Material>();

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || EditorUtility.IsPersistent(mesh)) continue;

                Mesh persistent = UnityEngine.Object.Instantiate(mesh);
                persistent.name = Sanitize(filter.gameObject.name) + "_Mesh";
                string path = AssetDatabase.GenerateUniqueAssetPath(
                    $"{GeneratedAssetFolder}/{meshIndex++:D3}_{persistent.name}.asset");
                AssetDatabase.CreateAsset(persistent, path);
                filter.sharedMesh = persistent;
                EditorUtility.SetDirty(filter);
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < slots.Length; i++)
                {
                    Material material = slots[i];
                    if (material == null || EditorUtility.IsPersistent(material)) continue;

                    if (!materialMap.TryGetValue(material, out Material persistent))
                    {
                        persistent = new Material(material)
                        {
                            name = Sanitize(renderer.gameObject.name) + "_Material"
                        };
                        string path = AssetDatabase.GenerateUniqueAssetPath(
                            $"{GeneratedAssetFolder}/{materialIndex++:D3}_{persistent.name}.mat");
                        AssetDatabase.CreateAsset(persistent, path);
                        materialMap[material] = persistent;
                    }

                    slots[i] = persistent;
                    changed = true;
                }

                if (!changed) continue;
                renderer.sharedMaterials = slots;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void RemoveTemplateChildren(Transform root)
        {
            var remove = new List<GameObject>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root) continue;
                if (t.name.StartsWith("HouseLayoutTemplate_", StringComparison.Ordinal))
                    remove.Add(t.gameObject);
            }

            foreach (GameObject go in remove)
                UnityEngine.Object.DestroyImmediate(go);
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Replace(' ', '_');
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static bool IsHouse(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded &&
                   scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static GameObject FindRoot(Scene scene, string exactName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == exactName) return root;
            return null;
        }

        private static void RemoveRoot(Scene scene, string exactName)
        {
            GameObject root = FindRoot(scene, exactName);
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
