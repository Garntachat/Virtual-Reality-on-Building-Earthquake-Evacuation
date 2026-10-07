using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>Captures the settled initial layout and persists generated meshes/materials.</summary>
    [InitializeOnLoad]
    public static class HouseFurnitureCapture
    {
        private const string Pending = "CEVR.FurnitureCapture.Pending";
        private const string SceneKey = "CEVR.FurnitureCapture.Scene";
        private const string PrefabKey = "CEVR.FurnitureCapture.Prefab";
        private static int frames;

        static HouseFurnitureCapture()
        {
            EditorApplication.playModeStateChanged += StateChanged;
        }

        [MenuItem("CEVR/House/Save editable furniture into scene")]
        public static void Begin()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0)
            {
                EditorUtility.DisplayDialog("House furniture", "Stop Play Mode and open House.unity first.", "OK");
                return;
            }
            if (Object.FindFirstObjectByType<HouseAuthoredFurniture>() != null)
            {
                EditorUtility.DisplayDialog("Furniture already saved", "Edit the objects under House_EditableFurniture and save the scene. Existing edits will not be regenerated.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (string.IsNullOrEmpty(scene.path)) return;
            SessionState.SetString(SceneKey, scene.path);
            SessionState.SetString(PrefabKey, "");
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void StateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                frames = 0;
                EditorApplication.update -= CaptureWhenReady;
                EditorApplication.update += CaptureWhenReady;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update -= CaptureWhenReady;
                SessionState.SetBool(Pending, false);
                string path = SessionState.GetString(PrefabKey, "");
                if (string.IsNullOrEmpty(path)) return;
                Scene scene = SceneManager.GetActiveScene();
                if (scene.path != SessionState.GetString(SceneKey, "")) return;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) return;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Undo.RegisterCreatedObjectUndo(instance, "Create editable House furniture");
                Selection.activeGameObject = instance;
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("House furniture is now editable outside Play Mode. Save the scene with Ctrl/Cmd+S.");
            }
        }

        private static void CaptureWhenReady()
        {
            if (!EditorApplication.isPlaying) return;
            if (++frames < 12) return;
            EditorApplication.update -= CaptureWhenReady;
            try { Capture(); }
            catch (Exception error) { Debug.LogException(error); }
            finally { EditorApplication.isPlaying = false; }
        }

        private static bool IsAnchor(string name)
        {
            return name.StartsWith("HouseChair_", StringComparison.Ordinal) ||
                name.StartsWith("HouseFallingObject_", StringComparison.Ordinal) ||
                name == "HouseTallCabinet_Left" || name == "HouseBookcase_Right" ||
                name == "HouseSturdyTableTop" || name == "HouseSturdyTableLeg";
        }

        private static void Capture()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != SessionState.GetString(SceneKey, "")) throw new InvalidOperationException("House changed during capture.");
            GroundMotionPlayer motion = Object.FindFirstObjectByType<GroundMotionPlayer>();
            if (motion != null && motion.IsPlaying) throw new InvalidOperationException("Capture must happen before the earthquake starts.");
            Transform[] objects = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.gameObject.scene == scene).OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToArray();
            var candidates = new HashSet<Transform>(objects.Where(t => IsAnchor(t.name) ||
                t.name.StartsWith("HouseFinal_", StringComparison.Ordinal) ||
                t.name.StartsWith("HouseDecor_", StringComparison.Ordinal) || t.name == "HouseWindow_REAL_Fitted"));
            Transform[] roots = candidates.Where(t => !Ancestors(t).Any(candidates.Contains)).ToArray();
            if (!roots.Any(t => t.name.StartsWith("HouseFinal_", StringComparison.Ordinal)))
                throw new InvalidOperationException("House furniture did not initialize; nothing was saved.");
            var root = new GameObject("House_EditableFurniture");
            root.SetActive(false); // Cloned gameplay scripts must not run during the capture.
            var bindings = new List<HouseAuthoredFurniture.Binding>();
            string folder = "Assets/CEVR/Generated/EditableHouse_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            AssetDatabase.CreateFolder("Assets/CEVR/Generated", folder.Substring(folder.LastIndexOf('/') + 1));
            var assets = new Dictionary<Object, Object>();
            try
            {
                foreach (Transform original in roots)
                {
                    GameObject copy = Object.Instantiate(original.gameObject, root.transform, false);
                    copy.name = original.name;
                    copy.transform.SetPositionAndRotation(original.position, original.rotation);
                    copy.transform.localScale = original.lossyScale;
                    foreach (MonoBehaviour script in copy.GetComponentsInChildren<MonoBehaviour>(true))
                        if (!(script is WindowView) && !(script is BreakableWindow)) Object.DestroyImmediate(script);
                    foreach (Joint joint in copy.GetComponentsInChildren<Joint>(true)) Object.DestroyImmediate(joint);
                    foreach (Rigidbody body in copy.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
                    foreach (MeshFilter filter in copy.GetComponentsInChildren<MeshFilter>(true))
                        filter.sharedMesh = Persist(filter.sharedMesh, folder, assets);
                    foreach (MeshCollider collider in copy.GetComponentsInChildren<MeshCollider>(true))
                        collider.sharedMesh = Persist(collider.sharedMesh, folder, assets);
                    foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(true))
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(m => Persist(m, folder, assets)).ToArray();
                    if (IsAnchor(original.name)) bindings.Add(new HouseAuthoredFurniture.Binding {
                        furnishing = copy.transform, anchorName = original.name,
                        anchorIndex = Array.IndexOf(objects.Where(t => t.name == original.name).ToArray(), original)
                    });
                }
                root.AddComponent<HouseAuthoredFurniture>().bindings = bindings.ToArray();
                string prefabPath = folder + "/HouseFurniture.prefab";
                // Save enabled state without running scripts in the live Play Mode scene.
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                contents.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                PrefabUtility.UnloadPrefabContents(contents);
                AssetDatabase.SaveAssets();
                SessionState.SetString(PrefabKey, prefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static IEnumerable<Transform> Ancestors(Transform t)
        {
            for (t = t.parent; t != null; t = t.parent) yield return t;
        }

        private static T Persist<T>(T value, string folder, Dictionary<Object, Object> saved) where T : Object
        {
            if (value == null || AssetDatabase.Contains(value)) return value;
            if (saved.TryGetValue(value, out Object existing)) return (T)existing;
            T clone = Object.Instantiate(value);
            AssetDatabase.CreateAsset(clone, folder + "/Asset_" + saved.Count + ".asset");
            saved[value] = clone;
            return clone;
        }
    }
}
