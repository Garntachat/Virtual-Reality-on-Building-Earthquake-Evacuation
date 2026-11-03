using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>
    /// Makes already-authored House/New_House physics objects editable BEFORE
    /// Play. No meshes, chairs, transforms, masses or damping values are replaced.
    /// Tutorial and Condo have their own one-time visual bakers; this tool can
    /// still be run manually on them if additional custom Rigidbodies are added.
    /// </summary>
    [InitializeOnLoad]
    public static class EditableRigidbodyEarthquakeSetup
    {
        private static bool queued;
        private static bool running;

        static EditableRigidbodyEarthquakeSetup()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            Queue();
        }

        [MenuItem("CEVR/Earthquake/Add simple physics settings to scene")]
        private static void ApplyMenu() => Apply(false);

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => Queue();

        private static void Queue()
        {
            if (queued) return;
            queued = true;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= OnEditorUpdate;
            queued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;
            string name = SceneManager.GetActiveScene().name;
            if (name == "House" || name == "New_House") Apply(true);
        }

        private static bool IsEligible(Rigidbody body)
        {
            if (body == null || body.GetComponent<InertialRigidbody>() != null) return false;
            if (body.GetComponentInParent<PlayerHealth>() != null ||
                body.GetComponentInParent<Camera>() != null ||
                body.GetComponentInParent<Canvas>() != null) return false;
            if (body.isKinematic && body.GetComponent<FallingHazard>() == null &&
                body.GetComponent<MovableFurniture>() == null) return false;
            string name = body.name.ToLowerInvariant();
            if (name.Contains("wall") || name.Contains("floor") || name.Contains("ground") ||
                name.Contains("foundation") || name.Contains("assembly")) return false;
            return true;
        }

        private static void Apply(bool automatic)
        {
            if (running || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path) ||
                SceneManager.sceneCount != 1) return;
            if (!scene.path.StartsWith("Assets/CEVR/Generated/Scenes/", StringComparison.Ordinal))
                return;

            var candidates = new System.Collections.Generic.List<Rigidbody>();
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
                    if (IsEligible(body)) candidates.Add(body);
            if (candidates.Count == 0)
            {
                if (!automatic) Debug.Log("CEVR: all eligible Rigidbodies already have simple earthquake settings.");
                return;
            }

            running = true;
            string backup = "";
            try
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                backup = AssetDatabase.GenerateUniqueAssetPath(
                    scene.path.Replace(".unity", "_BeforeSimplePhysics_" + stamp + ".unity"));
                if (!EditorSceneManager.SaveScene(scene, backup, true))
                    throw new InvalidOperationException("Could not back up the current scene.");

                int count = 0;
                foreach (Rigidbody body in candidates)
                {
                    if (body == null || !IsEligible(body)) continue;
                    InertialRigidbody settings = Undo.AddComponent<InertialRigidbody>(body.gameObject);
                    settings.Configure(null, 1f, body.mass < 12f &&
                        (body.constraints & RigidbodyConstraints.FreezePositionY) == 0);
                    count++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save Rigidbody settings in the current scene.");
                Debug.Log($"CEVR: added one simple earthquake component to {count} saved Rigidbodies " +
                          $"without changing Mass or Damping. Backup: {backup}");
                if (!automatic)
                    EditorUtility.DisplayDialog("Simple Rigidbody earthquake ready",
                        $"Added settings to {count} Rigidbody objects. They are editable in the " +
                        "Inspector outside Play mode. Mass, both Damping values, " +
                        "Force and Torque are all adjustable.\nBackup: " + backup, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("CEVR: scene setup did not finish. Reopen the original without saving. Backup: " + backup);
            }
            finally { running = false; }
        }
    }
}
