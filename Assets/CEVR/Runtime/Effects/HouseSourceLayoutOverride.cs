using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// House-only source-level layout. This runs before FurnitureSceneDressing.Start(), disables that
    /// generic dresser for House, and builds the final visuals directly against the measured
    /// ProBuilder coordinates. No later correction pass is required.
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class HouseSourceLayoutOverride : MonoBehaviour
    {
        [Serializable] private sealed class ModelData { public float[] vertices; public PartData[] parts; }
        [Serializable] private sealed class PartData { public float[] color; public int[] triangles; }

        private readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private Transform gameplayRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (FindFirstObjectByType<HouseSourceLayoutOverride>() != null) return;
            GameObject host = new GameObject("CEVR_HouseSourceLayoutOverride");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<HouseSourceLayoutOverride>();
        }

        private void Start()
        {
            gameplayRoot = GameObject.Find("CEVR_UniversalGameplay")?.transform;
            if (gameplayRoot == null)
            {
                Debug.LogError("CEVR House source layout could not find CEVR_UniversalGameplay.");
                return;
            }

            FurnitureSceneDressing generic = gameplayRoot.GetComponent<FurnitureSceneDressing>();
            if (generic != null) generic.enabled = false;
            HouseLiveLayoutEnforcer oldEnforcer = FindFirstObjectByType<HouseLiveLayoutEnforcer>();
            if (oldEnforcer != null) oldEnforcer.enabled = false;

            RemoveOldRuntimeVisuals();
            BuildDiningSet();
            BuildLivingRoom();
            BuildKitchen();
            BuildBedroomUpstairs();
            BuildDecorDetails();
            BuildCeilingHazards();
            BuildMeasuredWindow();
            BuildAmbientLighting();

            Debug.Log("CEVR HOUSE SOURCE LAYOUT READY: fully decorated House built directly from measured coordinates; upstairs bed verified clear of stairs.");
        }

        private void RemoveOldRuntimeVisuals()
        {
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null) continue;
                string n = r.name;
                if (n.StartsWith("HouseSofa", StringComparison.Ordinal) ||
                    n.StartsWith("HouseCoffeeTable", StringComparison.Ordinal) ||
                    n.StartsWith("HousePlant", StringComparison.Ordinal) ||
                    n.StartsWith("HouseShelfBook", StringComparison.Ordinal) ||
                    n.StartsWith("HousePhoto", StringComparison.Ordinal) ||
                    n.StartsWith("HouseRug", StringComparison.Ordinal))
                    r.enabled = false;
            }

            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (go.name.StartsWith("HouseWindow_", StringComparison.Ordinal) ||
                    go.name.StartsWith("Kenney_", StringComparison.Ordinal) ||
                    go.name.StartsWith("TeamFurniture_", StringComparison.Ordinal) ||
                    go.name.StartsWith("HouseFinal_", StringComparison.Ordinal) ||
                    go.name.StartsWith("HouseDecor_", StringComparison.Ordinal))
                    go.SetActive(false);
            }
        }

        private void BuildDiningSet()
        {
            Vector3 c = HouseSceneLayout.DiningTable;
            GameObject tableTop = GameObject.Find("HouseSturdyTableTop");
            if (tableTop != null)
            {
                tableTop.transform.SetPositionAndRotation(c + Vector3.up * 0.74f, Quaternion.identity);
                Renderer r = tableTop.GetComponent<Renderer>(); if (r != null) r.enabled = false;
                BoxCollider box = tableTop.GetComponent<BoxCollider>();
                if (box != null) box.size = new Vector3(1.55f, 0.14f, 0.88f);
                Team("DiningTable", tableTop.transform, c, new Vector3(1.55f, 0.74f, 0.88f), 0f, false);
            }

            var legs = new List<GameObject>();
            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.name == "HouseSturdyTableLeg") legs.Add(go);
            Vector3[] legOffsets =
            {
                new Vector3(-0.64f, 0.36f, -0.33f), new Vector3(0.64f, 0.36f, -0.33f),
                new Vector3(-0.64f, 0.36f, 0.33f), new Vector3(0.64f, 0.36f, 0.33f)
            };
            for (int i = 0; i < legs.Count && i < 4; i++)
            {
                legs[i].transform.position = c + legOffsets[i];
                Renderer r = legs[i].GetComponent<Renderer>(); if (r != null) r.enabled = false;
                BoxCollider b = legs[i].GetComponent<BoxCollider>(); if (b != null) b.size = new Vector3(0.10f, 0.72f, 0.10f);
            }

            Chair("HouseChair_CoverObstacle", new Vector3(c.x, HouseSceneLayout.FloorY, c.z - 0.78f), 180f);
            Chair("HouseChair_Spare", new Vector3(c.x, HouseSceneLayout.FloorY, c.z + 0.78f), 0f);
            Chair("HouseChair_DiningLeft", new Vector3(c.x - 1.03f, HouseSceneLayout.FloorY, c.z), -90f);
            Chair("HouseChair_DiningRight", new Vector3(c.x + 1.03f, HouseSceneLayout.FloorY, c.z), 90f);

            // A small centerpiece and rug make the dining area read as a real room without blocking
            // the crawl/cover training path.
            Team("Vase", gameplayRoot, c + Vector3.up * 0.74f, new Vector3(0.16f, 0.30f, 0.16f), 0f, false);
            Kenney("rugRectangle", gameplayRoot, c + new Vector3(0f, 0.012f, 0f), new Vector3(2.55f, 0.02f, 1.95f), 0f, false);
        }

        private void Chair(string objectName, Vector3 bottomCenter, float yaw)
        {
            GameObject chair = GameObject.Find(objectName);
            if (chair == null) return;
            chair.transform.localScale = new Vector3(0.52f, 0.98f, 0.52f);
            chair.transform.SetPositionAndRotation(bottomCenter, Quaternion.Euler(0f, yaw, 0f));
            foreach (Renderer r in chair.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            Team("DiningChair", chair.transform, bottomCenter, new Vector3(0.44f, 0.94f, 0.42f), yaw, false);
        }

        private void BuildLivingRoom()
        {
            Team("Sofa", gameplayRoot, HouseSceneLayout.Sofa, new Vector3(2.05f, 0.88f, 1.02f), 90f, true);
            Team("Sofa_Pillows", gameplayRoot, HouseSceneLayout.Sofa + new Vector3(0.18f, 0.48f, 0f),
                new Vector3(1.55f, 0.21f, 0.25f), 90f, false);
            Kenney("tableCoffee", gameplayRoot, HouseSceneLayout.CoffeeTable, new Vector3(1.55f, 0.48f, 0.82f), 90f, true);
            Kenney("rugRectangle", gameplayRoot, HouseSceneLayout.Rug + Vector3.up * 0.01f, new Vector3(2.50f, 0.02f, 3.00f), 90f, false);

            // Sofa is west of TV; +90 faces the display toward the sofa instead of showing its back.
            Kenney("cabinetTelevision", gameplayRoot, HouseSceneLayout.Television, new Vector3(2.0f, 0.65f, 0.50f), 90f, true);
            Kenney("televisionModern", gameplayRoot, HouseSceneLayout.Television + Vector3.up * 0.65f,
                new Vector3(1.45f, 0.85f, 0.22f), 90f, false);

            Kenney("pottedPlant", gameplayRoot, HouseSceneLayout.OnFloor(-4.45f, -0.95f), new Vector3(0.58f, 1.18f, 0.58f), 0f, false);
            Kenney("lampRoundFloor", gameplayRoot, HouseSceneLayout.OnFloor(-4.45f, 1.90f), new Vector3(0.42f, 1.45f, 0.42f), 0f, false);
            Kenney("books", gameplayRoot, HouseSceneLayout.CoffeeTable + Vector3.up * 0.49f + new Vector3(0.20f, 0f, -0.12f),
                new Vector3(0.30f, 0.12f, 0.24f), 15f, false);
        }

        private void BuildKitchen()
        {
            Kenney("kitchenCabinet", gameplayRoot, HouseSceneLayout.KitchenCabinet, new Vector3(1.05f, 0.90f, 0.65f), 180f, true);
            Kenney("kitchenSink", gameplayRoot, HouseSceneLayout.KitchenSink, new Vector3(1.05f, 0.90f, 0.65f), 180f, true);
            Kenney("kitchenStove", gameplayRoot, HouseSceneLayout.KitchenStove, new Vector3(0.90f, 0.90f, 0.65f), 180f, true);

            GameObject fridgeAnchor = GameObject.Find("HouseTallCabinet_Left");
            if (fridgeAnchor != null)
            {
                Renderer r = fridgeAnchor.GetComponent<Renderer>(); if (r != null) r.enabled = false;
                Team("Fridge", fridgeAnchor.transform, HouseSceneLayout.FridgeBottom, new Vector3(0.78f, 1.95f, 0.76f), 180f, false);
            }

            GameObject wardrobeAnchor = GameObject.Find("HouseBookcase_Right");
            if (wardrobeAnchor != null)
            {
                Renderer r = wardrobeAnchor.GetComponent<Renderer>(); if (r != null) r.enabled = false;
                Team("Wandrobe", wardrobeAnchor.transform, HouseSceneLayout.WardrobeBottom, new Vector3(1.01f, 2.03f, 0.68f), 180f, false);
            }

            Kenney("rugRectangle", gameplayRoot, HouseSceneLayout.OnFloor(3.35f, 5.10f, 0.01f), new Vector3(2.65f, 0.02f, 0.70f), 0f, false);
            Kenney("pottedPlant", gameplayRoot, HouseSceneLayout.OnFloor(5.50f, 5.15f), new Vector3(0.48f, 1.05f, 0.48f), 0f, false);
        }

        private void BuildBedroomUpstairs()
        {
            // Measured upper landing: x=4.25..6.25, z=0.25..3.00 at y=4.0.
            // Upper stair reaches into z>=1.5 and roughly x<=5.0. Therefore the entire bed is forced
            // into the guaranteed-clear rectangle x=4.75..6.12, z=0.28..1.12.
            Vector3 bedBottom = new Vector3(5.44f, HouseSceneLayout.SecondFloorY, 0.70f);
            GameObject bed = Team("Bed", gameplayRoot, bedBottom, new Vector3(1.36f, 0.82f, 0.84f), 90f, true);
            Team("Bed_Pillow", gameplayRoot, new Vector3(5.78f, HouseSceneLayout.SecondFloorY + 0.50f, 0.70f),
                new Vector3(0.48f, 0.08f, 0.24f), 90f, false);

            // Bedroom details stay in z<1.45 so none can block the stair arrival at z>=1.5.
            Kenney("rugRectangle", gameplayRoot, new Vector3(5.25f, HouseSceneLayout.SecondFloorY + 0.01f, 0.82f),
                new Vector3(1.75f, 0.02f, 1.10f), 90f, false);
            Kenney("tableCoffee", gameplayRoot, new Vector3(4.45f, HouseSceneLayout.SecondFloorY, 0.55f),
                new Vector3(0.46f, 0.48f, 0.42f), 0f, true);
            Kenney("lampRoundFloor", gameplayRoot, new Vector3(4.45f, HouseSceneLayout.SecondFloorY, 1.05f),
                new Vector3(0.32f, 1.10f, 0.32f), 0f, false);
            Kenney("books", gameplayRoot, new Vector3(4.45f, HouseSceneLayout.SecondFloorY + 0.49f, 0.55f),
                new Vector3(0.22f, 0.10f, 0.18f), 0f, false);

            if (bed != null) ValidateBedClearOfUpperStairs(bed);
        }

        private void ValidateBedClearOfUpperStairs(GameObject bed)
        {
            if (!Bounds(bed.transform, out Bounds bedBounds)) return;

            bool overlapsStair = false;
            foreach (Collider collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (collider == null || collider.name != "Stairs (1)") continue;
                Bounds stair = collider.bounds;
                bool xzOverlap = bedBounds.max.x > stair.min.x && bedBounds.min.x < stair.max.x &&
                                 bedBounds.max.z > stair.min.z && bedBounds.min.z < stair.max.z;
                bool sameLevel = bedBounds.max.y > stair.min.y && bedBounds.min.y < stair.max.y + 0.25f;
                if (xzOverlap && sameLevel) { overlapsStair = true; break; }
            }

            if (!overlapsStair)
            {
                Debug.Log($"CEVR upstairs bed verified clear of stairs: bounds {bedBounds.min} .. {bedBounds.max}");
                return;
            }

            // Defensive fallback: pull the bed farther south into the guaranteed-clear strip.
            Vector3 targetBottom = new Vector3(5.45f, HouseSceneLayout.SecondFloorY, 0.52f);
            Fit(bed.transform, new Vector3(1.30f, 0.80f, 0.68f), targetBottom);
            Debug.LogWarning("CEVR upstairs bed overlapped Stairs (1); moved to defensive clear-strip fallback.");
        }

        private void BuildDecorDetails()
        {
            // Ground-floor reading/storage corner.
            Kenney("bookcaseOpen", gameplayRoot, HouseSceneLayout.OnFloor(-4.35f, 3.65f), new Vector3(0.88f, 1.85f, 0.42f), 90f, true);
            Kenney("books", gameplayRoot, HouseSceneLayout.OnFloor(-3.75f, 3.65f, 0.62f), new Vector3(0.28f, 0.14f, 0.22f), 90f, false);

            // Small decorative console near the entry, kept shallow so the walking route stays open.
            Kenney("tableCoffee", gameplayRoot, HouseSceneLayout.OnFloor(0.70f, -5.75f), new Vector3(0.90f, 0.42f, 0.38f), 0f, true);
            Team("Vase", gameplayRoot, HouseSceneLayout.OnFloor(0.70f, -5.75f, 0.43f), new Vector3(0.15f, 0.28f, 0.15f), 0f, false);

            // Simple framed wall art made from thin primitives; no colliders, so nothing affects play.
            WallArt("LivingArtA", new Vector3(-4.88f, 2.35f, 0.20f), new Vector3(0.03f, 0.72f, 0.95f), new Color(0.20f, 0.42f, 0.58f));
            WallArt("LivingArtB", new Vector3(-4.88f, 2.35f, 1.35f), new Vector3(0.03f, 0.72f, 0.95f), new Color(0.66f, 0.33f, 0.38f));
            WallArt("DiningArt", new Vector3(-1.90f, 2.40f, -6.70f), new Vector3(1.15f, 0.78f, 0.03f), new Color(0.30f, 0.48f, 0.36f));
        }

        private void BuildCeilingHazards()
        {
            const float ceilingTop = 3.96f;
            foreach (GameObject hazard in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (!hazard.name.StartsWith("HouseFallingObject_", StringComparison.Ordinal)) continue;
                hazard.transform.position = new Vector3(hazard.transform.position.x, 3.84f, hazard.transform.position.z);
                Renderer r = hazard.GetComponent<Renderer>(); if (r != null) r.enabled = false;
                Kenney("books", hazard.transform,
                    new Vector3(hazard.transform.position.x, ceilingTop - 0.18f, hazard.transform.position.z),
                    new Vector3(0.55f, 0.18f, 0.38f), 0f, false);
            }

            Vector3[] lampXZ = { new Vector3(-2.8f, 0f, 0.60f), new Vector3(3.4f, 0f, 5.20f) };
            foreach (Vector3 p in lampXZ)
                Kenney("lampSquareCeiling", gameplayRoot, new Vector3(p.x, ceilingTop - 0.20f, p.z),
                    new Vector3(0.75f, 0.20f, 0.45f), 0f, false);
        }

        private void BuildMeasuredWindow()
        {
            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go.name.StartsWith("HouseWindow_", StringComparison.Ordinal)) go.SetActive(false);

            Vector3 center = HouseSceneLayout.WindowCenter;
            Vector3 paneSize = HouseSceneLayout.WindowSize;
            Material frame = Solid("Measured Window Frame", new Color(0.08f, 0.10f, 0.12f));

            GameObject root = new GameObject("HouseWindow_REAL_Fitted");
            root.transform.SetParent(gameplayRoot, false);
            root.transform.position = center;

            GameObject pane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pane.name = "HouseWindow_REAL_Glass";
            pane.transform.SetParent(root.transform, false);
            pane.transform.localPosition = Vector3.zero;
            pane.transform.localScale = paneSize;
            Collider pc = pane.GetComponent<Collider>(); if (pc != null) Destroy(pc);
            pane.AddComponent<WindowView>().Configure();
            pane.AddComponent<BreakableWindow>().Configure("house-window-real", FindFirstObjectByType<GroundMotionPlayer>(), FindFirstObjectByType<SessionLogger>());

            const float fw = 0.07f;
            const float fd = 0.10f;
            Frame(root.transform, "Top", new Vector3(0f, paneSize.y * 0.5f + fw * 0.5f, 0f), new Vector3(paneSize.x + fw * 2f, fw, fd), frame);
            Frame(root.transform, "Bottom", new Vector3(0f, -paneSize.y * 0.5f - fw * 0.5f, 0f), new Vector3(paneSize.x + fw * 2f, fw, fd), frame);
            Frame(root.transform, "Left", new Vector3(-paneSize.x * 0.5f - fw * 0.5f, 0f, 0f), new Vector3(fw, paneSize.y, fd), frame);
            Frame(root.transform, "Right", new Vector3(paneSize.x * 0.5f + fw * 0.5f, 0f, 0f), new Vector3(fw, paneSize.y, fd), frame);
            Frame(root.transform, "Mullion", Vector3.zero, new Vector3(0.045f, paneSize.y, fd), frame);
        }

        private void BuildAmbientLighting()
        {
            AddWarmLight("LivingWarmLight", new Vector3(-3.25f, 3.45f, 0.60f), 1.10f, 5.0f);
            AddWarmLight("DiningWarmLight", new Vector3(-1.90f, 3.45f, -2.70f), 0.90f, 4.4f);
            AddWarmLight("KitchenWarmLight", new Vector3(3.40f, 3.45f, 5.40f), 0.95f, 4.8f);
            AddWarmLight("BedroomWarmLight", new Vector3(5.20f, 5.55f, 0.75f), 0.72f, 3.2f);
        }

        private void AddWarmLight(string name, Vector3 position, float intensity, float range)
        {
            GameObject go = new GameObject("HouseDecor_" + name);
            go.transform.SetParent(gameplayRoot, false);
            go.transform.position = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.88f, 0.74f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private void WallArt(string name, Vector3 position, Vector3 scale, Color color)
        {
            Material frame = Solid(name + " Frame", new Color(0.10f, 0.08f, 0.07f));
            Material art = Solid(name + " Art", color);
            GameObject outer = DecorCube("HouseDecor_" + name + "Frame", position, scale, frame);
            Vector3 insetScale = new Vector3(scale.x < 0.1f ? 0.012f : scale.x * 0.84f,
                scale.y * 0.82f,
                scale.z < 0.1f ? 0.012f : scale.z * 0.84f);
            Vector3 normalOffset = scale.x < 0.1f ? Vector3.right * 0.022f : Vector3.forward * 0.022f;
            DecorCube("HouseDecor_" + name + "Art", position + normalOffset, insetScale, art);
            if (outer != null) { }
        }

        private GameObject DecorCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(gameplayRoot, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
            Renderer r = go.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = material;
            return go;
        }

        private GameObject Team(string resource, Transform parent, Vector3 bottomCenter, Vector3 desiredSize, float yaw, bool collider)
        {
            GameObject prefab = Resources.Load<GameObject>("PlengFurniture/" + resource);
            if (prefab == null) { Debug.LogWarning("Missing Pleng furniture: " + resource); return null; }
            GameObject go = Instantiate(prefab);
            go.name = "HouseFinal_" + resource;
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                if (CollisionNamed(r.transform, go.transform)) r.enabled = false;
            ConvertMaterials(go);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one;
            if (!Fit(go.transform, desiredSize, bottomCenter)) { Destroy(go); return null; }
            go.transform.SetParent(parent, true);
            if (collider) AddBoundsCollider(go);
            return go;
        }

        private GameObject Kenney(string resource, Transform parent, Vector3 bottomCenter, Vector3 desiredSize, float yaw, bool collider)
        {
            GameObject template = KenneyTemplate(resource);
            if (template == null) return null;
            GameObject go = Instantiate(template);
            go.name = "HouseFinal_" + resource;
            go.SetActive(true);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one;
            if (!Fit(go.transform, desiredSize, bottomCenter)) { Destroy(go); return null; }
            go.transform.SetParent(parent, true);
            if (collider) AddBoundsCollider(go);
            return go;
        }

        private GameObject KenneyTemplate(string name)
        {
            if (templates.TryGetValue(name, out GameObject cached) && cached != null) return cached;
            TextAsset source = Resources.Load<TextAsset>("Furniture/" + name);
            if (source == null) { Debug.LogWarning("Missing furniture resource: " + name); return null; }
            ModelData data = JsonUtility.FromJson<ModelData>(source.text);
            if (data == null || data.vertices == null || data.parts == null) return null;
            GameObject template = new GameObject("HouseTemplate_" + name);
            template.transform.SetParent(transform, false);
            template.SetActive(false);
            Shader shader = LitShader();
            foreach (PartData part in data.parts)
            {
                if (part == null || part.triangles == null || part.color == null || part.color.Length < 3) continue;
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
                Mesh mesh = new Mesh { name = "HouseMesh_" + name };
                mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                owned.Add(mesh);
                Material mat = new Material(shader) { color = new Color(part.color[0], part.color[1], part.color[2]) };
                owned.Add(mat);
                GameObject surface = new GameObject("Surface");
                surface.transform.SetParent(template.transform, false);
                surface.AddComponent<MeshFilter>().sharedMesh = mesh;
                surface.AddComponent<MeshRenderer>().sharedMaterial = mat;
            }
            templates[name] = template;
            return template;
        }

        private static bool Fit(Transform target, Vector3 desired, Vector3 bottomCenter)
        {
            if (!Bounds(target, out Bounds before) || before.size.x < 0.0001f || before.size.y < 0.0001f || before.size.z < 0.0001f) return false;
            target.localScale = new Vector3(desired.x / before.size.x, desired.y / before.size.y, desired.z / before.size.z);
            if (!Bounds(target, out Bounds after)) return false;
            target.position += bottomCenter - new Vector3(after.center.x, after.min.y, after.center.z);
            return true;
        }

        private static bool Bounds(Transform root, out Bounds bounds)
        {
            bounds = default; bool found = false;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled || CollisionNamed(r.transform, root)) continue;
                if (!found) { bounds = r.bounds; found = true; } else bounds.Encapsulate(r.bounds);
            }
            return found;
        }

        private static bool CollisionNamed(Transform t, Transform root)
        {
            while (t != null)
            {
                if (t.name.IndexOf("collision", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (t == root) break;
                t = t.parent;
            }
            return false;
        }

        private void ConvertMaterials(GameObject root)
        {
            Shader shader = LitShader();
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material original = mats[i];
                    Color color = original != null && original.HasProperty("_Color") ? original.color : new Color(0.72f, 0.72f, 0.72f);
                    Material converted = new Material(shader) { color = color, mainTexture = original == null ? null : original.mainTexture };
                    owned.Add(converted); mats[i] = converted;
                }
                r.sharedMaterials = mats;
            }
        }

        private static Shader LitShader() => Shader.Find(GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");

        private Material Solid(string name, Color color)
        {
            Material m = new Material(LitShader()) { name = name, color = color };
            owned.Add(m); return m;
        }

        private static void AddBoundsCollider(GameObject go)
        {
            if (!Bounds(go.transform, out Bounds world)) return;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(world.center);
            Vector3 lossy = go.transform.lossyScale;
            box.size = new Vector3(
                world.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                world.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                world.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));
        }

        private static void Frame(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "HouseWindowFrame_" + name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
            Renderer r = go.GetComponent<Renderer>(); if (r != null) r.sharedMaterial = material;
        }

        private void OnDestroy()
        {
            foreach (UnityEngine.Object item in owned) if (item != null) Destroy(item);
            owned.Clear();
        }
    }
}
