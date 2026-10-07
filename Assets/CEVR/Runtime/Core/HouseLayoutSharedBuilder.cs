using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// SINGLE SOURCE OF TRUTH for the House visual layout.
    ///
    /// Both Edit-mode preview and Play-mode runtime call this exact builder. No coordinates,
    /// furniture dimensions, rotations, placeholder hiding, or decoration placement are duplicated
    /// in Editor code anymore.
    /// </summary>
    public static class HouseLayoutSharedBuilder
    {
        [Serializable] private sealed class ModelData { public float[] vertices; public PartData[] parts; }
        [Serializable] private sealed class PartData { public float[] color; public int[] triangles; }

        public sealed class BuildContext
        {
            public Scene scene;
            public Transform root;
            public bool runtime;
            public GroundMotionPlayer groundMotion;
            public SessionLogger logger;

            internal readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        }

        public static void Build(BuildContext context)
        {
            if (context == null || context.root == null || !context.scene.IsValid()) return;

            ApplyAuthoredState(context.scene);
            BuildDining(context);
            BuildLiving(context);
            BuildKitchen(context);
            BuildBedroom(context);
            BuildDetails(context);
            BuildCeilingHazards(context);
            BuildWindow(context);
            BuildLighting(context);
        }

        public static bool ValidateBuiltLayout(Scene scene, Transform root, out string report)
        {
            var problems = new List<string>();
            if (root == null)
            {
                report = "missing layout root";
                return false;
            }

            RequireCount(root, "HouseLayout_DiningTable", 1, problems);
            RequireCount(root, "HouseLayout_DiningChair", 4, problems);
            RequireCount(root, "HouseLayout_Sofa", 1, problems);
            RequireCount(root, "HouseLayout_televisionModern", 1, problems);
            RequireCount(root, "HouseLayout_Bed", 1, problems);
            RequireCount(root, "HouseLayout_Fridge", 1, problems);
            RequireCount(root, "HouseLayout_Wandrobe", 1, problems);
            RequireCount(root, "HouseLayout_WindowGlass", 1, problems);

            Transform bed = FindChildExact(root, "HouseLayout_Bed");
            if (bed != null && HouseFurnitureGeometry.TryVisibleBounds(bed, out Bounds bedBounds))
            {
                if (Mathf.Abs(bedBounds.min.y - HouseSceneLayout.SecondFloorY) > 0.06f)
                    problems.Add($"bed bottom is y={bedBounds.min.y:F2}, expected {HouseSceneLayout.SecondFloorY:F2}");

                GameObject stairObject = FindSceneObject(scene, "Stairs (1)");
                Collider stair = stairObject == null ? null : stairObject.GetComponent<Collider>();
                if (stair != null)
                {
                    Bounds sb = stair.bounds;
                    bool xz = bedBounds.max.x > sb.min.x && bedBounds.min.x < sb.max.x &&
                              bedBounds.max.z > sb.min.z && bedBounds.min.z < sb.max.z;
                    bool y = bedBounds.max.y > sb.min.y && bedBounds.min.y < sb.max.y + 0.25f;
                    if (xz && y) problems.Add("bed overlaps Stairs (1)");
                }
            }

            Transform table = FindChildExact(root, "HouseLayout_DiningTable");
            if (table != null && HouseFurnitureGeometry.TryVisibleBounds(table, out Bounds tableBounds) &&
                Mathf.Abs(tableBounds.min.y - HouseSceneLayout.FloorY) > 0.06f)
                problems.Add($"dining table bottom is y={tableBounds.min.y:F2}, expected {HouseSceneLayout.FloorY:F2}");

            Transform tv = FindChildExact(root, "HouseLayout_televisionModern");
            if (tv != null)
            {
                float yaw = NormalizeYaw(tv.eulerAngles.y);
                if (Mathf.Abs(Mathf.DeltaAngle(yaw, HouseSceneLayout.TelevisionYaw)) > 1f)
                    problems.Add($"TV yaw is {yaw:F1}, expected {HouseSceneLayout.TelevisionYaw:F1}");
            }

            report = problems.Count == 0 ? "PASS" : string.Join("; ", problems);
            return problems.Count == 0;
        }

        private static void RequireCount(Transform root, string exactName, int expected, List<string> problems)
        {
            int count = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == exactName) count++;
            if (count != expected) problems.Add($"{exactName} count={count}, expected {expected}");
        }

        private static Transform FindChildExact(Transform root, string exactName)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == exactName) return t;
            return null;
        }

        private static float NormalizeYaw(float yaw)
        {
            yaw %= 360f;
            if (yaw < 0f) yaw += 360f;
            return yaw;
        }

        /// <summary>
        /// Mirrors the non-visual authored-object changes required by Play mode. This is deliberately
        /// shared with Edit mode so the scene cannot show old placeholder furniture underneath the
        /// new furniture.
        /// </summary>
        public static void ApplyAuthoredState(Scene scene)
        {
            Vector3 dining = HouseSceneLayout.DiningTable;

            GameObject tableTop = FindSceneObject(scene, "HouseSturdyTableTop");
            if (tableTop != null)
            {
                tableTop.transform.SetPositionAndRotation(
                    dining + Vector3.up * HouseSceneLayout.DiningTableSize.y,
                    Quaternion.identity);
                SetRenderers(tableTop, false);
                BoxCollider box = tableTop.GetComponent<BoxCollider>();
                if (box != null)
                    box.size = new Vector3(HouseSceneLayout.DiningTableSize.x, 0.14f, HouseSceneLayout.DiningTableSize.z);
            }

            Vector3[] legOffsets =
            {
                new Vector3(-0.64f, 0.36f, -0.33f),
                new Vector3( 0.64f, 0.36f, -0.33f),
                new Vector3(-0.64f, 0.36f,  0.33f),
                new Vector3( 0.64f, 0.36f,  0.33f)
            };
            int leg = 0;
            foreach (GameObject go in FindSceneObjects(scene))
            {
                if (go.name != "HouseSturdyTableLeg" || leg >= legOffsets.Length) continue;
                go.transform.position = dining + legOffsets[leg++];
                SetRenderers(go, false);
                BoxCollider b = go.GetComponent<BoxCollider>();
                if (b != null) b.size = new Vector3(0.10f, 0.72f, 0.10f);
            }

            ConfigureChairAnchor(scene, "HouseChair_CoverObstacle", HouseSceneLayout.CoverObstacleChair, 180f);
            ConfigureChairAnchor(scene, "HouseChair_Spare", HouseSceneLayout.SpareChair, 0f);
            ConfigureChairAnchor(scene, "HouseChair_DiningLeft", HouseSceneLayout.DiningLeftChair, -90f);
            ConfigureChairAnchor(scene, "HouseChair_DiningRight", HouseSceneLayout.DiningRightChair, 90f);

            string[] placeholderPrefixes =
            {
                "HouseSofa", "HouseCoffeeTable", "HousePlant", "HouseShelfBook", "HousePhoto", "HouseRug"
            };
            foreach (Renderer renderer in FindSceneComponents<Renderer>(scene))
            {
                foreach (string prefix in placeholderPrefixes)
                {
                    if (!renderer.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    renderer.enabled = false;
                    break;
                }
            }

            GameObject fridge = FindSceneObject(scene, "HouseTallCabinet_Left");
            if (fridge != null) SetRenderers(fridge, false);

            GameObject wardrobe = FindSceneObject(scene, "HouseBookcase_Right");
            if (wardrobe != null) SetRenderers(wardrobe, false);

            foreach (GameObject go in FindSceneObjects(scene))
            {
                if (go.name.StartsWith("HouseFallingObject_", StringComparison.Ordinal))
                {
                    go.transform.position = new Vector3(go.transform.position.x, 3.84f, go.transform.position.z);
                    SetRenderers(go, false);
                }

                if (go.name.StartsWith("HouseWindow_", StringComparison.Ordinal))
                    go.SetActive(false);
            }
        }

        private static void BuildDining(BuildContext c)
        {
            Vector3 p = HouseSceneLayout.DiningTable;
            Team(c, "DiningTable", p, HouseSceneLayout.DiningTableSize, HouseSceneLayout.DiningTableYaw, c.runtime);
            Team(c, "DiningChair", HouseSceneLayout.CoverObstacleChair, HouseSceneLayout.DiningChairSize, 180f, false);
            Team(c, "DiningChair", HouseSceneLayout.SpareChair, HouseSceneLayout.DiningChairSize, 0f, false);
            Team(c, "DiningChair", HouseSceneLayout.DiningLeftChair, HouseSceneLayout.DiningChairSize, -90f, false);
            Team(c, "DiningChair", HouseSceneLayout.DiningRightChair, HouseSceneLayout.DiningChairSize, 90f, false);

            Team(c, "Vase", p + Vector3.up * HouseSceneLayout.DiningTableSize.y,
                new Vector3(0.16f, 0.30f, 0.16f), 0f, false);
            Kenney(c, "rugRectangle", p + Vector3.up * 0.012f,
                new Vector3(2.55f, 0.02f, 1.95f), 0f, false);
        }

        private static void BuildLiving(BuildContext c)
        {
            Team(c, "Sofa", HouseSceneLayout.Sofa, HouseSceneLayout.SofaSize, HouseSceneLayout.SofaYaw, c.runtime);
            Team(c, "Sofa_Pillows", HouseSceneLayout.SofaPillows, HouseSceneLayout.SofaPillowsSize, HouseSceneLayout.SofaYaw, false);

            Kenney(c, "tableCoffee", HouseSceneLayout.CoffeeTable, HouseSceneLayout.CoffeeTableSize, 90f, c.runtime);
            Kenney(c, "rugRectangle", HouseSceneLayout.Rug + Vector3.up * 0.01f, HouseSceneLayout.RugSize, 90f, false);

            Kenney(c, "cabinetTelevision", HouseSceneLayout.Television,
                HouseSceneLayout.TelevisionCabinetSize, HouseSceneLayout.TelevisionYaw, c.runtime);
            Kenney(c, "televisionModern", HouseSceneLayout.Television + Vector3.up * 0.65f,
                HouseSceneLayout.TelevisionSize, HouseSceneLayout.TelevisionYaw, false);

            Kenney(c, "pottedPlant", HouseSceneLayout.OnFloor(-4.45f, -0.95f),
                new Vector3(0.58f, 1.18f, 0.58f), 0f, false);
            Kenney(c, "lampRoundFloor", HouseSceneLayout.OnFloor(-4.45f, 1.90f),
                new Vector3(0.42f, 1.45f, 0.42f), 0f, false);
            Kenney(c, "books", HouseSceneLayout.CoffeeTable + Vector3.up * 0.49f + new Vector3(0.20f, 0f, -0.12f),
                new Vector3(0.30f, 0.12f, 0.24f), 15f, false);
        }

        private static void BuildKitchen(BuildContext c)
        {
            Kenney(c, "kitchenCabinet", HouseSceneLayout.KitchenCabinet, new Vector3(1.05f, 0.90f, 0.65f), 180f, c.runtime);
            Kenney(c, "kitchenSink", HouseSceneLayout.KitchenSink, new Vector3(1.05f, 0.90f, 0.65f), 180f, c.runtime);
            Kenney(c, "kitchenStove", HouseSceneLayout.KitchenStove, new Vector3(0.90f, 0.90f, 0.65f), 180f, c.runtime);

            Team(c, "Fridge", HouseSceneLayout.FridgeBottom, new Vector3(0.78f, 1.95f, 0.76f), 180f, c.runtime);
            Team(c, "Wandrobe", HouseSceneLayout.WardrobeBottom, new Vector3(1.01f, 2.03f, 0.68f), 180f, c.runtime);

            Kenney(c, "rugRectangle", HouseSceneLayout.OnFloor(3.35f, 5.10f, 0.01f),
                new Vector3(2.65f, 0.02f, 0.70f), 0f, false);
            Kenney(c, "pottedPlant", HouseSceneLayout.OnFloor(5.50f, 5.15f),
                new Vector3(0.48f, 1.05f, 0.48f), 0f, false);
        }

        private static void BuildBedroom(BuildContext c)
        {
            // Analyzer-safe upper-floor footprint. Bed remains fully below z=1.5 where the upper
            // stair begins, while preserving a normal single-bed aspect.
            GameObject bed = Team(c, "Bed", HouseSceneLayout.Bed,
                HouseSceneLayout.BedSize, HouseSceneLayout.BedYaw, c.runtime);
            Team(c, "Bed_Pillow", HouseSceneLayout.BedPillow,
                HouseSceneLayout.BedPillowSize, HouseSceneLayout.BedYaw, false);

            Kenney(c, "rugRectangle",
                new Vector3(HouseSceneLayout.Bed.x, HouseSceneLayout.SecondFloorY + 0.01f, HouseSceneLayout.Bed.z),
                new Vector3(1.85f, 0.02f, 1.35f), 90f, false);

            if (c.runtime && bed != null) VerifyBedAgainstStairs(c.scene, bed);
        }

        private static void BuildDetails(BuildContext c)
        {
            Kenney(c, "bookcaseOpen", HouseSceneLayout.OnFloor(-4.35f, 3.65f),
                new Vector3(0.88f, 1.85f, 0.42f), 90f, c.runtime);
            Kenney(c, "books", HouseSceneLayout.OnFloor(-3.75f, 3.65f, 0.62f),
                new Vector3(0.28f, 0.14f, 0.22f), 90f, false);

            Kenney(c, "tableCoffee", HouseSceneLayout.OnFloor(0.70f, -5.75f),
                new Vector3(0.90f, 0.42f, 0.38f), 0f, c.runtime);
            Team(c, "Vase", HouseSceneLayout.OnFloor(0.70f, -5.75f, 0.43f),
                new Vector3(0.15f, 0.28f, 0.15f), 0f, false);

            BoxVisual(c, "LivingArtA", new Vector3(-4.88f, 2.35f, 0.20f),
                new Vector3(0.03f, 0.72f, 0.95f), new Color(0.20f, 0.42f, 0.58f));
            BoxVisual(c, "LivingArtB", new Vector3(-4.88f, 2.35f, 1.35f),
                new Vector3(0.03f, 0.72f, 0.95f), new Color(0.66f, 0.33f, 0.38f));
            BoxVisual(c, "DiningArt", new Vector3(-1.90f, 2.40f, -6.70f),
                new Vector3(1.15f, 0.78f, 0.03f), new Color(0.30f, 0.48f, 0.36f));
        }

        private static void BuildCeilingHazards(BuildContext c)
        {
            foreach (Vector3 p in HouseSceneLayout.OverheadHazards)
                Kenney(c, "books", new Vector3(p.x, 3.78f, p.z),
                    new Vector3(0.55f, 0.18f, 0.38f), 0f, false);

            Kenney(c, "lampSquareCeiling", new Vector3(-2.8f, 3.76f, 0.60f),
                new Vector3(0.75f, 0.20f, 0.45f), 0f, false);
            Kenney(c, "lampSquareCeiling", new Vector3(3.4f, 3.76f, 5.20f),
                new Vector3(0.75f, 0.20f, 0.45f), 0f, false);
        }

        private static void BuildWindow(BuildContext c)
        {
            Vector3 center = HouseSceneLayout.WindowCenter;
            Vector3 size = HouseSceneLayout.WindowSize;
            const float frameWidth = 0.07f;
            const float frameDepth = 0.10f;

            GameObject windowRoot = new GameObject("HouseLayout_Window");
            windowRoot.transform.SetParent(c.root, true);
            windowRoot.transform.position = Vector3.zero;

            GameObject glass = BoxVisual(c, "WindowGlass", center, size, new Color(0.36f, 0.64f, 0.80f, 0.34f), windowRoot.transform);
            if (glass != null && c.runtime)
            {
                glass.AddComponent<WindowView>().Configure();
                glass.AddComponent<BreakableWindow>().Configure("house-window-real", c.groundMotion, c.logger);
            }

            Color frame = new Color(0.08f, 0.10f, 0.12f);
            BoxVisual(c, "WindowTop", center + new Vector3(0f, size.y * 0.5f + frameWidth * 0.5f, 0f),
                new Vector3(size.x + frameWidth * 2f, frameWidth, frameDepth), frame, windowRoot.transform);
            BoxVisual(c, "WindowBottom", center + new Vector3(0f, -size.y * 0.5f - frameWidth * 0.5f, 0f),
                new Vector3(size.x + frameWidth * 2f, frameWidth, frameDepth), frame, windowRoot.transform);
            BoxVisual(c, "WindowLeft", center + new Vector3(-size.x * 0.5f - frameWidth * 0.5f, 0f, 0f),
                new Vector3(frameWidth, size.y, frameDepth), frame, windowRoot.transform);
            BoxVisual(c, "WindowRight", center + new Vector3(size.x * 0.5f + frameWidth * 0.5f, 0f, 0f),
                new Vector3(frameWidth, size.y, frameDepth), frame, windowRoot.transform);
            BoxVisual(c, "WindowMullion", center,
                new Vector3(0.045f, size.y, frameDepth), frame, windowRoot.transform);
        }

        private static void BuildLighting(BuildContext c)
        {
            AddWarmLight(c, "LivingWarmLight", new Vector3(-3.25f, 3.45f, 0.60f), 1.10f, 5.0f);
            AddWarmLight(c, "DiningWarmLight", new Vector3(-1.90f, 3.45f, -2.70f), 0.90f, 4.4f);
            AddWarmLight(c, "KitchenWarmLight", new Vector3(3.40f, 3.45f, 5.40f), 0.95f, 4.8f);
            AddWarmLight(c, "BedroomWarmLight", new Vector3(5.20f, 5.55f, 0.75f), 0.72f, 3.2f);
        }

        private static GameObject Team(BuildContext c, string resource, Vector3 bottomCenter,
            Vector3 desiredSize, float yaw, bool collider)
        {
            GameObject prefab = Resources.Load<GameObject>("PlengFurniture/" + resource);
            if (prefab == null)
            {
                Debug.LogWarning("CEVR House layout missing Pleng furniture: " + resource);
                return null;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab);
            go.name = "HouseLayout_" + resource;
            go.transform.SetParent(c.root, true);

            foreach (Collider imported in go.GetComponentsInChildren<Collider>(true))
                imported.enabled = false;
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                if (HouseFurnitureGeometry.IsCollisionVisual(renderer.transform, go.transform))
                    renderer.enabled = false;

            ConvertMaterials(go);

            if (!HouseFurnitureGeometry.FitAndPlace(go.transform, desiredSize, bottomCenter, yaw))
            {
                SafeDestroy(go);
                return null;
            }

            if (collider && c.runtime) AddBoundsCollider(go);
            return go;
        }

        private static GameObject Kenney(BuildContext c, string resource, Vector3 bottomCenter,
            Vector3 desiredSize, float yaw, bool collider)
        {
            GameObject template = GetKenneyTemplate(c, resource);
            if (template == null) return null;

            GameObject go = UnityEngine.Object.Instantiate(template);
            go.name = "HouseLayout_" + resource;
            go.SetActive(true);
            go.transform.SetParent(c.root, true);

            if (!HouseFurnitureGeometry.FitAndPlace(go.transform, desiredSize, bottomCenter, yaw))
            {
                SafeDestroy(go);
                return null;
            }

            if (collider && c.runtime) AddBoundsCollider(go);
            return go;
        }

        private static GameObject GetKenneyTemplate(BuildContext c, string resource)
        {
            if (c.templates.TryGetValue(resource, out GameObject cached) && cached != null) return cached;

            TextAsset source = Resources.Load<TextAsset>("Furniture/" + resource);
            if (source == null)
            {
                Debug.LogWarning("CEVR House layout missing furniture resource: " + resource);
                return null;
            }

            ModelData data = JsonUtility.FromJson<ModelData>(source.text);
            if (data == null || data.vertices == null || data.parts == null) return null;

            GameObject template = new GameObject("HouseLayoutTemplate_" + resource);
            template.transform.SetParent(c.root, false);
            template.SetActive(false);

            Shader shader = LitShader();
            foreach (PartData part in data.parts)
            {
                if (part == null || part.triangles == null || part.color == null || part.color.Length < 3) continue;

                Vector3[] vertices = new Vector3[part.triangles.Length];
                int[] triangles = new int[vertices.Length];
                bool valid = true;
                for (int i = 0; i < vertices.Length; i++)
                {
                    int v = part.triangles[i] * 3;
                    if (v < 0 || v + 2 >= data.vertices.Length)
                    {
                        valid = false;
                        break;
                    }
                    vertices[i] = new Vector3(data.vertices[v], data.vertices[v + 1], data.vertices[v + 2]);
                    triangles[i] = i;
                }
                if (!valid) continue;

                Mesh mesh = new Mesh { name = "HouseLayoutMesh_" + resource };
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                Material material = new Material(shader)
                {
                    name = "HouseLayoutMaterial_" + resource,
                    color = new Color(part.color[0], part.color[1], part.color[2])
                };
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.18f);

                GameObject surface = new GameObject("Surface");
                surface.transform.SetParent(template.transform, false);
                surface.AddComponent<MeshFilter>().sharedMesh = mesh;
                surface.AddComponent<MeshRenderer>().sharedMaterial = material;
            }

            c.templates[resource] = template;
            return template;
        }

        private static GameObject BoxVisual(BuildContext c, string name, Vector3 center, Vector3 size,
            Color color, Transform parentOverride = null)
        {
            GameObject go = new GameObject("HouseLayout_" + name);
            go.transform.SetParent(parentOverride != null ? parentOverride : c.root, true);
            go.transform.position = center;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = size;

            Mesh mesh = CreateUnitCubeMesh();
            Material material = new Material(LitShader()) { name = "HouseLayout_" + name + "_Mat", color = color };
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return go;
        }

        private static void AddWarmLight(BuildContext c, string name, Vector3 position, float intensity, float range)
        {
            GameObject go = new GameObject("HouseLayout_" + name);
            go.transform.SetParent(c.root, true);
            go.transform.position = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.88f, 0.74f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void VerifyBedAgainstStairs(Scene scene, GameObject bed)
        {
            if (!HouseFurnitureGeometry.TryVisibleBounds(bed.transform, out Bounds bedBounds)) return;
            GameObject stairsObject = FindSceneObject(scene, "Stairs (1)");
            Collider stairs = stairsObject == null ? null : stairsObject.GetComponent<Collider>();
            if (stairs == null) return;

            Bounds stairBounds = stairs.bounds;
            bool xzOverlap = bedBounds.max.x > stairBounds.min.x && bedBounds.min.x < stairBounds.max.x &&
                             bedBounds.max.z > stairBounds.min.z && bedBounds.min.z < stairBounds.max.z;
            bool yOverlap = bedBounds.max.y > stairBounds.min.y && bedBounds.min.y < stairBounds.max.y + 0.25f;
            if (!xzOverlap || !yOverlap)
            {
                Debug.Log($"CEVR House bed clear of Stairs (1): {bedBounds.min} .. {bedBounds.max}");
                return;
            }

            // Last-resort compact fallback entirely inside the measured clear strip.
            HouseFurnitureGeometry.FitAndPlace(
                bed.transform,
                new Vector3(1.05f, 0.70f, 1.65f),
                new Vector3(5.25f, HouseSceneLayout.SecondFloorY, 0.62f),
                HouseSceneLayout.BedYaw);
            Debug.LogWarning("CEVR House bed intersected Stairs (1); compact clear-strip fallback applied.");
        }

        private static void ConfigureChairAnchor(Scene scene, string name, Vector3 position, float yaw)
        {
            GameObject chair = FindSceneObject(scene, name);
            if (chair == null) return;
            chair.transform.localScale = new Vector3(0.52f, 0.98f, 0.52f);
            chair.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            SetRenderers(chair, false);
        }

        private static void SetRenderers(GameObject go, bool enabled)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = enabled;
        }

        private static GameObject FindSceneObject(Scene scene, string exactName)
        {
            foreach (GameObject go in FindSceneObjects(scene))
                if (go.name == exactName) return go;
            return null;
        }

        private static List<GameObject> FindSceneObjects(Scene scene)
        {
            var result = new List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                result.Add(root);
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.gameObject != root) result.Add(t.gameObject);
            }
            return result;
        }

        private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }

        private static void ConvertMaterials(GameObject root)
        {
            Shader shader = LitShader();
            var cache = new Dictionary<Material, Material>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material original = slots[i];
                    if (original != null && cache.TryGetValue(original, out Material existing))
                    {
                        slots[i] = existing;
                        continue;
                    }

                    Color color = original != null && original.HasProperty("_BaseColor")
                        ? original.GetColor("_BaseColor")
                        : original != null && original.HasProperty("_Color")
                            ? original.color
                            : new Color(0.72f, 0.72f, 0.72f);

                    Material converted = new Material(shader)
                    {
                        name = "HouseLayout_" + (original == null ? "Material" : original.name),
                        color = color,
                        mainTexture = original == null ? null : original.mainTexture
                    };
                    if (converted.HasProperty("_Smoothness")) converted.SetFloat("_Smoothness", 0.18f);
                    if (converted.HasProperty("_Glossiness")) converted.SetFloat("_Glossiness", 0.18f);
                    if (original != null) cache[original] = converted;
                    slots[i] = converted;
                }
                renderer.sharedMaterials = slots;
            }
        }

        private static void AddBoundsCollider(GameObject go)
        {
            if (!HouseFurnitureGeometry.TryVisibleBounds(go.transform, out Bounds world)) return;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(world.center);
            Vector3 lossy = go.transform.lossyScale;
            box.size = new Vector3(
                world.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                world.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                world.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));
        }

        private static Shader LitShader()
        {
            return Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard"
                : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
        }

        private static Mesh CreateUnitCubeMesh()
        {
            Vector3[] vertices =
            {
                new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
                new Vector3(-.5f,-.5f,.5f),  new Vector3(.5f,-.5f,.5f),  new Vector3(.5f,.5f,.5f),  new Vector3(-.5f,.5f,.5f),
                new Vector3(-.5f,-.5f,-.5f), new Vector3(-.5f,.5f,-.5f), new Vector3(-.5f,.5f,.5f), new Vector3(-.5f,-.5f,.5f),
                new Vector3(.5f,-.5f,-.5f),  new Vector3(.5f,.5f,-.5f),  new Vector3(.5f,.5f,.5f),  new Vector3(.5f,-.5f,.5f),
                new Vector3(-.5f,.5f,-.5f),  new Vector3(.5f,.5f,-.5f),  new Vector3(.5f,.5f,.5f),  new Vector3(-.5f,.5f,.5f),
                new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,-.5f,.5f), new Vector3(-.5f,-.5f,.5f)
            };
            int[] triangles =
            {
                0,2,1,0,3,2, 4,5,6,4,6,7, 8,10,9,8,11,10,
                12,13,14,12,14,15, 16,18,17,16,19,18, 20,21,22,20,22,23
            };
            Mesh mesh = new Mesh { name = "HouseLayoutCube" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
#if UNITY_EDITOR
            else UnityEngine.Object.DestroyImmediate(obj);
#endif
        }
    }
}
