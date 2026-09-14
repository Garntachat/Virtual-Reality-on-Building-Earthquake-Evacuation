using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Final house-only visual stabilization pass.
    ///
    /// Some contributed FBX furniture contains authored offsets/orientations that do not map
    /// reliably into the generated House coordinate system. The result can be floating, tiny,
    /// stretched or wall-mounted-looking furniture. For the House scene we therefore preserve
    /// every tested gameplay object/collider/script, remove only unstable TeamFurniture visuals,
    /// and draw deterministic Kenney/primitive furniture on the same gameplay anchors.
    /// </summary>
    [DefaultExecutionOrder(30000)]
    public sealed class HouseFurnitureStabilityPass : MonoBehaviour
    {
        [Serializable] private class ModelData { public float[] vertices; public PartData[] parts; }
        [Serializable] private class PartData { public float[] color; public int[] triangles; }

        private const string RootName = "CEVR_UniversalGameplay";
        private readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private Transform root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (FindFirstObjectByType<HouseFurnitureStabilityPass>() != null) return;
            new GameObject("CEVR_HouseFurnitureStabilityPass").AddComponent<HouseFurnitureStabilityPass>();
        }

        private IEnumerator Start()
        {
            // Allow the normal gameplay bootstrap, furniture dressing and layout guard to finish.
            yield return null;
            yield return null;

            GameObject gameplay = GameObject.Find(RootName);
            if (gameplay == null)
            {
                Debug.LogWarning("CEVR house furniture stability pass could not find gameplay root.");
                yield break;
            }
            root = gameplay.transform;

            RemoveUnstableTeamFurniture();
            StabilizeGameplayFurniture();
            BuildStableRoomDressing();
            Debug.Log("CEVR house furniture stability pass complete: unstable FBX visuals removed and deterministic furniture placed on preserved gameplay anchors.");
        }

        private void RemoveUnstableTeamFurniture()
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var destroy = new List<GameObject>();
            foreach (Transform candidate in transforms)
            {
                if (candidate == null || !candidate.name.StartsWith("TeamFurniture_", StringComparison.Ordinal)) continue;
                destroy.Add(candidate.gameObject);
            }
            foreach (GameObject item in destroy)
                if (item != null) Destroy(item);
        }

        private void StabilizeGameplayFurniture()
        {
            foreach (MovableFurniture chair in FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None))
            {
                if (!chair.name.StartsWith("HouseChair_", StringComparison.Ordinal)) continue;
                HideDirectPlaceholderRenderers(chair.gameObject, "Stable_");
                CreateAnchoredModel(chair.gameObject, "chairCushion", new Vector3(0.62f, 0.98f, 0.62f),
                    chair.transform.position, chair.transform.rotation, true);
            }

            GameObject table = GameObject.Find("HouseSturdyTableTop");
            if (table != null)
            {
                HideDirectPlaceholderRenderers(table, "Stable_");
                CreateAnchoredModel(table, "table", new Vector3(3.40f, 0.95f, 1.80f),
                    new Vector3(table.transform.position.x, 0f, table.transform.position.z), table.transform.rotation, false);
            }

            GameObject fridge = GameObject.Find("HouseTallCabinet_Left");
            if (fridge != null)
            {
                HideDirectPlaceholderRenderers(fridge, "Stable_");
                CreateAnchoredModel(fridge, "kitchenFridge", new Vector3(1.05f, 2.30f, 0.72f),
                    new Vector3(fridge.transform.position.x, 0f, fridge.transform.position.z), fridge.transform.rotation, false);
            }

            GameObject wardrobe = GameObject.Find("HouseBookcase_Right");
            if (wardrobe != null)
            {
                HideDirectPlaceholderRenderers(wardrobe, "Stable_");
                CreateAnchoredModel(wardrobe, "bookcaseOpen", new Vector3(1.05f, 2.30f, 0.72f),
                    new Vector3(wardrobe.transform.position.x, 0f, wardrobe.transform.position.z), wardrobe.transform.rotation, false);
            }

            GameObject pillow = GameObject.Find("ProtectivePillow");
            if (pillow != null)
            {
                HideDirectPlaceholderRenderers(pillow, "Stable_");
                CreateAnchoredModel(pillow, "pillowBlue", new Vector3(0.78f, 0.20f, 0.50f),
                    pillow.transform.position - Vector3.up * 0.10f, pillow.transform.rotation, false);
            }
        }

        private void BuildStableRoomDressing()
        {
            // Remove legacy placeholder visuals that can overlap the replacement room dressing.
            HideByPrefix("HouseSofa", "HousePlant", "HouseShelfBook", "HousePhoto", "HouseRug");

            CreateStandaloneModel("Stable_HouseSofa", "loungeSofaLong",
                new Vector3(-3.50f, 0f, -4.60f), new Vector3(2.20f, 1.00f, 0.90f), Quaternion.identity, true);
            CreateStandaloneModel("Stable_HouseCoffeeTable", "tableCoffee",
                new Vector3(-3.45f, 0f, -3.25f), new Vector3(1.70f, 0.48f, 0.90f), Quaternion.identity, true);
            CreateStandaloneModel("Stable_HouseRug", "rugRectangle",
                new Vector3(-3.45f, 0.015f, -3.70f), new Vector3(3.00f, 0.025f, 2.50f), Quaternion.identity, false);
            CreateStandaloneModel("Stable_HousePlant", "pottedPlant",
                new Vector3(4.60f, 0f, -4.80f), new Vector3(0.70f, 1.35f, 0.70f), Quaternion.identity, true);
            CreateStandaloneModel("Stable_HouseTvCabinet", "cabinetTelevision",
                new Vector3(-3.50f, 0f, -1.85f), new Vector3(2.00f, 0.65f, 0.50f), Quaternion.Euler(0f, 180f, 0f), true);
            CreateStandaloneModel("Stable_HouseTelevision", "televisionModern",
                new Vector3(-3.50f, 0.65f, -1.85f), new Vector3(1.45f, 0.85f, 0.22f), Quaternion.Euler(0f, 180f, 0f), false);

            // Existing DressHouse already creates kitchen modules, but ensure deterministic instances
            // in case a previous overlapping visual was hidden/destroyed.
            EnsureNamedStandalone("Stable_HouseKitchenCabinet", "kitchenCabinet",
                new Vector3(2.25f, 0f, 1.15f), new Vector3(1.10f, 0.90f, 0.65f), Quaternion.identity, true);
            EnsureNamedStandalone("Stable_HouseKitchenSink", "kitchenSink",
                new Vector3(3.35f, 0f, 1.15f), new Vector3(1.10f, 0.90f, 0.65f), Quaternion.identity, true);
            EnsureNamedStandalone("Stable_HouseKitchenStove", "kitchenStove",
                new Vector3(4.45f, 0f, 1.15f), new Vector3(0.90f, 0.90f, 0.65f), Quaternion.identity, true);

            BuildSimpleBed();
            BuildSimpleVase();
        }

        private void BuildSimpleBed()
        {
            GameObject bed = new GameObject("Stable_HouseBed");
            bed.transform.SetParent(root, false);
            bed.transform.position = new Vector3(-3.80f, 0f, 4.55f);

            Material frame = SolidMaterial("Stable Bed Frame", new Color(0.32f, 0.18f, 0.10f));
            Material mattress = SolidMaterial("Stable Mattress", new Color(0.87f, 0.88f, 0.90f));
            Material blanket = SolidMaterial("Stable Blanket", new Color(0.16f, 0.38f, 0.58f));

            Part("Frame", bed.transform, new Vector3(0f, 0.23f, 0f), new Vector3(1.70f, 0.32f, 2.15f), frame, true);
            Part("Mattress", bed.transform, new Vector3(0f, 0.48f, 0f), new Vector3(1.62f, 0.24f, 2.05f), mattress, false);
            Part("Blanket", bed.transform, new Vector3(0f, 0.625f, -0.20f), new Vector3(1.56f, 0.06f, 1.35f), blanket, false);
            Part("Headboard", bed.transform, new Vector3(0f, 0.72f, 1.02f), new Vector3(1.70f, 0.88f, 0.12f), frame, true);

            GameObject visualPillow = CreateStandaloneModel("Stable_HouseBedPillow", "pillowBlue",
                new Vector3(-3.80f, 0.63f, 5.12f), new Vector3(0.72f, 0.18f, 0.42f), Quaternion.identity, false);
            if (visualPillow == null)
                Part("Pillow", bed.transform, new Vector3(0f, 0.67f, 0.58f), new Vector3(0.70f, 0.16f, 0.40f), mattress, false);
        }

        private void BuildSimpleVase()
        {
            GameObject vase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vase.name = "Stable_HouseVase";
            vase.transform.SetParent(root, false);
            vase.transform.position = new Vector3(-3.45f, 0.64f, -3.25f);
            vase.transform.localScale = new Vector3(0.22f, 0.28f, 0.22f);
            Renderer renderer = vase.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = SolidMaterial("Stable Vase", new Color(0.56f, 0.23f, 0.17f));
            Collider collider = vase.GetComponent<Collider>();
            if (collider != null) collider.enabled = true;
            Rigidbody body = vase.AddComponent<Rigidbody>();
            body.mass = 1.1f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            GroundMotionPlayer motion = FindFirstObjectByType<GroundMotionPlayer>();
            vase.AddComponent<InertialRigidbody>().Configure(motion, 1.05f, true);
            vase.AddComponent<FurnitureImpactAudio>();
        }

        private GameObject CreateAnchoredModel(
            GameObject target, string modelName, Vector3 dimensions, Vector3 bottomCenter,
            Quaternion rotation, bool bottomAtTarget)
        {
            if (target == null) return null;
            return CreateModel("Stable_" + modelName, modelName, target.transform,
                bottomAtTarget ? target.transform.position : bottomCenter, dimensions, rotation);
        }

        private GameObject CreateStandaloneModel(
            string name, string modelName, Vector3 bottomCenter, Vector3 dimensions,
            Quaternion rotation, bool collider)
        {
            if (GameObject.Find(name) != null) return GameObject.Find(name);
            GameObject model = CreateModel(name, modelName, root, bottomCenter, dimensions, rotation);
            if (model != null && collider) AddBoundsCollider(model);
            return model;
        }

        private void EnsureNamedStandalone(
            string name, string modelName, Vector3 bottomCenter, Vector3 dimensions,
            Quaternion rotation, bool collider)
        {
            if (GameObject.Find(name) == null)
                CreateStandaloneModel(name, modelName, bottomCenter, dimensions, rotation, collider);
        }

        private GameObject CreateModel(
            string instanceName, string modelName, Transform parent, Vector3 bottomCenter,
            Vector3 dimensions, Quaternion rotation)
        {
            GameObject template = GetTemplate(modelName);
            if (template == null) return null;

            GameObject instance = Instantiate(template);
            instance.name = instanceName;
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(Vector3.zero, rotation);
            instance.transform.localScale = Vector3.one;
            instance.SetActive(true);

            if (!TryBounds(instance, out Bounds source) || source.size.x < 0.001f || source.size.y < 0.001f || source.size.z < 0.001f)
            {
                Destroy(instance);
                return null;
            }
            instance.transform.localScale = new Vector3(
                dimensions.x / source.size.x,
                dimensions.y / source.size.y,
                dimensions.z / source.size.z);
            if (!TryBounds(instance, out Bounds placed))
            {
                Destroy(instance);
                return null;
            }
            instance.transform.position += bottomCenter - new Vector3(placed.center.x, placed.min.y, placed.center.z);
            return instance;
        }

        private GameObject GetTemplate(string modelName)
        {
            if (templates.TryGetValue(modelName, out GameObject cached) && cached != null) return cached;
            TextAsset source = Resources.Load<TextAsset>("Furniture/" + modelName);
            if (source == null)
            {
                Debug.LogWarning("CEVR stable house furniture mesh missing: " + modelName);
                return null;
            }
            ModelData data = JsonUtility.FromJson<ModelData>(source.text);
            if (data == null || data.vertices == null || data.parts == null) return null;

            GameObject template = new GameObject("StableTemplate_" + modelName);
            template.transform.SetParent(transform, false);
            template.SetActive(false);

            foreach (PartData part in data.parts)
            {
                if (part == null || part.triangles == null || part.color == null || part.color.Length < 3) continue;
                Vector3[] vertices = new Vector3[part.triangles.Length];
                int[] indices = new int[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    int v = part.triangles[i] * 3;
                    if (v < 0 || v + 2 >= data.vertices.Length) continue;
                    vertices[i] = new Vector3(data.vertices[v], data.vertices[v + 1], data.vertices[v + 2]);
                    indices[i] = i;
                }
                Mesh mesh = new Mesh { name = "StableMesh_" + modelName };
                mesh.vertices = vertices;
                mesh.triangles = indices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                owned.Add(mesh);

                Material material = SolidMaterial("StableMat_" + modelName,
                    new Color(part.color[0], part.color[1], part.color[2]));
                GameObject surface = new GameObject("StableSurface");
                surface.transform.SetParent(template.transform, false);
                surface.AddComponent<MeshFilter>().sharedMesh = mesh;
                surface.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            templates[modelName] = template;
            return template;
        }

        private Material SolidMaterial(string name, Color color)
        {
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader == null) return null;
            Material material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.12f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
            owned.Add(material);
            return material;
        }

        private static void Part(
            string name, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, bool collider)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null) partCollider.enabled = collider;
        }

        private static void HideDirectPlaceholderRenderers(GameObject target, string keepPrefix)
        {
            if (target == null) return;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                if (renderer.name.StartsWith(keepPrefix, StringComparison.Ordinal)) continue;
                if (renderer.name.IndexOf("WarningStripe", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                renderer.enabled = false;
            }
        }

        private static void HideByPrefix(params string[] prefixes)
        {
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                foreach (string prefix in prefixes)
                {
                    if (!renderer.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    renderer.enabled = false;
                    break;
                }
            }
        }

        private static bool TryBounds(GameObject rootObject, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in rootObject.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static void AddBoundsCollider(GameObject rootObject)
        {
            if (!TryBounds(rootObject, out Bounds worldBounds)) return;
            Vector3 localCenter = rootObject.transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = rootObject.transform.InverseTransformVector(worldBounds.size);
            BoxCollider collider = rootObject.AddComponent<BoxCollider>();
            collider.center = localCenter;
            collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        private void OnDestroy()
        {
            foreach (UnityEngine.Object item in owned)
                if (item != null) Destroy(item);
            owned.Clear();
        }
    }
}
