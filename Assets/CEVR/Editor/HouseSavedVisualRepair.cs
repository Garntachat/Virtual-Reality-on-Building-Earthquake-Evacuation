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
    /// Restores the VISUAL layer of the saved House furniture without changing the user's layout.
    ///
    /// HouseFurniture root transforms are the source of truth. For every HouseLayout_* item we build
    /// a clean visual source in a temporary preview scene, place that visual INSIDE the already-saved
    /// furniture root, and keep the saved root position/rotation/scale untouched.
    ///
    /// Nothing runs automatically and this tool never saves House.unity for the user.
    /// </summary>
    public static class HouseSavedVisualRepair
    {
        private const string RootName = "HouseFurniture";
        private const string RestoredVisualName = "__CEVR_RESTORED_VISUAL";
        private const string AssetFolder = "Assets/CEVR/Generated/HouseVisualAssets";

        private sealed class TransformState
        {
            public Transform transform;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        [MenuItem("CEVR/House/RESTORE Furniture Colors + Kitchen (KEEP LAYOUT)", priority = 1)]
        public static void RestoreAllFurnitureVisuals()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("CEVR House restore: stop Play mode first.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Debug.LogError("CEVR House restore: open House.unity first.");
                return;
            }

            GameObject savedRoot = FindRoot(scene, RootName);
            if (savedRoot == null)
            {
                Debug.LogError(
                    "CEVR House restore: HouseFurniture was not found. Nothing was changed.");
                return;
            }

            BackupScene(scene);
            List<TransformState> savedTransforms = CaptureTransforms(savedRoot.transform);

            Scene previewScene = default;
            GameObject sourceRoot = null;
            int restoredObjects = 0;
            int restoredRenderers = 0;
            int persistedMeshes = 0;
            int persistedMaterials = 0;

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
                        Transform target = targets[i];
                        Transform source = sources[Mathf.Min(i, sources.Count - 1)];

                        if (!HasVisualContent(source))
                            continue;

                        RemovePreviousRestoredVisual(target);
                        DisableOldVisualRenderers(target);

                        GameObject visual = new GameObject(RestoredVisualName);
                        visual.transform.SetParent(target, false);
                        visual.transform.localPosition = Vector3.zero;
                        visual.transform.localRotation = Quaternion.identity;
                        visual.transform.localScale = Vector3.one;

                        // Copy only the SOURCE ROOT'S VISUAL CONTENT. Do not clone the source root
                        // transform itself, because target already contains the user's saved
                        // position/rotation/scale. Cloning the source root and zeroing it was the
                        // reason visuals disappeared or ended up the wrong size.
                        CloneRootVisualComponents(source, visual.transform);
                        foreach (Transform sourceChild in source)
                        {
                            GameObject clonedChild =
                                UnityEngine.Object.Instantiate(sourceChild.gameObject);
                            clonedChild.name = sourceChild.name;
                            clonedChild.transform.SetParent(visual.transform, false);
                            clonedChild.transform.localPosition = sourceChild.localPosition;
                            clonedChild.transform.localRotation = sourceChild.localRotation;
                            clonedChild.transform.localScale = sourceChild.localScale;
                        }

                        SetHideFlagsRecursive(visual, HideFlags.None);
                        StripNonVisualComponents(visual);
                        PersistVisualAssets(
                            visual,
                            Safe(target.name) + "_" + i.ToString("D2"),
                            ref persistedMeshes,
                            ref persistedMaterials);

                        restoredObjects++;
                        restoredRenderers += visual.GetComponentsInChildren<Renderer>(true).Length;
                    }
                }

                int transformCorrections = RestoreTransforms(savedTransforms);

                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                SceneView.RepaintAll();
                EditorApplication.RepaintHierarchyWindow();

                if (transformCorrections != 0)
                {
                    Debug.LogWarning(
                        "CEVR House restore blocked " + transformCorrections +
                        " transform change(s). Your saved furniture layout was restored exactly.");
                }

                Debug.Log(
                    "CEVR HOUSE COLORS/KITCHEN RESTORE READY: rebuilt visuals for " +
                    restoredObjects + " furniture/decor root(s), " +
                    restoredRenderers + " renderer(s), " +
                    persistedMeshes + " generated mesh asset(s), and " +
                    persistedMaterials + " generated material asset(s). " +
                    "Furniture ROOT position/rotation/scale was NOT changed. " +
                    "Each restored object now has a __CEVR_RESTORED_VISUAL child. " +
                    "Inspect the House now. Press Cmd/Ctrl+S only after it looks correct.");
            }
            catch (Exception ex)
            {
                RestoreTransforms(savedTransforms);
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

        // Keep the previous menu name as an alias so an older instruction still works.
        [MenuItem("CEVR/House/Repair Missing Furniture Visuals (KEEP TRANSFORMS)", priority = 2)]
        public static void RepairMissingFurnitureVisuals()
        {
            RestoreAllFurnitureVisuals();
        }

        private static bool HasVisualContent(Transform root)
        {
            return root.GetComponentsInChildren<Renderer>(true).Length > 0;
        }

        private static void RemovePreviousRestoredVisual(Transform target)
        {
            Transform previous = target.Find(RestoredVisualName);
            if (previous != null)
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
        }

        private static void DisableOldVisualRenderers(Transform target)
        {
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.transform.IsChildOf(target) &&
                    renderer.transform.parent != null &&
                    renderer.transform.parent.name == RestoredVisualName)
                    continue;

                renderer.enabled = false;
            }
        }

        private static void CloneRootVisualComponents(Transform source, Transform destination)
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();

            if (sourceFilter == null && sourceRenderer == null)
                return;

            GameObject rootVisual = new GameObject("__RootVisual");
            rootVisual.transform.SetParent(destination, false);
            rootVisual.transform.localPosition = Vector3.zero;
            rootVisual.transform.localRotation = Quaternion.identity;
            rootVisual.transform.localScale = Vector3.one;

            if (sourceFilter != null)
            {
                MeshFilter filter = rootVisual.AddComponent<MeshFilter>();
                filter.sharedMesh = sourceFilter.sharedMesh;
            }

            if (sourceRenderer != null)
            {
                MeshRenderer renderer = rootVisual.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = sourceRenderer.sharedMaterials;
                renderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                renderer.receiveShadows = sourceRenderer.receiveShadows;
            }
        }

        private static void PersistVisualAssets(
            GameObject visualRoot,
            string assetPrefix,
            ref int meshCount,
            ref int materialCount)
        {
            MeshFilter[] filters = visualRoot.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null || EditorUtility.IsPersistent(mesh))
                    continue;

                string path =
                    AssetFolder + "/" + assetPrefix + "_Mesh_" + i.ToString("D3") + ".asset";
                Mesh persistent = AssetDatabase.LoadAssetAtPath<Mesh>(path);

                if (persistent == null)
                {
                    persistent = UnityEngine.Object.Instantiate(mesh);
                    persistent.name = assetPrefix + "_Mesh_" + i.ToString("D3");
                    AssetDatabase.CreateAsset(persistent, path);
                    meshCount++;
                }
                else
                {
                    EditorUtility.CopySerialized(mesh, persistent);
                    EditorUtility.SetDirty(persistent);
                }

                filters[i].sharedMesh = persistent;
                EditorUtility.SetDirty(filters[i]);
            }

            var materialCache = new Dictionary<Material, Material>();
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] slots = renderers[r].sharedMaterials;
                bool changed = false;

                for (int slot = 0; slot < slots.Length; slot++)
                {
                    Material source = slots[slot];
                    if (source == null)
                        continue;

                    // Even if the source material is a persistent FBX sub-asset, create a normal
                    // project material. That makes the restored color/shader portable and prevents
                    // another missing/external-material reference.
                    if (!materialCache.TryGetValue(source, out Material persistent))
                    {
                        string path =
                            AssetFolder + "/" + assetPrefix + "_Mat_" +
                            r.ToString("D3") + "_" + slot.ToString("D2") + ".mat";

                        persistent = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (persistent == null)
                        {
                            persistent = new Material(source)
                            {
                                name = assetPrefix + "_Mat_" +
                                       r.ToString("D3") + "_" + slot.ToString("D2")
                            };
                            AssetDatabase.CreateAsset(persistent, path);
                            materialCount++;
                        }
                        else
                        {
                            EditorUtility.CopySerialized(source, persistent);
                            EditorUtility.SetDirty(persistent);
                        }

                        materialCache[source] = persistent;
                    }

                    slots[slot] = persistent;
                    changed = true;
                }

                if (!changed) continue;
                renderers[r].sharedMaterials = slots;
                EditorUtility.SetDirty(renderers[r]);
            }
        }

        private static void StripNonVisualComponents(GameObject root)
        {
            foreach (Collider component in root.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(component);

            foreach (Rigidbody component in root.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.DestroyImmediate(component);

            foreach (Animator component in root.GetComponentsInChildren<Animator>(true))
                UnityEngine.Object.DestroyImmediate(component);

            foreach (Animation component in root.GetComponentsInChildren<Animation>(true))
                UnityEngine.Object.DestroyImmediate(component);

            foreach (AudioSource component in root.GetComponentsInChildren<AudioSource>(true))
                UnityEngine.Object.DestroyImmediate(component);

            // The source layout's lights already exist as separate saved HouseLayout_* objects.
            // Do not duplicate a light if one ever appears inside a furniture prefab.
            foreach (Light component in root.GetComponentsInChildren<Light>(true))
                UnityEngine.Object.DestroyImmediate(component);

            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                UnityEngine.Object.DestroyImmediate(component);
        }

        private static void SetHideFlagsRecursive(GameObject root, HideFlags flags)
        {
            root.hideFlags = flags;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = flags;
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
            if (string.IsNullOrEmpty(scene.path) || !File.Exists(scene.path))
                return;

            string directory = Path.Combine("Library", "CEVRBackups");
            Directory.CreateDirectory(directory);
            string destination = Path.Combine(
                directory,
                "House_before_color_restore_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
            File.Copy(scene.path, destination, true);
            Debug.Log("CEVR House restore backup created: " + destination);
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
