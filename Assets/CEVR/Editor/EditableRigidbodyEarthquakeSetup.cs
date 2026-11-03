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
        private const string MenuScenePath = "Assets/CEVR/Generated/Scenes/CEVR_MainMenu.unity";
        private const string DirectTestKey = "CEVR.SimplePhysics.DirectPlay";
        private static bool queued;
        private static bool running;
        private const string SelectPhysicsKey = "CEVR.Earthquake.SelectChairPhysicsRoot";
        private static readonly string[] HouseChairs =
        {
            "HouseLayout_DiningChair_CoverObstacle",
            "HouseLayout_DiningChair_Left",
            "HouseLayout_DiningChair_Right",
            "HouseLayout_DiningChair_Spare"
        };

        static EditableRigidbodyEarthquakeSetup()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged -= RestoreMenuAfterTest;
            EditorApplication.playModeStateChanged += RestoreMenuAfterTest;
            Selection.selectionChanged -= OnFurniturePartSelected;
            Selection.selectionChanged += OnFurniturePartSelected;
            Queue();
        }

        [MenuItem("CEVR/Earthquake/Add simple physics settings to scene")]
        private static void ApplyMenu() => Apply(false);

        [MenuItem("CEVR/Earthquake/Select furniture earthquake controls from mesh parts")]
        private static void ToggleChairSelection()
        {
            EditorPrefs.SetBool(SelectPhysicsKey, !EditorPrefs.GetBool(SelectPhysicsKey, true));
        }

        [MenuItem("CEVR/Earthquake/Select furniture earthquake controls from mesh parts", true)]
        private static bool ValidateChairSelection()
        {
            Menu.SetChecked("CEVR/Earthquake/Select furniture earthquake controls from mesh parts",
                EditorPrefs.GetBool(SelectPhysicsKey, true));
            return true;
        }

        private static bool IsHouseChair(Transform target)
        {
            if (target == null) return false;
            foreach (string name in HouseChairs)
                if (target.name == name) return true;
            return target.name == "EDITOR_DiningChair";
        }

        private static void OnFurniturePartSelected()
        {
            if (running || EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorPrefs.GetBool(SelectPhysicsKey, true)) return;
            GameObject selected = Selection.activeGameObject;
            if (selected == null || !selected.scene.IsValid()) return;

            for (Transform candidate = selected.transform.parent; candidate != null;
                 candidate = candidate.parent)
            {
                if (candidate.GetComponent<InertialRigidbody>() == null &&
                    candidate.GetComponent<HouseProxyEarthquakeSettings>() == null &&
                    candidate.GetComponent<DecorativeQuakeResponse>() == null) continue;
                GameObject physicsRoot = candidate.gameObject;
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                        selected != null && physicsRoot != null &&
                        Selection.activeGameObject == selected)
                        Selection.activeGameObject = physicsRoot;
                };
                break;
            }
        }

        [MenuItem("CEVR/Earthquake/Check House chair physics")]
        private static void CheckHouseChairPhysics()
        {
            if (SceneManager.GetActiveScene().name != "House")
            {
                EditorUtility.DisplayDialog("House scene", "Open House.unity in Edit mode.", "OK");
                return;
            }
            int ready = 0;
            foreach (Transform chair in FindHouseChairRoots(SceneManager.GetActiveScene()))
            {
                bool correct = chair.GetComponent<Rigidbody>() != null &&
                    chair.GetComponent<InertialRigidbody>() != null &&
                    chair.GetComponent<BoxCollider>() != null;
                if (!correct) Debug.LogError("CEVR House chair needs physics setup: " + chair.name, chair);
                else ready++;
            }
            EditorUtility.DisplayDialog("House chair physics",
                ready + "/4 named House chairs have saved Rigidbody, InertialRigidbody and BoxCollider. " +
                (ready == 4 ? "Click a chair Seat and the Inspector will open its physics root." :
                    "Run Add simple physics settings to scene to repair missing components."),
                "OK");
        }

        [MenuItem("CEVR/Earthquake/Check all House furniture earthquake controls")]
        private static void CheckAllHouseFurniture()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "House")
            {
                EditorUtility.DisplayDialog("House scene required", "Open House.unity in Edit mode.", "OK");
                return;
            }
            int ready = 0, expected = 0;
            foreach (Transform chair in FindHouseChairRoots(scene))
            {
                expected++;
                if (chair.GetComponent<Rigidbody>() != null &&
                    chair.GetComponent<InertialRigidbody>() != null &&
                    chair.GetComponent<BoxCollider>() != null) ready++;
                else Debug.LogError("CEVR missing House chair physics: " + chair.name, chair);
            }
            foreach (Transform item in FindOtherHouseFurniture(scene))
            {
                expected++;
                bool present = IsHouseProxy(item)
                    ? item.GetComponent<HouseProxyEarthquakeSettings>() != null
                    : item.GetComponent<DecorativeQuakeResponse>() != null ||
                      item.GetComponent<InertialRigidbody>() != null;
                if (present) ready++;
                else Debug.LogError("CEVR missing House furniture earthquake settings: " + item.name, item);
            }
            EditorUtility.DisplayDialog("House earthquake controls",
                ready + "/" + expected + " furniture roots have visible earthquake controls. " +
                "Doors, windows and the animated cat remain under their own gameplay systems. " +
                (ready == expected ? "Click any furniture part to edit its parent settings."
                    : "Run Add simple physics settings to scene and check again."),
                "OK");
        }

        [MenuItem("CEVR/Earthquake/Test current scene (not Main Menu)")]
        private static void TestCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(DirectTestKey, true);
            EditorSceneManager.playModeStartScene = null;
            EditorApplication.isPlaying = true;
        }

        private static void RestoreMenuAfterTest(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode ||
                !SessionState.GetBool(DirectTestKey, false)) return;
            SessionState.EraseBool(DirectTestKey);
            SceneAsset menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
            if (menu != null) EditorSceneManager.playModeStartScene = menu;
        }

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

        private static System.Collections.Generic.List<Transform> FindHouseChairRoots(Scene scene)
        {
            var chairs = new System.Collections.Generic.List<Transform>();
            if (scene.name != "House") return chairs;
            Transform furniture = HouseSavedSceneRuntimeBinder.FindSavedFurnitureRoot(scene);
            if (furniture == null) return chairs;
            foreach (Transform candidate in furniture.GetComponentsInChildren<Transform>(true))
                if (IsHouseChair(candidate)) chairs.Add(candidate);
            return chairs;
        }

        private static bool IsHouseProxy(Transform item)
        {
            return item.name == "HouseLayout_Fridge" ||
                item.name == "HouseLayout_Wandrobe" ||
                item.name.StartsWith("HouseLayout_HazardBooks_", StringComparison.Ordinal);
        }

        private static bool IsFixedHouseStructure(Transform item)
        {
            // Do NOT turn building structure, camera, door, window or pet
            // controllers into physical furniture. They have their own behavior.
            string name = item.name.ToLowerInvariant();
            return name == "houselayout_cat" || name.Contains("window") ||
                name.Contains("door") || name.Contains("wall") ||
                name.Contains("floor") || name.Contains("ceiling") ||
                name.Contains("ground") || name.Contains("camera") ||
                name.Contains("player") || name.Contains("assembly") ||
                name.Contains("safetyzone") || name.Contains("foundation");
        }

        private static System.Collections.Generic.List<Transform> FindOtherHouseFurniture(Scene scene)
        {
            var items = new System.Collections.Generic.List<Transform>();
            if (scene.name != "House") return items;
            Transform root = HouseSavedSceneRuntimeBinder.FindSavedFurnitureRoot(scene);
            if (root == null) return items;
            foreach (Transform item in root)
            {
                if (IsHouseChair(item) || IsFixedHouseStructure(item)) continue;
                // HouseLayout_* is the original scene convention, but also
                // include user-added custom furniture under HouseFurniture.
                if (!item.name.StartsWith("HouseLayout_", StringComparison.Ordinal) &&
                    item.GetComponentInChildren<Renderer>(true) == null &&
                    item.GetComponent<Rigidbody>() == null) continue;
                items.Add(item);
            }
            return items;
        }

        private static bool IsFixedDecoration(string name)
        {
            return name.IndexOf("rug", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("table", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("sofa", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("bed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("kitchen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("cabinet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("bookcase", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("sink", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("stove", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int AddOtherHouseFurnitureSettings(
            System.Collections.Generic.List<Transform> items)
        {
            int added = 0;
            foreach (Transform item in items)
            {
                if (item == null) continue;
                if (IsHouseProxy(item))
                {
                    if (item.GetComponent<HouseProxyEarthquakeSettings>() != null) continue;
                    var proxySettings = Undo.AddComponent<HouseProxyEarthquakeSettings>(item.gameObject);
                    proxySettings.SetDefaultsForNewComponent(
                        item.name.StartsWith("HouseLayout_HazardBooks_", StringComparison.Ordinal));
                    added++;
                }
                else if (item.GetComponent<Rigidbody>() == null &&
                         item.GetComponent<DecorativeQuakeResponse>() == null)
                {
                    // Static interior decor moves only visually. It never gets a
                    // fake Rigidbody or competes with an authored floor collider.
                    DecorativeQuakeMode mode = item.name.IndexOf("lamp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                               item.name.IndexOf("light", StringComparison.OrdinalIgnoreCase) >= 0
                        ? DecorativeQuakeMode.Hanging
                        : IsFixedDecoration(item.name) ? DecorativeQuakeMode.Heavy
                        : DecorativeQuakeMode.Loose;
                    float strength = IsFixedDecoration(item.name) ? 0f : 1f;
                    Undo.AddComponent<DecorativeQuakeResponse>(item.gameObject)
                        .Configure(null, mode, strength);
                    added++;
                }
            }
            return added;
        }

        private static bool TryLocalFurnitureBounds(Transform root, out Bounds local)
        {
            local = default;
            if (!HouseFurnitureGeometry.TryVisibleBounds(root, out Bounds world)) return false;
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            foreach (float x in new[] {world.min.x, world.max.x})
            foreach (float y in new[] {world.min.y, world.max.y})
            foreach (float z in new[] {world.min.z, world.max.z})
            {
                Vector3 corner = root.InverseTransformPoint(new Vector3(x, y, z));
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
            }
            local = new Bounds((min + max) * 0.5f, max - min);
            return true;
        }

        private static int AddMissingHouseChairComponents(
            System.Collections.Generic.List<Transform> chairs)
        {
            int added = 0;
            foreach (Transform chair in chairs)
            {
                if (chair == null) continue;
                Rigidbody body = chair.GetComponent<Rigidbody>();
                if (body == null)
                {
                    body = Undo.AddComponent<Rigidbody>(chair.gameObject);
                    body.mass = 7.5f;
                    body.linearDamping = 2.5f;
                    body.angularDamping = 4f;
                    body.useGravity = false;
                    body.interpolation = RigidbodyInterpolation.Interpolate;
                    body.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    body.constraints = RigidbodyConstraints.FreezePositionY |
                                       RigidbodyConstraints.FreezeRotationX |
                                       RigidbodyConstraints.FreezeRotationZ;
                    added++;
                }

                if (chair.GetComponent<BoxCollider>() == null)
                {
                    BoxCollider box = Undo.AddComponent<BoxCollider>(chair.gameObject);
                    if (TryLocalFurnitureBounds(chair, out Bounds local))
                    {
                        box.center = local.center;
                        box.size = new Vector3(Mathf.Max(0.30f, local.size.x),
                            Mathf.Max(0.55f, local.size.y),
                            Mathf.Max(0.30f, local.size.z));
                    }
                    else
                    {
                        box.center = new Vector3(0f, 0.5f, 0f);
                        box.size = new Vector3(0.7f, 1f, 0.7f);
                        Debug.LogWarning("CEVR: chair mesh bounds unavailable; adjust BoxCollider manually.", chair);
                    }
                    added++;
                }

                if (chair.GetComponent<InertialRigidbody>() == null)
                {
                    Undo.AddComponent<InertialRigidbody>(chair.gameObject)
                        .Configure(null, 0.65f, false);
                    added++;
                }
            }
            return added;
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

            var chairs = FindHouseChairRoots(scene);
            bool missingChairPhysics = false;
            foreach (Transform chair in chairs)
            {
                if (chair.GetComponent<Rigidbody>() == null ||
                    chair.GetComponent<BoxCollider>() == null ||
                    chair.GetComponent<InertialRigidbody>() == null)
                {
                    missingChairPhysics = true;
                    break;
                }
            }
            var otherHouseFurniture = FindOtherHouseFurniture(scene);
            bool missingHouseFurniture = false;
            foreach (Transform item in otherHouseFurniture)
            {
                if (IsHouseProxy(item)
                    ? item.GetComponent<HouseProxyEarthquakeSettings>() == null
                    : item.GetComponent<DecorativeQuakeResponse>() == null &&
                      item.GetComponent<InertialRigidbody>() == null)
                {
                    missingHouseFurniture = true;
                    break;
                }
            }
            var candidates = new System.Collections.Generic.List<Rigidbody>();
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
                    if (IsEligible(body)) candidates.Add(body);
            if (candidates.Count == 0 && !missingChairPhysics && !missingHouseFurniture)
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

                int count = AddMissingHouseChairComponents(chairs);
                count += AddOtherHouseFurnitureSettings(otherHouseFurniture);
                foreach (Rigidbody body in candidates)
                {
                    if (body == null || !IsEligible(body)) continue;
                    InertialRigidbody settings = Undo.AddComponent<InertialRigidbody>(body.gameObject);
                    settings.Configure(null, IsHouseChair(body.transform) ? 0.65f : 1f,
                        body.mass < 12f &&
                        (body.constraints & RigidbodyConstraints.FreezePositionY) == 0);
                    count++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save Rigidbody settings in the current scene.");
                Debug.Log($"CEVR: prepared {count} saved Rigidbody/Collider/Shake components. " +
                          $"Existing Mass, Damping and Shake Strength were preserved. Backup: {backup}");
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
