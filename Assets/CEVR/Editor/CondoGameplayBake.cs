using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    // Prepares the existing Condo scene once. It never regenerates a layout
    // after a designer has moved the saved furniture or adjusted its mass.
    [InitializeOnLoad]
    public static class CondoGameplayBake
    {
        private const string ScenePath = "Assets/CEVR/Generated/Scenes/Condo.unity";
        private const string Generated = "Assets/CEVR/Generated";
        private static bool queued;
        private static bool busy;

        static CondoGameplayBake()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            Queue();
        }

        [MenuItem("CEVR/Condo/Prepare editable gameplay (once)")]
        private static void RunMenu() => Build(false);

        [MenuItem("CEVR/Condo/Select gameplay objects")]
        private static void Select()
        {
            CondoGameplayLayout current = UnityEngine.Object.FindFirstObjectByType<CondoGameplayLayout>();
            if (current != null) Selection.activeGameObject = current.gameObject;
            else EditorUtility.DisplayDialog("Condo gameplay",
                "Open the Condo scene and run Prepare editable gameplay (once).", "OK");
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => Queue();

        private static void Queue()
        {
            if (queued) return;
            queued = true;
            EditorApplication.update += TryOnEditorUpdate;
        }

        private static void TryOnEditorUpdate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= TryOnEditorUpdate;
            queued = false;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !Application.isBatchMode)
                Build(true);
        }

        private static void Build(bool automatic)
        {
            if (busy || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (SceneManager.sceneCount != 1 || scene.path != ScenePath)
            {
                if (!automatic) EditorUtility.DisplayDialog("Condo scene required",
                    "Open only " + ScenePath + " in Edit mode.", "OK");
                return;
            }

            CondoGameplayLayout existing = UnityEngine.Object.FindFirstObjectByType<CondoGameplayLayout>();
            if (existing != null)
            {
                if (!existing.IsPrepared)
                    Debug.LogWarning("CEVR Condo: an incomplete authored gameplay root exists. " +
                                     "It will not be overwritten automatically.", existing);
                if (!automatic) Selection.activeGameObject = existing.gameObject;
                return;
            }
            // Do not add a second gameplay hierarchy if the first run stopped.
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Condo Gameplay")
                {
                    Debug.LogError("CEVR Condo: partial layout exists; refusing to create duplicates.");
                    return;
                }

            Material dark = AssetDatabase.LoadAssetAtPath<Material>(Generated + "/Materials/BenchDark.mat");
            Material pink = AssetDatabase.LoadAssetAtPath<Material>(Generated + "/Materials/ChulaPinkAccent.mat");
            Material green = AssetDatabase.LoadAssetAtPath<Material>(Generated + "/Materials/SafetyGreen.mat");
            Material amber = AssetDatabase.LoadAssetAtPath<Material>(Generated + "/Materials/HazardYellow.mat");
            if (dark == null || pink == null || green == null || amber == null)
            {
                Debug.LogError("CEVR Condo: project materials are missing; gameplay was not baked.");
                return;
            }

            busy = true;
            string backup = "";
            try
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                backup = AssetDatabase.GenerateUniqueAssetPath(
                    ScenePath.Replace(".unity", "_BeforeCondoGameplay_" + stamp + ".unity"));
                if (!EditorSceneManager.SaveScene(scene, backup, true))
                    throw new InvalidOperationException("Could not back up the Condo scene.");

                string folderName = "CondoBake_" + stamp;
                string folder = Generated + "/" + folderName;
                if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(Generated, folderName)))
                    throw new InvalidOperationException("Could not create generated asset folder.");

                GameObject root = new GameObject("Condo Gameplay");
                var layout = root.AddComponent<CondoGameplayLayout>();
                layout.Build(dark, pink, green, amber);
                if (!layout.IsPrepared)
                    throw new InvalidOperationException("Condo gameplay objects were not created.");

                // Match the detailed Play-mode safety-shoe meshes before saving.
                foreach (WearableShoes shoe in root.GetComponentsInChildren<WearableShoes>(true))
                    shoe.PrepareEditableVisual();

                var saved = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
                foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                    filter.sharedMesh = Persist(filter.sharedMesh, folder, saved);
                foreach (MeshCollider collider in root.GetComponentsInChildren<MeshCollider>(true))
                    collider.sharedMesh = Persist(collider.sharedMesh, folder, saved);
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                        materials[i] = Persist(materials[i], folder, saved);
                    renderer.sharedMaterials = materials;
                }

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity could not save Condo.unity.");

                Selection.activeGameObject = root;
                SceneView.RepaintAll();
                Debug.Log("CEVR Condo gameplay saved. Edit the objects under 'Condo Gameplay' " +
                          "before Play; original Condo mesh and prefabs remain untouched. Backup: " + backup);
                if (!automatic)
                    EditorUtility.DisplayDialog("Condo gameplay ready",
                        "Condo gameplay is saved into the scene. Edit the furniture roots " +
                        "and use Inspector for Mass and Shake Strength. Play will preserve " +
                        "the layout without duplicates.\n\nBackup: " + backup, "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("CEVR Condo bake failed. Do not save the incomplete scene. " +
                               "Reopen the original or backup: " + backup);
                if (!automatic)
                    EditorUtility.DisplayDialog("Condo setup failed",
                        "Do not save the incomplete scene. Check the Console and reopen the " +
                        "original or backup: " + backup, "OK");
            }
            finally
            {
                busy = false;
            }
        }

        private static T Persist<T>(T source, string folder,
            Dictionary<UnityEngine.Object, UnityEngine.Object> saved) where T : UnityEngine.Object
        {
            if (source == null || EditorUtility.IsPersistent(source)) return source;
            if (saved.TryGetValue(source, out UnityEngine.Object existing)) return (T)existing;
            T copy = UnityEngine.Object.Instantiate(source);
            copy.name = source.name;
            string name = source.name.Replace("/", "_").Replace("\\", "_");
            string extension = source is Material ? ".mat" : ".asset";
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + extension);
            AssetDatabase.CreateAsset(copy, path);
            saved.Add(source, copy);
            return copy;
        }
    }
}
