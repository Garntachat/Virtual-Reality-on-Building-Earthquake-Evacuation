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

        private void Start()
        {
            bool house = SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("house");
            foreach (MovableFurniture chair in FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None))
                Replace(chair.gameObject, house ? "chairCushion" : "chairDesk", new Vector3(0.9f, 1.14f, 0.9f), true);
            Replace(GameObject.Find("ProtectivePillow"), "pillowBlue", new Vector3(0.9f, 0.24f, 0.64f));
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r.name == "LabBenchTop")
                {
                    Replace(r.gameObject, "desk", new Vector3(4f, 0.89f, 1.2f));
                    Transform model = r.transform.Find("Kenney_desk");
                    if (model != null) model.position = new Vector3(r.transform.position.x, 0, r.transform.position.z);
                }
                if (r.name == "LabBenchLeg" || r.name == "SturdyTableLeg" || r.name == "HouseSturdyTableLeg") r.enabled = false;
                if (r.name.StartsWith("HangingLamp_")) Replace(r.gameObject, "lampSquareCeiling", new Vector3(0.8f, 0.3f, 0.45f));
            }
            string table = house ? "HouseSturdyTableTop" : "SturdyCoverTableTop";
            GameObject top = GameObject.Find(table);
            if (top != null)
            {
                Replace(top, "table", new Vector3(house ? 2.8f : 3.2f, 0.95f, house ? 1.5f : 1.6f));
                Transform model = top.transform.Find("Kenney_table");
                if (model != null) model.position = new Vector3(top.transform.position.x, 0, top.transform.position.z);
            }
            Replace(GameObject.Find("UnsecuredTallCabinet"), "bookcaseOpen", new Vector3(1.1f, 2.5f, 0.7f));
            Replace(GameObject.Find("HouseTallCabinet_Left"), "kitchenFridge", new Vector3(1.05f, 2.3f, 0.72f));
            Replace(GameObject.Find("HouseBookcase_Right"), "bookcaseOpen", new Vector3(1.05f, 2.3f, 0.72f));
            if (house) DressHouse(); else DressTutorial();
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
            => Model(model, transform, position, size, Quaternion.Euler(0, yaw, 0));

        private void DressHouse()
        {
            Hide("HouseSofa", "HouseCoffeeTable", "HousePlant", "HouseShelfBook", "HousePhoto", "HouseRug");
            Decor("loungeSofaLong", new Vector3(-3.5f, 0, -4.6f), new Vector3(2.2f, 1.1f, 0.85f));
            Decor("tableCoffee", new Vector3(-3.45f, 0, -3.25f), new Vector3(1.7f, 0.48f, 0.9f));
            Decor("rugRectangle", new Vector3(-3.45f, 0.01f, -3.7f), new Vector3(3, 0.02f, 2.5f));
            Decor("pottedPlant", new Vector3(4.6f, 0, -4.8f), new Vector3(0.7f, 1.35f, 0.7f));
            Decor("cabinetTelevision", new Vector3(-3.5f, 0, -1.85f), new Vector3(2, 0.65f, 0.5f), 180);
            Decor("televisionModern", new Vector3(-3.5f, 0.65f, -1.85f), new Vector3(1.45f, 0.85f, 0.22f), 180);
            Decor("kitchenCabinet", new Vector3(2.25f, 0, 1.15f), new Vector3(1.1f, 0.9f, 0.65f));
            Decor("kitchenSink", new Vector3(3.35f, 0, 1.15f), new Vector3(1.1f, 0.9f, 0.65f));
            Decor("kitchenStove", new Vector3(4.45f, 0, 1.15f), new Vector3(0.9f, 0.9f, 0.65f));
            Decor("lampRoundFloor", new Vector3(-4.9f, 0, -4.9f), new Vector3(0.45f, 1.6f, 0.45f));
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
            Decor("loungeSofaLong", new Vector3(1.5f, 0, 4.9f), new Vector3(2.6f, 1f, 0.85f));
            Decor("tableCoffee", new Vector3(1.5f, 0, 3.6f), new Vector3(1.3f, 0.45f, 0.65f));
            Decor("books", new Vector3(1.5f, 0.46f, 3.6f), new Vector3(0.35f, 0.15f, 0.3f));
        }
        private void OnDestroy()
        {
            foreach (UnityEngine.Object item in owned) if (item != null) Destroy(item);
        }
    }
}
