using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    public static class TutorialSceneBake
    {
        [MenuItem("CEVR/Tutorial/Make furniture editable (one time)")]
        public static void Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Stop Play Mode before baking tutorial furniture.");
                return;
            }
            var scene = SceneManager.GetActiveScene();
            if (scene.name != MainMenuController.TutorialScene || SceneManager.sceneCount != 1)
            {
                EditorUtility.DisplayDialog("Open tutorial", "Open only CEVR_ChulaEngineering_Tutorial.unity first.", "OK");
                return;
            }
            var existing = UnityEngine.Object.FindFirstObjectByType<FurnitureSceneDressing>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("Already prepared", "Existing furniture was preserved. Edit its objects directly; this command will not rebuild them.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backup = AssetDatabase.GenerateUniqueAssetPath(scene.path.Replace(".unity", "_BeforeBake_" + stamp + ".unity"));
            if (!AssetDatabase.CopyAsset(scene.path, backup)) throw new InvalidOperationException("Could not back up scene; nothing baked.");
            string parent = "Assets/CEVR/Generated";
            string folder = parent + "/TutorialBake_" + stamp;
            AssetDatabase.CreateFolder(parent, "TutorialBake_" + stamp);
            try
            {
                RuntimeStageRepair.EnsurePlayableStage();
                var props = new GameObject("Tutorial Editable Props");
                props.AddComponent<UniversalSceneGameplayBootstrap>().PrepareTutorialProps();
                var dressing = new GameObject("Tutorial Editable Furniture").AddComponent<FurnitureSceneDressing>();
                dressing.BakeForEditing();
                var saved = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                        filter.sharedMesh = Persist(filter.sharedMesh, folder, saved);
                    foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true))
                        collider.sharedMesh = Persist(collider.sharedMesh, folder, saved);
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++) materials[i] = Persist(materials[i], folder, saved);
                        renderer.sharedMaterials = materials;
                        if (renderer is SkinnedMeshRenderer skinned)
                            skinned.sharedMesh = Persist(skinned.sharedMesh, folder, saved);
                    }
                }
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                Selection.activeGameObject = dressing.gameObject;
                SceneView.RepaintAll();
                EditorUtility.DisplayDialog("Furniture ready for editing",
                    "Inspect the scene and press Cmd/Ctrl+S to keep it. A pre-bake scene backup was saved at:\n" + backup +
                    "\n\nMove furniture ROOT objects, not just their mesh children. Test grabbing, cover and shaking before sharing.", "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Bake stopped", "Do not save this partial result. Reopen the pre-bake backup:\n" + backup, "OK");
            }
        }

        private static T Persist<T>(T source, string folder, Dictionary<UnityEngine.Object, UnityEngine.Object> saved) where T : UnityEngine.Object
        {
            if (source == null || EditorUtility.IsPersistent(source)) return source;
            if (saved.TryGetValue(source, out var result)) return (T)result;
            T copy = UnityEngine.Object.Instantiate(source);
            copy.name = source.name;
            string extension = source is Material ? ".mat" : ".asset";
            AssetDatabase.CreateAsset(copy, AssetDatabase.GenerateUniqueAssetPath(folder + "/Generated" + extension));
            saved.Add(source, copy);
            if (copy is Material material)
                foreach (string property in material.GetTexturePropertyNames())
                    material.SetTexture(property, Persist(material.GetTexture(property), folder, saved));
            return copy;
        }
    }
}
