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
    /// Repairs missing House furniture meshes/materials WITHOUT moving, rebuilding, deleting,
    /// or auto-saving the user's House layout.
    ///
    /// The saved HouseFurniture transforms are treated as read-only. A temporary preview scene is
    /// used to rebuild visual source data, then only missing MeshFilter/Renderer references are
    /// copied back to the existing saved furniture.
    /// </summary>
    public static class HouseSavedVisualRepair
    {
        private const string RootName = "HouseFurniture";
        private const string AssetFolder = "Assets/CEVR/Generated/HouseVisualAssets";

        private sealed class TransformState
        {
            public Transform transform;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        [MenuItem("CEVR/House/Repair Missing Furniture Visuals (KEEP TRANSFORMS)", priority = 5)]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("CEVR House visual repair: stop Play mode first.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Debug.LogError("CEVR House visual repair: open House.unity first.");
                return;
            }

            GameObject savedRoot = FindRoot(scene, RootName);
            if (savedRoot == null)
            {
                Debug.LogError("CEVR House visual repair: HouseFurniture was not found. Nothing was changed.");
                return;
            }

            BackupScene(scene);
            List<TransformState> transforms = CaptureTransforms(savedRoot.transform);

            Scene previewScene = default;
            GameObject sourceRoot = null;
            int repairedMeshes = 0;
            int repairedMaterials = 0;

            try
            {
                EnsureAssetFolder();

                previewScene = EditorSceneManager.NewPreviewScene();
                sourceRoot = new GameObject("CEVR_HouseVisualRepair_Source");
                SceneManager.MoveGameObjectToScene(sourceRoot, previewScene);

                var context = new HouseLayoutSharedBuilder.BuildContext
                {
                    scene = previewScene,
                    root = sourceRoot.transform,
                    runtime = false,
                    groundMotion = null,
                    logger = null
                };
                HouseLayoutSharedBuilder.Build(context);

                Dictionary<string, List<Transform>> targetByName =
                    DirectChildrenByName(savedRoot.transform);
                Dictionary<string, List<Transform>> sourceByName =
                    DirectChildrenByName(sourceRoot.transform);

                foreach (KeyValuePair<string, List<Transform>> pair in targetByName)
                {
                    if (!pair.Key.StartsWith("HouseLayout_", StringComparison.Ordinal))
                        continue;
                    if (!sourceByName.TryGetValue(pair.Key, out List<Transform> sources) ||
                        sources.Count == 0)
                        continue;

                    List<Transform> targets = pair.Value;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        Transform source = sources[Mathf.Min(i, sources.Count - 1)];
                        RepairSubtree(
                            targets[i],
                            source,
                            ref repairedMeshes,
                            ref repairedMaterials);
                    }
                }

                int restoredTransforms = RestoreTransforms(transforms);
                if (restoredTransforms != 0)
                {
                    Debug.LogWarning(
                        "CEVR House visual repair restored " + restoredTransforms +
                        " transform change(s). Furniture positions/rotations/scales remain exactly as saved.");
                }

                if (repairedMeshes == 0 && repairedMaterials == 0)
                {
                    Debug.Log(
                        "CEVR HOUSE VISUAL REPAIR: no missing mesh/material references were found. " +
                        "House transforms were not changed.");
                    return;
                }

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                SceneView.RepaintAll();
                EditorApplication.RepaintHierarchyWindow();

                Debug.Log(
                    "CEVR HOUSE VISUAL REPAIR PASS: repaired " + repairedMeshes +
                    " mesh reference(s) and " + repairedMaterials +
                    " material reference(s). NO furniture transform was changed. " +
                    "Inspect the House, then press Cmd/Ctrl+S only if it looks correct.");
            }
            catch (Exception ex)
            {
                RestoreTransforms(transforms);
                Debug.LogException(ex);
            }
            finally
            {
                if (sourceRoot != null)
                    UnityEngine.Object.DestroyImmediate(sourceRoot);
                if (previewScene.IsValid())
                    EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void RepairSubtree(
            Transform targetRoot,
            Transform sourceRoot,
            ref int repairedMeshes,
            ref int repairedMaterials)
        {
            MeshFilter[] targetFilters = targetRoot.GetComponentsInChildren<MeshFilter>(true);
            MeshFilter[] sourceFilters = sourceRoot.GetComponentsInChildren<MeshFilter>(true);
            int filterCount = Mathf.Min(targetFilters.Length, sourceFilters.Length);

            for (int i = 0; i < filterCount; i++)
            {
                if (targetFilters[i].sharedMesh != null) continue;
                Mesh source = sourceFilters[i].sharedMesh;
                if (source == null) continue;

                targetFilters[i].sharedMesh =
                    PersistMesh(source, targetRoot.name, i);
                EditorUtility.SetDirty(targetFilters[i]);
                repairedMeshes++;
            }

            Renderer[] targetRenderers = targetRoot.GetComponentsInChildren<Renderer>(true);
            Renderer[] sourceRenderers = sourceRoot.GetComponentsInChildren<Renderer>(true);
            int rendererCount = Mathf.Min(targetRenderers.Length, sourceRenderers.Length);

            for (int i = 0; i < rendererCount; i++)
            {
                Material[] targetSlots = targetRenderers[i].sharedMaterials;
                Material[] sourceSlots = sourceRenderers[i].sharedMaterials;
                int slotCount = Mathf.Max(targetSlots.Length, sourceSlots.Length);
                if (slotCount == 0) continue;

                if (targetSlots.Length < slotCount)
                    Array.Resize(ref targetSlots, slotCount);

                bool changed = false;
                for (int slot = 0; slot < slotCount; slot++)
                {
                    Material current = targetSlots[slot];
                    bool broken =
                        current == null ||
                        (!EditorUtility.IsPersistent(current) &&
                         (current.name == "Default-Material" ||
                          current.name == "Default Material"));

                    if (!broken || slot >= sourceSlots.Length || sourceSlots[slot] == null)
                        continue;

                    targetSlots[slot] =
                        PersistMaterial(sourceSlots[slot], targetRoot.name, i, slot);
                    repairedMaterials++;
                    changed = true;
                }

                if (!changed) continue;
                targetRenderers[i].sharedMaterials = targetSlots;
                EditorUtility.SetDirty(targetRenderers[i]);
            }
        }

        private static Mesh PersistMesh(Mesh source, string rootName, int index)
        {
            string path =
                AssetFolder + "/" + Safe(rootName) + "_Mesh_" + index.ToString("D2") + ".asset";

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                Mesh created = UnityEngine.Object.Instantiate(source);
                created.name = Safe(rootName) + "_Mesh_" + index.ToString("D2");
                AssetDatabase.CreateAsset(created, path);
                return created;
            }

            EditorUtility.CopySerialized(source, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Material PersistMaterial(
            Material source,
            string rootName,
            int rendererIndex,
            int slotIndex)
        {
            string path =
                AssetFolder + "/" + Safe(rootName) + "_Mat_" +
                rendererIndex.ToString("D2") + "_" + slotIndex.ToString("D2") + ".mat";

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                Material created = new Material(source)
                {
                    name = Safe(rootName) + "_Mat_" +
                           rendererIndex.ToString("D2") + "_" + slotIndex.ToString("D2")
                };
                AssetDatabase.CreateAsset(created, path);
                return created;
            }

            EditorUtility.CopySerialized(source, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Dictionary<string, List<Transform>> DirectChildrenByName(Transform root)
        {
            var map = new Dictionary<string, List<Transform>>();
            foreach (Transform child in root)
            {
                if (!map.TryGetValue(child.name, out List<Transform> list))
                {
                    list = new List<Transform>();
                    map[child.name] = list;
                }
                list.Add(child);
            }
            return map;
        }

        private static List<TransformState> CaptureTransforms(Transform root)
        {
            var result = new List<TransformState>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                result.Add(new TransformState
                {
                    transform = t,
                    localPosition = t.localPosition,
                    localRotation = t.localRotation,
                    localScale = t.localScale
                });
            }
            return result;
        }

        private static int RestoreTransforms(List<TransformState> states)
        {
            int restored = 0;
            foreach (TransformState state in states)
            {
                Transform t = state.transform;
                if (t == null) continue;

                bool changed =
                    (t.localPosition - state.localPosition).sqrMagnitude > 0.00000001f ||
                    Quaternion.Angle(t.localRotation, state.localRotation) > 0.001f ||
                    (t.localScale - state.localScale).sqrMagnitude > 0.00000001f;

                if (!changed) continue;
                t.localPosition = state.localPosition;
                t.localRotation = state.localRotation;
                t.localScale = state.localScale;
                restored++;
            }
            return restored;
        }

        private static void BackupScene(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.path) || !File.Exists(scene.path)) return;

            string directory = Path.Combine("Library", "CEVRBackups");
            Directory.CreateDirectory(directory);
            string destination = Path.Combine(
                directory,
                "House_before_visual_repair_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
            File.Copy(scene.path, destination, true);
            Debug.Log("CEVR House visual repair backup: " + destination);
        }

        private static void EnsureAssetFolder()
        {
            const string generated = "Assets/CEVR/Generated";
            if (!AssetDatabase.IsValidFolder(generated))
                AssetDatabase.CreateFolder("Assets/CEVR", "Generated");
            if (!AssetDatabase.IsValidFolder(AssetFolder))
                AssetDatabase.CreateFolder(generated, "HouseVisualAssets");
        }

        private static GameObject FindRoot(Scene scene, string exactName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == exactName)
                    return root;
            return null;
        }

        private static string Safe(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Replace(' ', '_');
        }
    }
}
