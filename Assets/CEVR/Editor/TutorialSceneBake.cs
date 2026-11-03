using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>
    /// Turns the same tutorial dressing used by Play mode into saved scene objects.
    /// This deliberately does not regenerate an already baked scene: designers own
    /// furniture positions, rotations, scales and colliders after the first bake.
    /// </summary>
    public static class TutorialSceneBake
    {
        public const string ScenePath = "Assets/CEVR/Generated/Scenes/CEVR_ChulaEngineering_Tutorial.unity";
        private const string GeneratedRoot = "Assets/CEVR/Generated";
        private static bool isBaking;

        [MenuItem("CEVR/Tutorial/Make furniture editable (one time)")]
        public static void Bake() => BakeInternal(false);

        [MenuItem("CEVR/Tutorial/Check saved editable furniture")]
        private static void Check()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Tutorial scene required", "Open " + ScenePath, "OK");
                return;
            }

            FurnitureSceneDressing dressing = FindSavedDressing(scene);
            string message = dressing == null || !dressing.SavedInEditor
                ? "The furniture has not been saved to the scene. Run CEVR > Tutorial > Make furniture editable (one time)."
                : "Saved dressing found. " + CountActiveChairs() + " movable chairs, " +
                  dressing.GetComponentsInChildren<Renderer>(true).Length +
                  " furniture renderers. Move the parent objects, save and test Play mode.";
            EditorUtility.DisplayDialog("Editable Tutorial Status", message, "OK");
        }

        internal static void BakeOnSceneOpen() => BakeInternal(true);

        private static FurnitureSceneDressing FindSavedDressing(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                FurnitureSceneDressing dressing = root.GetComponentInChildren<FurnitureSceneDressing>(true);
                if (dressing != null) return dressing;
            }
            return null;
        }

        private static int CountActiveChairs()
        {
            int count = 0;
            foreach (MovableFurniture chair in UnityEngine.Object.FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None))
                if (chair.gameObject.activeInHierarchy) count++;
            return count;
        }

        private static void BakeInternal(bool automatic)
        {
            if (isBaking || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || SceneManager.sceneCount != 1)
            {
                if (!automatic)
                    EditorUtility.DisplayDialog("Open only the tutorial",
                        "Close other scenes and open " + ScenePath, "OK");
                return;
            }

            FurnitureSceneDressing already = FindSavedDressing(scene);
            if (already != null)
            {
                if (!automatic)
                {
                    Selection.activeGameObject = already.gameObject;
                    EditorUtility.DisplayDialog("Furniture already present",
                        already.SavedInEditor
                            ? "The saved furniture is already editable. Your changes will not be overwritten."
                            : "A partial or unbaked dressing already exists. Inspect it before attempting another bake.",
                        "OK");
                }
                return;
            }

            // A partially prepared scene must not be silently rebuilt or duplicated.
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Tutorial Editable Props" || root.name == "Tutorial Editable Furniture")
                {
                    Debug.LogError("CEVR: Partial tutorial bake detected. Reopen the original scene without saving, then retry.");
                    return;
                }
            }

            // Scene-opening material repair often marks the scene dirty.
            // Back up the actual in-memory scene below instead of skipping the
            // first bake; the backup preserves unsaved artist edits too.
            isBaking = true;
            string backup = "";
            try
            {
                // This is an actual asset backup, taken before changing a single scene object.
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                backup = AssetDatabase.GenerateUniqueAssetPath(
                    ScenePath.Replace(".unity", "_BeforeEditableBake_" + stamp + ".unity"));
                if (!EditorSceneManager.SaveScene(scene, backup, true))
                    throw new InvalidOperationException("Unable to back up the complete in-memory tutorial scene. No changes were made.");

                string folder = AssetDatabase.GenerateUniqueAssetPath(
                    GeneratedRoot + "/TutorialBake_" + stamp);
                string folderName = folder.Substring(GeneratedRoot.Length + 1);
                if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(GeneratedRoot, folderName)))
                    throw new InvalidOperationException("Unable to create the baked asset folder.");

                EditorUtility.DisplayProgressBar("CEVR editable tutorial", "Preparing the same visuals as Play mode", 0.15f);
                RuntimeStageRepair.EnsurePlayableStage();

                // Keep runtime-only controllers separate; only their physical props belong in the scene.
                var props = new GameObject("Tutorial Editable Props");
                props.AddComponent<UniversalSceneGameplayBootstrap>().PrepareTutorialProps();
                foreach (WearableShoes shoes in
                    UnityEngine.Object.FindObjectsByType<WearableShoes>(FindObjectsSortMode.None))
                    shoes.PrepareEditableVisual();

                var furniture = new GameObject("Tutorial Editable Furniture").AddComponent<FurnitureSceneDressing>();
                furniture.BakeForEditing();

                // Bind every physics prop and non-physics furniture decoration NOW,
                // in Edit mode. This exposes each item's individual settings before Play.
                GameObject temporaryInstaller = new GameObject("CEVR_TemporaryEarthquakeBinder");
                try
                {
                    EarthquakeSceneResponseInstaller installer =
                        temporaryInstaller.AddComponent<EarthquakeSceneResponseInstaller>();
                    installer.Configure(UnityEngine.Object.FindFirstObjectByType<GroundMotionPlayer>(), 3);
                    installer.BindSceneResponses();
                    if (installer.PhysicsObjectsBound < 4 || installer.DecorativeObjectsBound < 1)
                        throw new InvalidOperationException("Could not attach earthquake settings to the furniture.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(temporaryInstaller);
                }
                ValidateCreatedFurniture(furniture);

                EditorUtility.DisplayProgressBar("CEVR editable tutorial", "Saving generated meshes and materials", 0.65f);
                var persisted = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                        filter.sharedMesh = Persist(filter.sharedMesh, folder, persisted);
                    foreach (MeshCollider collider in root.GetComponentsInChildren<MeshCollider>(true))
                        collider.sharedMesh = Persist(collider.sharedMesh, folder, persisted);
                    foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                            materials[i] = Persist(materials[i], folder, persisted);
                        renderer.sharedMaterials = materials;
                        if (renderer is SkinnedMeshRenderer skinned)
                            skinned.sharedMesh = Persist(skinned.sharedMesh, folder, persisted);
                    }
                }

                // The temporary prop builder only exists to populate the scene.
                // Remove its script to keep the editor hierarchy straightforward.
                UniversalSceneGameplayBootstrap temporaryPropBuilder =
                    props.GetComponent<UniversalSceneGameplayBootstrap>();
                if (temporaryPropBuilder != null) UnityEngine.Object.DestroyImmediate(temporaryPropBuilder);

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity did not save the baked tutorial scene.");

                Selection.activeGameObject = furniture.gameObject;
                SceneView.RepaintAll();
                Debug.Log("CEVR: Tutorial furniture, props and visual details are now saved in the editable scene. Backup: " + backup);
                if (!automatic)
                    EditorUtility.DisplayDialog("Tutorial furniture saved",
                        "Furniture is now serialized into the tutorial scene, not just generated during Play.\n\n" +
                        "Edit furniture root GameObjects in the Hierarchy and save normally. " +
                        "Opening the scene again will NOT reset your edits.\n\nPre-bake backup:\n" + backup,
                        "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("CEVR: Tutorial bake was not completed. Do NOT save this partial scene. Reopen " +
                               ScenePath + " without saving; pre-bake backup: " + backup);
                if (!automatic)
                    EditorUtility.DisplayDialog("Tutorial bake could not finish",
                        "The scene was not saved by the baker. Reopen it without saving.\nBackup:\n" + backup +
                        "\n\nSee the Console for the error.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isBaking = false;
            }
        }

        private static void ValidateCreatedFurniture(FurnitureSceneDressing furniture)
        {
            if (!furniture.SavedInEditor)
                throw new InvalidOperationException("Furniture dressing did not mark itself as saved.");

            string[] chairs =
            {
                "MovableChair_StrongTableApproach",
                "MovableChair_LabBenchNorth",
                "MovableChair_LabBenchSouth",
                "MovableChair_Spare"
            };
            foreach (string name in chairs)
            {
                GameObject obj = GameObject.Find(name);
                if (obj == null || obj.GetComponent<MovableFurniture>() == null ||
                    obj.GetComponent<Rigidbody>() == null ||
                    obj.GetComponent<InertialRigidbody>() == null ||
                    obj.GetComponentsInChildren<Renderer>(true).Length == 0)
                    throw new InvalidOperationException("Cannot save: missing chair or gameplay parts: " + name);
            }

            string[] required =
            {
                "SturdyCoverTableTop", "UnsecuredTallCabinet", "ProtectivePillow",
                "WearableSafetyShoes_P1", "WearableSafetyShoes_P2"
            };
            foreach (string name in required)
                if (GameObject.Find(name) == null)
                    throw new InvalidOperationException("Cannot save: tutorial prop missing: " + name);

            if (furniture.GetComponentsInChildren<Renderer>(true).Length < 8)
                throw new InvalidOperationException("Cannot save: furniture models were not generated.");
        }

        private static T Persist<T>(
            T source, string folder, Dictionary<UnityEngine.Object, UnityEngine.Object> saved)
            where T : UnityEngine.Object
        {
            if (source == null || EditorUtility.IsPersistent(source)) return source;
            if (saved.TryGetValue(source, out UnityEngine.Object existing)) return (T)existing;

            T copy = UnityEngine.Object.Instantiate(source);
            copy.name = source.name;
            string cleanName = Regex.Replace(source.name ?? "Generated", @"[^a-zA-Z0-9_-]", "_");
            if (string.IsNullOrEmpty(cleanName)) cleanName = "Generated";
            string extension = source is Material ? ".mat" : ".asset";
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + cleanName + extension);
            AssetDatabase.CreateAsset(copy, path);
            saved.Add(source, copy);
            if (copy is Material material)
            {
                foreach (string property in material.GetTexturePropertyNames())
                    material.SetTexture(property, Persist(material.GetTexture(property), folder, saved));
            }
            return copy;
        }
    }

    // Unity runs this on script reload AND when the actual tutorial scene is opened.
    // No Play mode, no manual generator and no destructive re-bake of an edited scene.
    [InitializeOnLoad]
    internal static class TutorialSceneAutoBake
    {
        private static bool pending;

        static TutorialSceneAutoBake()
        {
            EditorSceneManager.sceneOpened -= SceneOpened;
            EditorSceneManager.sceneOpened += SceneOpened;
            QueueCheck();
        }

        private static void SceneOpened(Scene scene, OpenSceneMode mode) => QueueCheck();

        private static void QueueCheck()
        {
            if (pending) return;
            pending = true;
            EditorApplication.update += CheckWhenReady;
        }

        private static void CheckWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= CheckWhenReady;
            pending = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (SceneManager.sceneCount != 1 || scene.path != TutorialSceneBake.ScenePath) return;
            TutorialSceneBake.BakeOnSceneOpen();
        }
    }
}
