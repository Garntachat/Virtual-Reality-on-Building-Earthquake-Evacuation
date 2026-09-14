using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Single authoritative House furniture layout.
    /// Uses the tested JSON furniture under Resources/Furniture and keeps gameplay physics on
    /// separate original anchors. No competing House furniture pass should reposition these.
    /// </summary>
    [DefaultExecutionOrder(-5000)]
    public sealed class HouseResourceFurnitureLayout : MonoBehaviour
    {
        [Serializable] private sealed class ModelData { public float[] vertices; public PartData[] parts; }
        [Serializable] private sealed class PartData { public float[] color; public int[] triangles; }

        private sealed class FollowVisual
        {
            public Transform anchor;
            public Transform visual;
            public Vector3 positionOffset;
            public Quaternion rotationOffset;
        }

        private readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly List<FollowVisual> followers = new List<FollowVisual>();
        private Transform root;
        private GroundMotionPlayer motion;
        private SessionLogger logger;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (FindFirstObjectByType<HouseResourceFurnitureLayout>() != null) return;
            new GameObject("CEVR_HouseResourceFurnitureLayout").AddComponent<HouseResourceFurnitureLayout>();
        }

        private void Start()
        {
            // FurnitureSceneDressing is still used by Tutorial, but its House branch previously
            // mixed several replacement systems. Disable it here before its default-order Start().
            FurnitureSceneDressing legacy = FindFirstObjectByType<FurnitureSceneDressing>();
            if (legacy != null) legacy.enabled = false;

            GameObject gameplay = GameObject.Find("CEVR_UniversalGameplay");
            if (gameplay == null)
            {
                Debug.LogError("CEVR House resource layout could not find CEVR_UniversalGameplay.");
                return;
            }
            root = gameplay.transform;
            motion = FindFirstObjectByType<GroundMotionPlayer>();
            logger = FindFirstObjectByType<SessionLogger>();

            RemoveOldFurnitureVisuals();
            BuildGameplayFurniture();
            BuildLivingRoom();
            BuildKitchen();
            BuildBedroom();
            RebuildWindowAtOpening();

            Debug.Log("CEVR HOUSE RESOURCE LAYOUT READY: resource furniture placed at unique room coordinates; legacy furniture dressing disabled for House.");
        }

        private void LateUpdate()
        {
            // Visuals for movable/toppling gameplay objects are deliberately detached from scaled
            // primitive anchors. Following in world space prevents inherited-scale distortion.
            for (int i = followers.Count - 1; i >= 0; i--)
            {
                FollowVisual f = followers[i];
                if (f.anchor == null || f.visual == null) { followers.RemoveAt(i); continue; }
                f.visual.position = f.anchor.TransformPoint(f.positionOffset);
                f.visual.rotation = f.anchor.rotation * f.rotationOffset;
            }
        }

        private void RemoveOldFurnitureVisuals()
        {
            var destroy = new List<GameObject>();
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null || t == transform || t == root) continue;
                string n = t.name;
                if (n.StartsWith("TeamFurniture_", StringComparison.Ordinal) ||
                    n.StartsWith("Kenney_", StringComparison.Ordinal) ||
                    n.StartsWith("FurnitureTemplate_", StringComparison.Ordinal) ||
                    n.StartsWith("Stable_", StringComparison.Ordinal) ||
                    n.StartsWith("V2_", StringComparison.Ordinal) ||
                    n.StartsWith("Final_", StringComparison.Ordinal))
                    destroy.Add(t.gameObject);
            }
            foreach (GameObject go in destroy) if (go != null) Destroy(go);

            HideLegacyRenderers("HouseSofa", "HouseCoffeeTable", "HouseRug", "HousePlant", "HouseShelfBook", "HousePhoto");
        }

        private void BuildGameplayFurniture()
        {
            BuildFollower("HouseChair_CoverObstacle", "chairCushion", new Vector3(0.62f, 0.98f, 0.62f),
                new Vector3(0f, 0f, -4.15f), 0f);
            BuildFollower("HouseChair_DiningLeft", "chairCushion", new Vector3(0.62f, 0.98f, 0.62f),
                new Vector3(-1.65f, 0f, -2.70f), 90f);
            BuildFollower("HouseChair_DiningRight", "chairCushion", new Vector3(0.62f, 0.98f, 0.62f),
                new Vector3(1.65f, 0f, -2.70f), -90f);
            BuildFollower("HouseChair_Spare", "chairCushion", new Vector3(0.62f, 0.98f, 0.62f),
                new Vector3(3.55f, 0f, -4.55f), -35f);

            GameObject table = GameObject.Find("HouseSturdyTableTop");
            if (table != null)
            {
                table.transform.position = new Vector3(0f, 0.90f, -2.70f);
                HideAllRenderers(table);
                BuildStatic("table", new Vector3(0f, 0f, -2.70f), new Vector3(3.30f, 0.94f, 1.72f), 0f, true);
            }

            BuildFollower("HouseTallCabinet_Left", "kitchenFridge", new Vector3(1.05f, 2.30f, 0.72f),
                new Vector3(-4.35f, 0f, 0.30f), 0f);
            BuildFollower("HouseBookcase_Right", "bookcaseOpen", new Vector3(1.05f, 2.30f, 0.72f),
                new Vector3(4.35f, 0f, 0.45f), 180f);
            BuildFollower("ProtectivePillow", "pillowBlue", new Vector3(0.78f, 0.20f, 0.50f),
                new Vector3(-3.65f, 0.68f, 4.35f), 0f);
        }

        private void BuildLivingRoom()
        {
            BuildStatic("loungeSofaLong", new Vector3(-3.45f, 0f, -4.65f), new Vector3(2.25f, 1.00f, 0.95f), 0f, true);
            BuildStatic("tableCoffee", new Vector3(-3.45f, 0f, -3.30f), new Vector3(1.55f, 0.48f, 0.82f), 0f, true);
            BuildStatic("rugRectangle", new Vector3(-3.45f, 0.012f, -3.65f), new Vector3(2.80f, 0.025f, 1.95f), 0f, false);
            BuildStatic("pottedPlant", new Vector3(-4.70f, 0f, -2.95f), new Vector3(0.62f, 1.25f, 0.62f), 0f, true);
            BuildStatic("cabinetTelevision", new Vector3(-3.45f, 0f, -1.65f), new Vector3(1.85f, 0.65f, 0.50f), 180f, true);
            BuildStatic("televisionModern", new Vector3(-3.45f, 0.66f, -1.65f), new Vector3(1.35f, 0.78f, 0.20f), 180f, false);
            BuildStatic("lampRoundFloor", new Vector3(-4.75f, 0f, -4.80f), new Vector3(0.42f, 1.50f, 0.42f), 0f, false);
        }

        private void BuildKitchen()
        {
            // Right side kitchen, deliberately separated from living-room and dining coordinates.
            BuildStatic("kitchenCabinet", new Vector3(2.15f, 0f, 1.10f), new Vector3(1.00f, 0.90f, 0.65f), 0f, true);
            BuildStatic("kitchenSink", new Vector3(3.20f, 0f, 1.10f), new Vector3(1.00f, 0.90f, 0.65f), 0f, true);
            BuildStatic("kitchenStove", new Vector3(4.25f, 0f, 1.10f), new Vector3(0.90f, 0.90f, 0.65f), 0f, true);
        }

        private void BuildBedroom()
        {
            // There is no bed JSON in Resources/Furniture. Build a clean bed here, but use the
            // resource pillow. Nothing in this group shares a coordinate with living/dining items.
            Transform bed = new GameObject("ResourceHouse_Bed").transform;
            bed.SetParent(root, false);
            bed.position = new Vector3(-3.65f, 0f, 4.65f);
            Material frame = Solid("BedFrame", new Color(0.31f, 0.18f, 0.10f));
            Material mattress = Solid("Mattress", new Color(0.88f, 0.88f, 0.86f));
            Material blanket = Solid("Blanket", new Color(0.24f, 0.44f, 0.66f));
            Primitive("BedFrame", bed, new Vector3(0f, 0.22f, 0f), new Vector3(1.75f, 0.30f, 2.15f), frame, true);
            Primitive("Mattress", bed, new Vector3(0f, 0.48f, 0f), new Vector3(1.65f, 0.24f, 2.03f), mattress, false);
            Primitive("Blanket", bed, new Vector3(0f, 0.63f, -0.23f), new Vector3(1.58f, 0.06f, 1.30f), blanket, false);
            Primitive("Headboard", bed, new Vector3(0f, 0.73f, 1.04f), new Vector3(1.75f, 0.92f, 0.12f), frame, true);
            BuildStatic("pillowBlue", new Vector3(-3.65f, 0.66f, 5.20f), new Vector3(0.70f, 0.18f, 0.42f), 0f, false);
        }

        private void BuildFollower(string anchorName, string resourceName, Vector3 dimensions,
            Vector3 bottomCenter, float yaw)
        {
            GameObject anchorObject = GameObject.Find(anchorName);
            if (anchorObject == null) return;

            // Put gameplay anchor where intended but preserve its original Y convention.
            Vector3 current = anchorObject.transform.position;
            if (anchorName.StartsWith("HouseChair_", StringComparison.Ordinal))
                anchorObject.transform.SetPositionAndRotation(bottomCenter, Quaternion.Euler(0f, yaw, 0f));
            else if (anchorName == "HouseTallCabinet_Left" || anchorName == "HouseBookcase_Right")
                anchorObject.transform.SetPositionAndRotation(new Vector3(bottomCenter.x, 1.15f, bottomCenter.z), Quaternion.Euler(0f, yaw, 0f));
            else
                anchorObject.transform.position = new Vector3(bottomCenter.x, current.y, bottomCenter.z);

            HideAllRenderers(anchorObject);
            GameObject visual = BuildModel(resourceName, bottomCenter, dimensions, yaw, false);
            if (visual == null) return;

            // Store the visual's world transform relative to the gameplay anchor; future quake/grab
            // motion follows without inheriting the anchor's primitive scaling.
            followers.Add(new FollowVisual
            {
                anchor = anchorObject.transform,
                visual = visual.transform,
                positionOffset = anchorObject.transform.InverseTransformPoint(visual.transform.position),
                rotationOffset = Quaternion.Inverse(anchorObject.transform.rotation) * visual.transform.rotation
            });
        }

        private GameObject BuildStatic(string resourceName, Vector3 bottomCenter, Vector3 dimensions,
            float yaw, bool collider)
        {
            return BuildModel(resourceName, bottomCenter, dimensions, yaw, collider);
        }

        private GameObject BuildModel(string resourceName, Vector3 bottomCenter, Vector3 dimensions,
            float yaw, bool addCollider)
        {
            GameObject template = Template(resourceName);
            if (template == null) return null;

            GameObject instance = Instantiate(template);
            instance.name = "ResourceHouse_" + resourceName;
            instance.transform.SetParent(root, true);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, yaw, 0f));
            instance.transform.localScale = Vector3.one;
            instance.SetActive(true);

            if (!BoundsOf(instance, out Bounds source) || !Valid(source.size))
            {
                Destroy(instance);
                return null;
            }

            instance.transform.localScale = new Vector3(
                dimensions.x / source.size.x,
                dimensions.y / source.size.y,
                dimensions.z / source.size.z);

            if (!BoundsOf(instance, out Bounds scaled) || !Valid(scaled.size))
            {
                Destroy(instance);
                return null;
            }

            // Bounds alignment, not prefab pivot alignment: every resource sits on the requested
            // floor/top surface regardless of where its source mesh origin was authored.
            instance.transform.position += bottomCenter -
                new Vector3(scaled.center.x, scaled.min.y, scaled.center.z);

            if (addCollider) AddBoundsCollider(instance);
            return instance;
        }

        private GameObject Template(string resourceName)
        {
            if (templates.TryGetValue(resourceName, out GameObject existing) && existing != null) return existing;
            TextAsset source = Resources.Load<TextAsset>("Furniture/" + resourceName);
            if (source == null)
            {
                Debug.LogError("CEVR House missing furniture resource: Furniture/" + resourceName);
                return null;
            }

            ModelData data = JsonUtility.FromJson<ModelData>(source.text);
            if (data == null || data.vertices == null || data.parts == null)
            {
                Debug.LogError("CEVR House invalid furniture JSON: " + resourceName);
                return null;
            }

            GameObject template = new GameObject("ResourceTemplate_" + resourceName);
            template.transform.SetParent(transform, false);
            template.SetActive(false);
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");

            foreach (PartData part in data.parts)
            {
                if (part == null || part.triangles == null || part.color == null || part.color.Length < 3) continue;
                Vector3[] vertices = new Vector3[part.triangles.Length];
                int[] triangles = new int[vertices.Length];
                bool valid = true;
                for (int i = 0; i < vertices.Length; i++)
                {
                    int sourceIndex = part.triangles[i] * 3;
                    if (sourceIndex < 0 || sourceIndex + 2 >= data.vertices.Length) { valid = false; break; }
                    vertices[i] = new Vector3(data.vertices[sourceIndex], data.vertices[sourceIndex + 1], data.vertices[sourceIndex + 2]);
                    triangles[i] = i;
                }
                if (!valid) continue;

                Mesh mesh = new Mesh { name = "ResourceMesh_" + resourceName };
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                owned.Add(mesh);

                Material material = new Material(shader)
                {
                    name = "ResourceMat_" + resourceName,
                    color = new Color(part.color[0], part.color[1], part.color[2])
                };
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.15f);
                owned.Add(material);

                GameObject surface = new GameObject("Surface");
                surface.transform.SetParent(template.transform, false);
                surface.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            templates[resourceName] = template;
            return template;
        }

        private void RebuildWindowAtOpening()
        {
            // Remove hard-coded generated windows from UniversalSceneGameplayBootstrap.
            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go != null && go.name.StartsWith("HouseWindow_", StringComparison.Ordinal)) Destroy(go);

            Vector3 center = DetectWallOpening(out float wallZ);
            Material frame = Solid("WindowFrame", new Color(0.10f, 0.12f, 0.15f));
            Material glass = Transparent("WindowGlass", new Color(0.42f, 0.72f, 0.92f, 0.18f));

            GameObject pane = PrimitiveWorld("ResourceHouse_Window", new Vector3(center.x, center.y, wallZ - 0.025f),
                new Vector3(1.85f, 1.32f, 0.035f), glass, false);
            pane.AddComponent<BreakableWindow>().Configure("house-window-resource", motion, logger);
            pane.AddComponent<WindowView>().Configure();
            PrimitiveWorld("ResourceHouse_WindowTop", new Vector3(center.x, center.y + 0.70f, wallZ - 0.04f), new Vector3(2.05f, 0.08f, 0.10f), frame, false);
            PrimitiveWorld("ResourceHouse_WindowBottom", new Vector3(center.x, center.y - 0.70f, wallZ - 0.04f), new Vector3(2.05f, 0.08f, 0.10f), frame, false);
            PrimitiveWorld("ResourceHouse_WindowLeft", new Vector3(center.x - 0.99f, center.y, wallZ - 0.04f), new Vector3(0.08f, 1.48f, 0.10f), frame, false);
            PrimitiveWorld("ResourceHouse_WindowRight", new Vector3(center.x + 0.99f, center.y, wallZ - 0.04f), new Vector3(0.08f, 1.48f, 0.10f), frame, false);
            PrimitiveWorld("ResourceHouse_WindowMullion", new Vector3(center.x, center.y, wallZ - 0.05f), new Vector3(0.055f, 1.32f, 0.10f), frame, false);
        }

        private Vector3 DetectWallOpening(out float wallZ)
        {
            const float startZ = 0.35f;
            const float fallbackZ = 2.24f;
            Vector3 best = new Vector3(0f, 1.65f, fallbackZ);
            float bestScore = float.NegativeInfinity;
            float bestZ = fallbackZ;

            for (float y = 1.10f; y <= 2.20f; y += 0.10f)
            for (float x = -4.25f; x <= 4.25f; x += 0.10f)
            {
                if (HitsAuthoredHouse(new Vector3(x, y, startZ), out _)) continue;
                Vector2[] ring = { new Vector2(-1.02f, 0f), new Vector2(1.02f, 0f), new Vector2(0f, -0.76f), new Vector2(0f, 0.76f) };
                int count = 0;
                float z = 0f;
                foreach (Vector2 d in ring)
                {
                    if (!HitsAuthoredHouse(new Vector3(x + d.x, y + d.y, startZ), out float hitZ)) continue;
                    count++; z += hitZ;
                }
                if (count < 3) continue;
                float score = count * 10f - Mathf.Abs(y - 1.65f) * 2f - Mathf.Abs(x) * 0.05f;
                if (score <= bestScore) continue;
                bestScore = score;
                best = new Vector3(x, y, fallbackZ);
                bestZ = z / count;
            }

            wallZ = float.IsNegativeInfinity(bestScore) ? fallbackZ : bestZ;
            Debug.Log($"CEVR House window target = ({best.x:F2}, {best.y:F2}, {wallZ:F2})" +
                      (float.IsNegativeInfinity(bestScore) ? " [fallback]" : " [wall opening detected]"));
            return best;
        }

        private bool HitsAuthoredHouse(Vector3 origin, out float hitZ)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.forward, 5f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                Transform t = hit.collider.transform;
                if (t == root || t.IsChildOf(root)) continue;
                string n = t.name.ToLowerInvariant();
                if (n.Contains("floor") || n.Contains("ceiling") || n.Contains("furniture")) continue;
                hitZ = hit.point.z;
                return true;
            }
            hitZ = 0f;
            return false;
        }

        private static bool BoundsOf(GameObject go, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static bool Valid(Vector3 size)
        {
            return size.x > 0.0001f && size.y > 0.0001f && size.z > 0.0001f &&
                   !float.IsNaN(size.x) && !float.IsNaN(size.y) && !float.IsNaN(size.z);
        }

        private static void AddBoundsCollider(GameObject go)
        {
            if (!BoundsOf(go, out Bounds bounds)) return;
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            foreach (float x in new[] { bounds.min.x, bounds.max.x })
            foreach (float y in new[] { bounds.min.y, bounds.max.y })
            foreach (float z in new[] { bounds.min.z, bounds.max.z })
            {
                Vector3 local = go.transform.InverseTransformPoint(new Vector3(x, y, z));
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.center = (min + max) * 0.5f;
            collider.size = max - min;
        }

        private static void HideAllRenderers(GameObject go)
        {
            if (go == null) return;
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                if (renderer.name.IndexOf("WarningStripe", StringComparison.OrdinalIgnoreCase) < 0)
                    renderer.enabled = false;
        }

        private static void HideLegacyRenderers(params string[] prefixes)
        {
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            foreach (string prefix in prefixes)
                if (renderer.name.StartsWith(prefix, StringComparison.Ordinal)) { renderer.enabled = false; break; }
        }

        private Material Solid(string name, Color color)
        {
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.15f);
            owned.Add(material);
            return material;
        }

        private Material Transparent(string name, Color color)
        {
            Material material = Solid(name, color);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }

        private static GameObject PrimitiveWorld(string name, Vector3 position, Vector3 size, Material material, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = size;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider c = go.GetComponent<Collider>();
            if (c != null) c.enabled = collider;
            return go;
        }

        private static void Primitive(string name, Transform parent, Vector3 localPosition, Vector3 size,
            Material material, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider c = go.GetComponent<Collider>();
            if (c != null) c.enabled = collider;
        }

        private void OnDestroy()
        {
            foreach (UnityEngine.Object item in owned) if (item != null) Destroy(item);
            owned.Clear();
        }
    }
}
