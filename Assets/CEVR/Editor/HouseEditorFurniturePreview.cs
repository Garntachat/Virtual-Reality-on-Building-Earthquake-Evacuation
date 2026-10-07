using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>
    /// Edit-mode wrapper around HouseLayoutSharedBuilder.
    ///
    /// IMPORTANT: there is no editor-specific furniture layout anymore. This script only creates a
    /// preview root and asks the SAME runtime builder to populate it.
    /// </summary>
    [InitializeOnLoad]
    public static class HouseEditorFurniturePreview
    {
        private const string RootName = "CEVR_HouseEditorFurniturePreview";
        private static bool rebuilding;
        private static double nextEnsureTime;

        static HouseEditorFurniturePreview()
        {
            InstallHooks();
        }

        [InitializeOnLoadMethod]
        private static void InitializeAgain()
        {
            InstallHooks();
        }

        [MenuItem("CEVR/House/Refresh Exact Play Layout", priority = 20)]
        public static void RefreshMenu()
        {
            RefreshIfHouse();
        }

        [MenuItem("CEVR/House/Remove Editor Furniture Preview", priority = 21)]
        public static void RemoveMenu()
        {
            RemovePreview();
            SceneView.RepaintAll();
        }

        private static void InstallHooks()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorApplication.projectChanged -= ScheduleRefresh;
            EditorApplication.projectChanged += ScheduleRefresh;

            EditorApplication.update -= EnsurePreview;
            EditorApplication.update += EnsurePreview;

            ScheduleRefresh();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ScheduleRefresh();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                GameObject preview = FindPreviewRoot(SceneManager.GetActiveScene());
                if (preview != null) preview.SetActive(false);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                ScheduleRefresh();
            }
        }

        private static void ScheduleRefresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            nextEnsureTime = 0d;
            EditorApplication.delayCall -= RefreshIfHouse;
            EditorApplication.delayCall += RefreshIfHouse;
        }

        private static void EnsurePreview()
        {
            if (rebuilding || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.timeSinceStartup < nextEnsureTime) return;
            nextEnsureTime = EditorApplication.timeSinceStartup + 1d;

            Scene scene = SceneManager.GetActiveScene();
            if (!IsHouse(scene)) return;

            GameObject preview = FindPreviewRoot(scene);
            if (preview == null) RefreshIfHouse();
            else if (!preview.activeSelf) preview.SetActive(true);
        }

        private static void RefreshIfHouse()
        {
            if (rebuilding || EditorApplication.isPlayingOrWillChangePlaymode) return;

            Scene scene = SceneManager.GetActiveScene();
            if (!IsHouse(scene))
            {
                RemovePreview();
                return;
            }

            rebuilding = true;
            try
            {
                RemovePreview();

                GameObject root = new GameObject(RootName);
                root.tag = "EditorOnly";
                root.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                var context = new HouseLayoutSharedBuilder.BuildContext
                {
                    scene = scene,
                    root = root.transform,
                    runtime = false,
                    groundMotion = null,
                    logger = null
                };
                HouseLayoutSharedBuilder.Build(context);

                bool valid = HouseLayoutSharedBuilder.ValidateBuiltLayout(scene, root.transform, out string report);
                if (valid)
                    Debug.Log("CEVR HOUSE EDITOR LAYOUT VALIDATION PASS.");
                else
                    Debug.LogError("CEVR HOUSE EDITOR LAYOUT VALIDATION FAILED: " + report);

                // ApplyAuthoredState changes the real House placeholders so that the Edit scene
                // itself visually matches Play. The preview root remains non-saved/EditorOnly.
                EditorSceneManager.MarkSceneDirty(scene);
                SceneView.RepaintAll();
                EditorApplication.RepaintHierarchyWindow();

                Debug.Log("CEVR HOUSE EDITOR = PLAY LAYOUT READY: both modes were built by HouseLayoutSharedBuilder.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                rebuilding = false;
            }
        }

        private static bool IsHouse(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded &&
                   scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static GameObject FindPreviewRoot(Scene scene)
        {
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == RootName) return root;
            return null;
        }

        private static void RemovePreview()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            GameObject existing = FindPreviewRoot(scene);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing);
        }
    }
}
