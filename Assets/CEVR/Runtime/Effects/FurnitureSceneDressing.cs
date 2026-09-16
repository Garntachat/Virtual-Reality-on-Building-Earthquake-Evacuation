using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    public sealed class FurnitureSceneDressing : MonoBehaviour
    {
        [Serializable] private class ModelData { public float[] vertices; public PartData[] parts; }
        [Serializable] private class PartData { public float[] color; public int[] triangles; }
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        private readonly HashSet<string> missingTeamModels = new HashSet<string>();
        private const string TeamFurniturePath = "PlengFurniture/";

        private GameObject Model(string name, Transform parent, Vector3 position, Vector3 size, Quaternion rotation)
        {
            if (!templates.TryGetValue(name, out GameObject template))
            {
                TextAsset source = Resources.Load<TextAsset>("Furniture/" + name);
                if (source == null) { Debug.LogError("Missing furniture mesh: " + name); return null; }
                ModelData data = JsonUtility.FromJson<ModelData>(source.text);
                template = new GameObject("FurnitureTemplate_" + name);
                template.transform.SetParent(transform, false);
                template.SetActive(false);
                foreach (PartData part in data.parts)
                {
                    // Keep flat, authored faces without requiring an OBJ importer or texture download.
                    var vertices = new Vector3[part.triangles.Length];
                    var indices = new int[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        int v = part.triangles[i] * 3;
                        vertices[i] = new Vector3(data.vertices[v], data.vertices[v + 1], data.vertices[v + 2]);
                        indices[i] = i;
                    }
                    Mesh mesh = new Mesh { name = "Kenney_" + name };
                    mesh.vertices = vertices; mesh.triangles = indices;
                    mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh);
                    Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
                    Material material = new Material(shader) { color = new Color(part.color[0], part.color[1], part.color[2]) };
                    if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.12f);
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
                    owned.Add(material);
                    var surface = new GameObject("AuthoredSurface");
                    surface.transform.SetParent(template.transform, false);
                    surface.AddComponent<MeshFilter>().sharedMesh = mesh;
                    surface.AddComponent<MeshRenderer>().sharedMaterial = material;
                }
                templates[name] = template;
            }
            GameObject model = Instantiate(template);
            model.name = "Kenney_" + name;
            model.transform.SetPositionAndRotation(position, rotation);
            model.transform.localScale = size;
            model.transform.SetParent(parent, true);
            model.SetActive(true);
            return model;
        }

        private void Replace(GameObject target, string model, Vector3 dimensions, bool bottomAtPivot = false)
        {
            if (target == null || target.transform.Find("Kenney_" + model) != null) return;
            Vector3 position = target.transform.position;
            if (!bottomAtPivot) position -= Vector3.up * dimensions.y * 0.5f;
            // Instantiate successfully before hiding any visible fallback. Existing collision/physics stays intact.
            var old = target.GetComponentsInChildren<Renderer>();
            GameObject result = Model(model, target.transform, position, dimensions, target.transform.rotation);
            if (result == null) return;
            foreach (Renderer renderer in old) renderer.enabled = false;
        }

        private GameObject TeamModel(
            string name, Transform parent, Vector3 bottomCenter, Vector3 dimensions, Quaternion rotation)
        {
            GameObject source = Resources.Load<GameObject>(TeamFurniturePath + name);
            if (source == null)
            {
                if (missingTeamModels.Add(name))
                    Debug.LogWarning("CEVR team furniture unavailable: " + name + ". Keeping the tested fallback visual.");
                return null;
            }

            GameObject model = Instantiate(source);
            model.name = "TeamFurniture_" + name;
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;

            foreach (Collider importedCollider in model.GetComponentsInChildren<Collider>(true))
                importedCollider.enabled = false;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                if (IsCollisionVisual(renderer.transform, model.transform)) renderer.enabled = false;

            ConvertMaterialsForActivePipeline(model);
            if (!TryGetVisibleBounds(model, out Bounds sourceBounds) ||
                sourceBounds.size.x < 0.001f || sourceBounds.size.y < 0.001f || sourceBounds.size.z < 0.001f)
            {
                Debug.LogWarning("CEVR team furniture has no usable visible bounds: " + name + ". Keeping fallback visual.");
                Destroy(model);
                return null;
            }

            model.transform.localScale = new Vector3(
                dimensions.x / sourceBounds.size.x,
                dimensions.y / sourceBounds.size.y,
                dimensions.z / sourceBounds.size.z);
            model.transform.rotation = rotation;
            if (!TryGetVisibleBounds(model, out Bounds placedBounds))
            {
                Destroy(model);
                return null;
            }
            model.transform.position += bottomCenter -
                new Vector3(placedBounds.center.x, placedBounds.min.y, placedBounds.center.z);
            model.transform.SetParent(parent, true);
            return model;
        }

        private bool ReplaceWithTeamModel(
            GameObject target, string model, Vector3 dimensions, bool bottomAtPivot = false)
        {
            Vector3 bottom = target == null ? Vector3.zero : target.transform.position;
            if (!bottomAtPivot) bottom -= Vector3.up * dimensions.y * 0.5f;
            return ReplaceWithTeamModelAt(target, model, dimensions, bottom);
        }

        private bool ReplaceWithTeamModelAt(
            GameObject target, string model, Vector3 dimensions, Vector3 bottomCenter)
        {
            if (target == null) return false;
            if (target.transform.Find("TeamFurniture_" + model) != null) return true;
            Renderer[] old = target.GetComponentsInChildren<Renderer>(true);
            GameObject result = TeamModel(model, target.transform, bottomCenter, dimensions, target.transform.rotation);
            if (result == null) return false;
            // Gameplay colliders and scripts stay on the original object. Only its placeholder surfaces are hidden.
            foreach (Renderer renderer in old)
                if (renderer.name.IndexOf("WarningStripe", StringComparison.Ordinal) < 0) renderer.enabled = false;
            return true;
        }

        private GameObject TeamDecor(
            string model, Vector3 bottomCenter, Vector3 dimensions, float yaw = 0f, bool addCollider = false)
        {
            GameObject result = TeamModel(model, transform, bottomCenter, dimensions, Quaternion.Euler(0f, yaw, 0f));
            if (result != null && addCollider) AddBoundsCollider(result);
            return result;
        }

        private void CreateShakingVase(Vector3 bottomCenter)
        {
            GameObject vase = TeamDecor("Vase", bottomCenter, new Vector3(0.28f, 0.62f, 0.28f));
            if (vase == null) return;
            AddBoundsCollider(vase);
            Rigidbody body = vase.AddComponent<Rigidbody>();
            body.mass = 1.1f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            GroundMotionPlayer motion = FindFirstObjectByType<GroundMotionPlayer>();
            vase.AddComponent<InertialRigidbody>().Configure(motion, 1.05f, true);
            vase.AddComponent<FurnitureImpactAudio>();
        }

        private void ConvertMaterialsForActivePipeline(GameObject model)
        {
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            var converted = new Dictionary<Material, Material>();
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material original = slots[i];
                    if (original != null && converted.TryGetValue(original, out Material cached))
                    {
                        slots[i] = cached;
                        continue;
                    }
                    Color color = original != null && original.HasProperty("_BaseColor")
                        ? original.GetColor("_BaseColor")
                        : original != null && original.HasProperty("_Color") ? original.color : new Color(0.72f, 0.72f, 0.72f);
                    Material material = new Material(shader)
                    {
                        name = "CEVR " + (original == null ? "Team Furniture" : original.name),
                        color = color,
                        mainTexture = original == null ? null : original.mainTexture
                    };
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
                    if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.22f);
                    owned.Add(material);
                    if (original != null) converted[original] = material;
                    slots[i] = material;
                }
                renderer.sharedMaterials = slots;
            }
        }

        private static bool IsCollisionVisual(Transform candidate, Transform root)
        {
            Transform current = candidate;
            while (current != null)
            {
                if (current.name.IndexOf("Collision", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (current == root) break;
                current = current.parent;
            }
            return false;
        }

        private static bool TryGetVisibleBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || IsCollisionVisual(renderer.transform, root.transform)) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static void AddBoundsCollider(GameObject root)
        {
            foreach (Collider existing in root.GetComponents<Collider>())
                if (existing.enabled) return;
            if (!TryGetVisibleBounds(root, out Bounds worldBounds)) return;
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            foreach (float x in new[] { worldBounds.min.x, worldBounds.max.x })
            foreach (float y in new[] { worldBounds.min.y, worldBounds.max.y })
            foreach (float z in new[] { worldBounds.min.z, worldBounds.max.z })
            {
                Vector3 local = root.transform.InverseTransformPoint(new Vector3(x, y, z));
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = (min + max) * 0.5f;
            collider.size = max - min;
        }

        private void Start()
        {
            bool house = SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("house");
            foreach (MovableFurniture chair in FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None))
            {
                // Scale visual and existing compound collision together: ~60 cm seat, ~1 m overall height.
                chair.transform.localScale = Vector3.Scale(chair.transform.localScale, new Vector3(0.67f, 0.9f, 0.67f));
                if (!ReplaceWithTeamModel(chair.gameObject, "DiningChair", new Vector3(0.64f, 0.96f, 0.64f), true))
                    Replace(chair.gameObject, house ? "chairCushion" : "chairDesk", new Vector3(0.9f, 1.14f, 0.9f), true);
            }
            Replace(GameObject.Find("TaskItem_circuit-module"), "laptop", new Vector3(0.42f, 0.18f, 0.3f));
            Replace(GameObject.Find("TaskItem_safety-canister"), "books", new Vector3(0.25f, 0.35f, 0.25f));
            GameObject protectivePillow = GameObject.Find("ProtectivePillow");
            if (!ReplaceWithTeamModel(protectivePillow, "Bed_Pillow", new Vector3(0.78f, 0.20f, 0.50f)))
                Replace(protectivePillow, "pillowBlue", new Vector3(0.9f, 0.24f, 0.64f));
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r.name == "LabBenchTop")
                {
                    Replace(r.gameObject, "desk", new Vector3(4f, 0.89f, 1.2f));
                    Transform model = r.transform.Find("Kenney_desk");
                    if (model != null) model.position = new Vector3(r.transform.position.x, 0, r.transform.position.z);
                }
                if (r.name == "LabBenchLeg" || r.name == "SturdyTableLeg" || r.name == "HouseSturdyTableLeg") r.enabled = false;
                if (r.name.StartsWith("StagedFallingHazard_") || r.name.StartsWith("HouseFallingObject_"))
                    Replace(r.gameObject, "books", r.bounds.size);
                if (r.name.StartsWith("HangingLamp_")) Replace(r.gameObject, "lampSquareCeiling", new Vector3(0.8f, 0.3f, 0.45f));
            }
            string table = house ? "HouseSturdyTableTop" : "SturdyCoverTableTop";
            GameObject top = GameObject.Find(table);
            if (top != null)
            {
                Vector3 dimensions = new Vector3(house ? 3.4f : 3.2f, house ? 0.99f : 0.95f, house ? 1.8f : 1.6f);
                Vector3 bottom = new Vector3(top.transform.position.x, HouseSceneLayout.FloorY, top.transform.position.z);
                if (!ReplaceWithTeamModelAt(top, "DiningTable", dimensions, bottom))
                {
                    Replace(top, "table", dimensions);
                    Transform model = top.transform.Find("Kenney_table");
                    if (model != null) model.position = bottom;
                }
            }
            GameObject tutorialCabinet = GameObject.Find("UnsecuredTallCabinet");
            if (!ReplaceWithTeamModel(tutorialCabinet, "Wandrobe", new Vector3(1.1f, 2.5f, 0.7f)))
                Replace(tutorialCabinet, "bookcaseOpen", new Vector3(1.1f, 2.5f, 0.7f));
            GameObject houseFridge = GameObject.Find("HouseTallCabinet_Left");
            if (!ReplaceWithTeamModel(houseFridge, "Fridge", new Vector3(1.05f, 2.3f, 0.72f)))
                Replace(houseFridge, "kitchenFridge", new Vector3(1.05f, 2.3f, 0.72f));
            GameObject houseWardrobe = GameObject.Find("HouseBookcase_Right");
            if (!ReplaceWithTeamModel(houseWardrobe, "Wandrobe", new Vector3(1.05f, 2.3f, 0.72f)))
                Replace(houseWardrobe, "bookcaseOpen", new Vector3(1.05f, 2.3f, 0.72f));
            if (house) DressHouse(); else DressTutorial();
            gameObject.AddComponent<QuakeLightFailures>();
            if (house)
            {
                var outline = new GameObject("HouseSafeCoverOutline");
                outline.transform.SetParent(transform, false);
                var line = outline.AddComponent<LineRenderer>();
                line.useWorldSpace = true; line.loop = true; line.widthMultiplier = 0.04f;
                line.positionCount = 4;
                Vector3 table = HouseSceneLayout.DiningTable;
                float y = HouseSceneLayout.FloorY + 0.025f;
                line.SetPositions(new[] { new Vector3(table.x - 1.55f, y, table.z - 0.8f),
                    new Vector3(table.x + 1.55f, y, table.z - 0.8f),
                    new Vector3(table.x + 1.55f, y, table.z + 0.8f),
                    new Vector3(table.x - 1.55f, y, table.z + 0.8f) });
                Material marker = new Material(Shader.Find("Sprites/Default"));
                owned.Add(marker); line.sharedMaterial = marker;
                line.startColor = line.endColor = new Color(0.05f, 0.85f, 0.25f);
            }
            foreach (Renderer original in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!original.enabled || (original.name != "Ceiling")) continue;
                MeshFilter filter = original.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var visual = new GameObject("QuakeVisual_" + original.name);
                visual.transform.SetParent(original.transform, false);
                visual.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                visual.AddComponent<MeshRenderer>().sharedMaterials = original.sharedMaterials;
                visual.AddComponent<VisualQuakeSway>();
                original.enabled = false;
            }
        }

        private void Hide(params string[] prefixes)
        {
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (string prefix in prefixes)
                    if (renderer.name.StartsWith(prefix, StringComparison.Ordinal)) renderer.enabled = false;
        }
        private void Decor(string model, Vector3 position, Vector3 size, float yaw = 0)
        {
            GameObject decoration = Model(model, transform, position, size, Quaternion.Euler(0, yaw, 0));
            if (decoration == null) return;
            // Static authored collision preserves open space beneath coffee tables and between legs.
            if (model == "loungeSofaLong" || model == "tableCoffee" || model == "bookcaseOpen" ||
                model == "cabinetTelevision" || model.StartsWith("kitchen", StringComparison.Ordinal))
                foreach (MeshFilter surface in decoration.GetComponentsInChildren<MeshFilter>())
                    surface.gameObject.AddComponent<MeshCollider>().sharedMesh = surface.sharedMesh;
        }

        private void DressHouse()
        {
            Hide("HouseSofa", "HouseCoffeeTable", "HousePlant", "HouseShelfBook", "HousePhoto", "HouseRug");

            if (TeamDecor("Sofa", HouseSceneLayout.Sofa, new Vector3(2.2f, 0.95f, 1.0f), 90f, true) == null)
                Decor("loungeSofaLong", HouseSceneLayout.Sofa, new Vector3(2.2f, 1.1f, 0.85f), 90f);
            TeamDecor("Sofa_Pillows", HouseSceneLayout.Sofa + new Vector3(0.22f, 0.50f, 0f),
                new Vector3(1.45f, 0.42f, 0.28f), 90f);

            Decor("tableCoffee", HouseSceneLayout.CoffeeTable, new Vector3(1.7f, 0.48f, 0.9f), 90f);
            Decor("rugRectangle", HouseSceneLayout.Rug + Vector3.up * 0.01f,
                new Vector3(2.5f, 0.02f, 3.0f), 90f);
            Decor("pottedPlant", HouseSceneLayout.Plant, new Vector3(0.7f, 1.35f, 0.7f));
            Decor("cabinetTelevision", HouseSceneLayout.Television,
                new Vector3(2.0f, 0.65f, 0.5f), -90f);
            Decor("televisionModern", HouseSceneLayout.Television + Vector3.up * 0.65f,
                new Vector3(1.45f, 0.85f, 0.22f), -90f);

            // Kitchen line stays against the north wall and clear of the stair volume.
            Decor("kitchenCabinet", HouseSceneLayout.KitchenCabinet,
                new Vector3(1.05f, 0.9f, 0.65f), 180f);
            Decor("kitchenSink", HouseSceneLayout.KitchenSink,
                new Vector3(1.05f, 0.9f, 0.65f), 180f);
            Decor("kitchenStove", HouseSceneLayout.KitchenStove,
                new Vector3(0.9f, 0.9f, 0.65f), 180f);

            Decor("lampRoundFloor", HouseSceneLayout.OnFloor(-4.0f, 1.75f),
                new Vector3(0.45f, 1.6f, 0.45f));
            TeamDecor("Bed", HouseSceneLayout.Bed, new Vector3(1.80f, 0.72f, 2.40f), 0f, true);
            TeamDecor("Bed_Pillow", HouseSceneLayout.BedPillow,
                new Vector3(0.72f, 0.18f, 0.42f));
            CreateShakingVase(HouseSceneLayout.CoffeeTable + Vector3.up * 0.49f);

            foreach (Vector3 position in new[]
            {
                HouseSceneLayout.OnFloor(-2.8f, 0.6f, 2.5f),
                HouseSceneLayout.OnFloor(3.4f, 5.2f, 2.5f)
            })
            {
                var lamp = new GameObject("HouseWarmFill");
                lamp.transform.SetParent(transform, false);
                lamp.transform.position = position;
                Light light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.88f, 0.73f);
                light.intensity = 0.8f;
                light.range = 5.5f;
                light.shadows = LightShadows.None;
                GameObject fixture = Model("lampSquareCeiling", transform, position + Vector3.up * 0.75f,
                    new Vector3(0.8f, 0.15f, 0.5f), Quaternion.identity);
                if (fixture != null) fixture.AddComponent<VisualQuakeSway>();
            }

            ValidateHouseFurnitureBounds();
        }

        private void ValidateHouseFurnitureBounds()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !IsTeamFurnitureRenderer(renderer.transform)) continue;
                Bounds bounds = renderer.bounds;
                if (bounds.min.y < HouseSceneLayout.FloorY - 0.06f)
                    Debug.LogError($"CEVR furniture below the authored floor: {renderer.name} at y={bounds.min.y:F2}.");
                if (bounds.center.x < -5.0f || bounds.center.x > 7.0f ||
                    bounds.center.z < -7.5f || bounds.center.z > 7.5f)
                    Debug.LogError($"CEVR furniture outside the authored house: {renderer.name} at {bounds.center}.");
            }
        }

        private bool IsTeamFurnitureRenderer(Transform candidate)
        {
            Transform current = candidate;
            while (current != null && current != transform)
            {
                if (current.name.StartsWith("TeamFurniture_", StringComparison.Ordinal)) return true;
                current = current.parent;
            }
            return false;
        }

        private void DressTutorial()
        {
            // Replace decorative workstation blocks, preserving task items and their existing triggers.
            Hide("Monitor", "Keyboard", "Mouse", "Manual", "Plant", "Storage");
            foreach (float z in new[] { -1.6f, 1.6f })
            {
                Decor("computerScreen", new Vector3(-4.4f, 0.89f, z + 0.25f), new Vector3(0.65f, 0.5f, 0.2f), 180);
                Decor("computerKeyboard", new Vector3(-4.4f, 0.9f, z - 0.1f), new Vector3(0.52f, 0.035f, 0.18f));
            }
            Decor("bookcaseOpen", new Vector3(-8.7f, 0, 4.9f), new Vector3(1.3f, 2.2f, 0.4f));
            Decor("pottedPlant", new Vector3(-9.0f, 0, -4.9f), new Vector3(0.6f, 1.4f, 0.6f));
            if (TeamDecor("Sofa", new Vector3(1.5f, 0f, 4.9f), new Vector3(2.6f, 1.0f, 1.08f), 0f, true) == null)
                Decor("loungeSofaLong", new Vector3(1.5f, 0, 4.9f), new Vector3(2.6f, 1f, 0.85f));
            TeamDecor("Sofa_Pillows", new Vector3(1.5f, 0.53f, 4.65f), new Vector3(1.65f, 0.45f, 0.30f));
            Decor("tableCoffee", new Vector3(1.5f, 0, 3.6f), new Vector3(1.3f, 0.45f, 0.65f));
            Decor("books", new Vector3(1.5f, 0.46f, 3.6f), new Vector3(0.35f, 0.15f, 0.3f));
        }
        private void OnDestroy()
        {
            foreach (UnityEngine.Object item in owned) if (item != null) Destroy(item);
        }
    }
}
