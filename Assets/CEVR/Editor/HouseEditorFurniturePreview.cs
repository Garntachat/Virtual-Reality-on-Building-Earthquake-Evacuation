using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>
    /// Edit-mode visualization of the final House layout. The actual gameplay furniture is still
    /// created by HouseSourceLayoutOverride at runtime, but this preview mirrors the same measured
    /// positions so the Scene view is no longer empty before Play.
    ///
    /// The preview is intentionally not saved into the scene/build. It is rebuilt automatically
    /// whenever the House scene is opened or scripts finish compiling, and it is disabled before
    /// entering Play Mode so it can never duplicate runtime furniture.
    /// </summary>
    [InitializeOnLoad]
    public static class HouseEditorFurniturePreview
    {
        [Serializable] private sealed class ModelData { public float[] vertices; public PartData[] parts; }
        [Serializable] private sealed class PartData { public float[] color; public int[] triangles; }

        private const string RootName = "CEVR_HouseEditorFurniturePreview";
        // Keep the preview visible/selectable in the normal Hierarchy. The root is tagged EditorOnly,
        // so Unity strips it from player builds; runtime furniture is still created separately.
        private const HideFlags PreviewFlags = HideFlags.None;
        private static bool rebuilding;
        private static double nextEnsureTime;

        static HouseEditorFurniturePreview()
        {
            InstallHooks();
        }

        [InitializeOnLoadMethod]
        private static void InitializeAgain()
        {
            // InitializeOnLoadMethod is intentionally redundant with the static constructor.
            // Some Unity domain-reload configurations can skip a one-shot delay callback; the
            // persistent update hook below guarantees the House preview is eventually created.
            InstallHooks();
        }

        private static void InstallHooks()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.projectChanged -= ScheduleRefresh;
            EditorApplication.projectChanged += ScheduleRefresh;
            EditorApplication.hierarchyChanged -= EnsurePreviewSoon;
            EditorApplication.hierarchyChanged += EnsurePreviewSoon;
            EditorApplication.update -= EnsurePreviewOnEditorUpdate;
            EditorApplication.update += EnsurePreviewOnEditorUpdate;
            ScheduleRefresh();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ScheduleRefresh();
        }

        private static void EnsurePreviewSoon()
        {
            nextEnsureTime = 0d;
        }

        private static void EnsurePreviewOnEditorUpdate()
        {
            if (rebuilding || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.timeSinceStartup < nextEnsureTime) return;
            nextEnsureTime = EditorApplication.timeSinceStartup + 1.0d;

            Scene scene = SceneManager.GetActiveScene();
            if (!IsHouse(scene)) return;

            GameObject existing = FindPreviewRoot(scene);
            if (existing == null)
                RefreshIfHouse();
            else if (!existing.activeSelf)
                existing.SetActive(true);
        }

        [MenuItem("CEVR/House/Refresh Editor Furniture Preview", priority = 20)]
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

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                SetPreviewActive(false);
            else if (state == PlayModeStateChange.EnteredEditMode)
                ScheduleRefresh();
        }

        private static void ScheduleRefresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.delayCall += RefreshIfHouse;
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

                GameObject root = new GameObject(RootName) { hideFlags = PreviewFlags };
                root.tag = "EditorOnly";
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                BuildDining(root.transform, scene);
                BuildLiving(root.transform, scene);
                BuildKitchen(root.transform, scene);
                BuildBedroom(root.transform, scene);
                BuildDetails(root.transform, scene);
                BuildCeilingHazards(root.transform, scene);
                BuildWindow(root.transform, scene);

                // Marking dirty makes it obvious these are real, selectable scene objects in Edit
                // mode. If the scene is saved, the EditorOnly root may persist in the .unity file but
                // is stripped from builds and disabled before entering Play Mode.
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("CEVR HOUSE EDITOR PREVIEW READY: selectable furniture/decor is now present in the House Hierarchy without Play.");
                SceneView.RepaintAll();
                EditorApplication.RepaintHierarchyWindow();
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
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || go.name != RootName || go.scene != scene) continue;
                return go;
            }
            return null;
        }

        private static void RemovePreview()
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || go.name != RootName || !go.scene.IsValid()) continue;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void SetPreviewActive(bool active)
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || go.name != RootName || !go.scene.IsValid()) continue;
                go.SetActive(active);
            }
        }

        private static void BuildDining(Transform root, Scene scene)
        {
            Vector3 c = HouseSceneLayout.DiningTable;
            Team("DiningTable", root, scene, c, new Vector3(1.55f, 0.74f, 0.88f), 0f);
            Team("DiningChair", root, scene, new Vector3(c.x, HouseSceneLayout.FloorY, c.z - 0.78f), new Vector3(0.44f, 0.94f, 0.42f), 180f);
            Team("DiningChair", root, scene, new Vector3(c.x, HouseSceneLayout.FloorY, c.z + 0.78f), new Vector3(0.44f, 0.94f, 0.42f), 0f);
            Team("DiningChair", root, scene, new Vector3(c.x - 1.03f, HouseSceneLayout.FloorY, c.z), new Vector3(0.44f, 0.94f, 0.42f), -90f);
            Team("DiningChair", root, scene, new Vector3(c.x + 1.03f, HouseSceneLayout.FloorY, c.z), new Vector3(0.44f, 0.94f, 0.42f), 90f);
            Team("Vase", root, scene, c + Vector3.up * 0.74f, new Vector3(0.16f, 0.30f, 0.16f), 0f);
            Kenney("rugRectangle", root, scene, c + new Vector3(0f, 0.012f, 0f), new Vector3(2.55f, 0.02f, 1.95f), 0f);
        }

        private static void BuildLiving(Transform root, Scene scene)
        {
            Team("Sofa", root, scene, HouseSceneLayout.Sofa, new Vector3(2.05f, 0.88f, 1.02f), 90f);
            Team("Sofa_Pillows", root, scene, HouseSceneLayout.Sofa + new Vector3(0.18f, 0.48f, 0f), new Vector3(1.55f, 0.21f, 0.25f), 90f);
            Kenney("tableCoffee", root, scene, HouseSceneLayout.CoffeeTable, new Vector3(1.55f, 0.48f, 0.82f), 90f);
            Kenney("rugRectangle", root, scene, HouseSceneLayout.Rug + Vector3.up * 0.01f, new Vector3(2.50f, 0.02f, 3.00f), 90f);
            Kenney("cabinetTelevision", root, scene, HouseSceneLayout.Television, new Vector3(2.0f, 0.65f, 0.50f), 90f);
            Kenney("televisionModern", root, scene, HouseSceneLayout.Television + Vector3.up * 0.65f, new Vector3(1.45f, 0.85f, 0.22f), 90f);
            Kenney("pottedPlant", root, scene, HouseSceneLayout.OnFloor(-4.45f, -0.95f), new Vector3(0.58f, 1.18f, 0.58f), 0f);
            Kenney("lampRoundFloor", root, scene, HouseSceneLayout.OnFloor(-4.45f, 1.90f), new Vector3(0.42f, 1.45f, 0.42f), 0f);
            Kenney("books", root, scene, HouseSceneLayout.CoffeeTable + Vector3.up * 0.49f + new Vector3(0.20f, 0f, -0.12f), new Vector3(0.30f, 0.12f, 0.24f), 15f);
        }

        private static void BuildKitchen(Transform root, Scene scene)
        {
            Kenney("kitchenCabinet", root, scene, HouseSceneLayout.KitchenCabinet, new Vector3(1.05f, 0.90f, 0.65f), 180f);
            Kenney("kitchenSink", root, scene, HouseSceneLayout.KitchenSink, new Vector3(1.05f, 0.90f, 0.65f), 180f);
            Kenney("kitchenStove", root, scene, HouseSceneLayout.KitchenStove, new Vector3(0.90f, 0.90f, 0.65f), 180f);
            Team("Fridge", root, scene, HouseSceneLayout.FridgeBottom, new Vector3(0.78f, 1.95f, 0.76f), 180f);
            Team("Wandrobe", root, scene, HouseSceneLayout.WardrobeBottom, new Vector3(1.01f, 2.03f, 0.68f), 180f);
            Kenney("rugRectangle", root, scene, HouseSceneLayout.OnFloor(3.35f, 5.10f, 0.01f), new Vector3(2.65f, 0.02f, 0.70f), 0f);
            Kenney("pottedPlant", root, scene, HouseSceneLayout.OnFloor(5.50f, 5.15f), new Vector3(0.48f, 1.05f, 0.48f), 0f);
        }

        private static void BuildBedroom(Transform root, Scene scene)
        {
            // This is the same guaranteed-clear second-floor rectangle used at runtime.
            // Bed visual bounds stay approximately x=4.76..6.12 and z=0.28..1.12 at y=4.
            Team("Bed", root, scene, new Vector3(5.44f, HouseSceneLayout.SecondFloorY, 0.70f), new Vector3(1.36f, 0.82f, 0.84f), 90f);
            Team("Bed_Pillow", root, scene, new Vector3(5.78f, HouseSceneLayout.SecondFloorY + 0.50f, 0.70f), new Vector3(0.48f, 0.08f, 0.24f), 90f);
            Kenney("rugRectangle", root, scene, new Vector3(5.25f, HouseSceneLayout.SecondFloorY + 0.01f, 0.82f), new Vector3(1.75f, 0.02f, 1.10f), 90f);
            Kenney("tableCoffee", root, scene, new Vector3(4.45f, HouseSceneLayout.SecondFloorY, 0.55f), new Vector3(0.46f, 0.48f, 0.42f), 0f);
            Kenney("lampRoundFloor", root, scene, new Vector3(4.45f, HouseSceneLayout.SecondFloorY, 1.05f), new Vector3(0.32f, 1.10f, 0.32f), 0f);
            Kenney("books", root, scene, new Vector3(4.45f, HouseSceneLayout.SecondFloorY + 0.49f, 0.55f), new Vector3(0.22f, 0.10f, 0.18f), 0f);
        }

        private static void BuildDetails(Transform root, Scene scene)
        {
            Kenney("bookcaseOpen", root, scene, HouseSceneLayout.OnFloor(-4.35f, 3.65f), new Vector3(0.88f, 1.85f, 0.42f), 90f);
            Kenney("books", root, scene, HouseSceneLayout.OnFloor(-3.75f, 3.65f, 0.62f), new Vector3(0.28f, 0.14f, 0.22f), 90f);
            Kenney("tableCoffee", root, scene, HouseSceneLayout.OnFloor(0.70f, -5.75f), new Vector3(0.90f, 0.42f, 0.38f), 0f);
            Team("Vase", root, scene, HouseSceneLayout.OnFloor(0.70f, -5.75f, 0.43f), new Vector3(0.15f, 0.28f, 0.15f), 0f);

            PreviewCube(root, scene, "LivingArtA", new Vector3(-4.88f, 2.35f, 0.20f), new Vector3(0.03f, 0.72f, 0.95f), new Color(0.20f, 0.42f, 0.58f));
            PreviewCube(root, scene, "LivingArtB", new Vector3(-4.88f, 2.35f, 1.35f), new Vector3(0.03f, 0.72f, 0.95f), new Color(0.66f, 0.33f, 0.38f));
            PreviewCube(root, scene, "DiningArt", new Vector3(-1.90f, 2.40f, -6.70f), new Vector3(1.15f, 0.78f, 0.03f), new Color(0.30f, 0.48f, 0.36f));
        }

        private static void BuildCeilingHazards(Transform root, Scene scene)
        {
            foreach (Vector3 p in HouseSceneLayout.OverheadHazards)
                Kenney("books", root, scene, new Vector3(p.x, 3.78f, p.z), new Vector3(0.55f, 0.18f, 0.38f), 0f);

            Kenney("lampSquareCeiling", root, scene, new Vector3(-2.8f, 3.76f, 0.60f), new Vector3(0.75f, 0.20f, 0.45f), 0f);
            Kenney("lampSquareCeiling", root, scene, new Vector3(3.4f, 3.76f, 5.20f), new Vector3(0.75f, 0.20f, 0.45f), 0f);
        }

        private static void BuildWindow(Transform root, Scene scene)
        {
            Vector3 c = HouseSceneLayout.WindowCenter;
            Vector3 s = HouseSceneLayout.WindowSize;
            const float fw = 0.07f;
            const float fd = 0.10f;

            PreviewCube(root, scene, "WindowGlass", c, s, new Color(0.36f, 0.64f, 0.80f, 0.35f));
            PreviewCube(root, scene, "WindowTop", c + new Vector3(0f, s.y * 0.5f + fw * 0.5f, 0f), new Vector3(s.x + fw * 2f, fw, fd), new Color(0.08f, 0.10f, 0.12f));
            PreviewCube(root, scene, "WindowBottom", c + new Vector3(0f, -s.y * 0.5f - fw * 0.5f, 0f), new Vector3(s.x + fw * 2f, fw, fd), new Color(0.08f, 0.10f, 0.12f));
            PreviewCube(root, scene, "WindowLeft", c + new Vector3(-s.x * 0.5f - fw * 0.5f, 0f, 0f), new Vector3(fw, s.y, fd), new Color(0.08f, 0.10f, 0.12f));
            PreviewCube(root, scene, "WindowRight", c + new Vector3(s.x * 0.5f + fw * 0.5f, 0f, 0f), new Vector3(fw, s.y, fd), new Color(0.08f, 0.10f, 0.12f));
            PreviewCube(root, scene, "WindowMullion", c, new Vector3(0.045f, s.y, fd), new Color(0.08f, 0.10f, 0.12f));
        }

        private static GameObject Team(string resource, Transform parent, Scene scene, Vector3 bottomCenter, Vector3 desiredSize, float yaw)
        {
            GameObject prefab = Resources.Load<GameObject>("PlengFurniture/" + resource);
            if (prefab == null)
            {
                Debug.LogWarning("CEVR editor preview missing Pleng furniture: " + resource);
                return null;
            }

            GameObject go = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (go == null) return null;
            go.name = "EDITOR_" + resource;
            go.transform.SetParent(parent, true);
            SetPreviewFlags(go);
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                if (CollisionNamed(renderer.transform, go.transform)) renderer.enabled = false;

            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one;
            if (!Fit(go.transform, desiredSize, bottomCenter))
            {
                UnityEngine.Object.DestroyImmediate(go);
                return null;
            }
            return go;
        }

        private static GameObject Kenney(string resource, Transform parent, Scene scene, Vector3 bottomCenter, Vector3 desiredSize, float yaw)
        {
            TextAsset source = Resources.Load<TextAsset>("Furniture/" + resource);
            if (source == null) return null;
            ModelData data = JsonUtility.FromJson<ModelData>(source.text);
            if (data == null || data.vertices == null || data.parts == null) return null;

            GameObject go = new GameObject("EDITOR_" + resource) { hideFlags = PreviewFlags };
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(parent, true);

            Shader shader = LitShader();
            foreach (PartData part in data.parts)
            {
                if (part == null || part.color == null || part.color.Length < 3 || part.triangles == null) continue;
                Vector3[] vertices = new Vector3[part.triangles.Length];
                int[] triangles = new int[vertices.Length];
                bool ok = true;
                for (int i = 0; i < vertices.Length; i++)
                {
                    int v = part.triangles[i] * 3;
                    if (v < 0 || v + 2 >= data.vertices.Length) { ok = false; break; }
                    vertices[i] = new Vector3(data.vertices[v], data.vertices[v + 1], data.vertices[v + 2]);
                    triangles[i] = i;
                }
                if (!ok) continue;

                Mesh mesh = new Mesh { name = "EDITOR_Mesh_" + resource, hideFlags = PreviewFlags };
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                Material material = new Material(shader)
                {
                    name = "EDITOR_Mat_" + resource,
                    color = new Color(part.color[0], part.color[1], part.color[2]),
                    hideFlags = PreviewFlags
                };

                GameObject surface = new GameObject("Surface") { hideFlags = PreviewFlags };
                surface.transform.SetParent(go.transform, false);
                surface.AddComponent<MeshFilter>().sharedMesh = mesh;
                surface.AddComponent<MeshRenderer>().sharedMaterial = material;
            }

            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one;
            if (!Fit(go.transform, desiredSize, bottomCenter))
            {
                UnityEngine.Object.DestroyImmediate(go);
                return null;
            }
            return go;
        }

        private static void PreviewCube(Transform root, Scene scene, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "EDITOR_" + name;
            go.hideFlags = PreviewFlags;
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(root, true);
            go.transform.position = position;
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

            Material material = new Material(LitShader())
            {
                name = "EDITOR_" + name + "_Material",
                color = color,
                hideFlags = PreviewFlags
            };
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static bool Fit(Transform target, Vector3 desired, Vector3 bottomCenter)
        {
            if (!TryBounds(target, out Bounds before) ||
                before.size.x < 0.0001f || before.size.y < 0.0001f || before.size.z < 0.0001f)
                return false;

            target.localScale = new Vector3(
                desired.x / before.size.x,
                desired.y / before.size.y,
                desired.z / before.size.z);

            if (!TryBounds(target, out Bounds after)) return false;
            target.position += bottomCenter - new Vector3(after.center.x, after.min.y, after.center.z);
            return true;
        }

        private static bool TryBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || CollisionNamed(renderer.transform, root)) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static bool CollisionNamed(Transform candidate, Transform root)
        {
            Transform current = candidate;
            while (current != null)
            {
                if (current.name.IndexOf("collision", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (current == root) break;
                current = current.parent;
            }
            return false;
        }

        private static Shader LitShader()
        {
            return Shader.Find(GraphicsSettings.currentRenderPipeline == null
                       ? "Standard"
                       : "Universal Render Pipeline/Lit")
                   ?? Shader.Find("Sprites/Default");
        }

        private static void SetPreviewFlags(GameObject root)
        {
            root.hideFlags = PreviewFlags;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = PreviewFlags;
        }
    }
}
