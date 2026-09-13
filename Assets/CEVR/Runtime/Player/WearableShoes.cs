using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    public sealed class WearableShoes : MonoBehaviour
    {
        [SerializeField] private string footwearId = "protective-shoes-01";
        [SerializeField] private SessionLogger logger;
        private readonly List<Object> visualResources = new List<Object>();

        public bool IsEquipped { get; private set; }

        private void Start()
        {
            if (logger == null) logger = FindFirstObjectByType<SessionLogger>();
            EnsureAuthoredVisual();
        }

        public void Configure(string id, SessionLogger sessionLogger)
        {
            footwearId = string.IsNullOrWhiteSpace(id) ? name : id;
            logger = sessionLogger;
        }

        private void EnsureAuthoredVisual()
        {
            if (transform.Find("AuthoredSafetyShoes") != null) return;
            foreach (Renderer oldRenderer in GetComponentsInChildren<Renderer>()) oldRenderer.enabled = false;

            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            Material upper = NewMaterial(shader, "Safety Shoe Navy", new Color(0.055f, 0.09f, 0.16f));
            Material sole = NewMaterial(shader, "Safety Shoe Sole", new Color(0.025f, 0.03f, 0.04f));
            Material lace = NewMaterial(shader, "Safety Shoe Laces", new Color(0.91f, 0.94f, 0.96f));

            var pair = new GameObject("AuthoredSafetyShoes");
            pair.transform.SetParent(transform, false);
            CreateShoe(pair.transform, -0.15f, upper, sole, lace);
            CreateShoe(pair.transform, 0.15f, upper, sole, lace);
        }

        private Material NewMaterial(Shader shader, string materialName, Color color)
        {
            var material = new Material(shader) { name = materialName, color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.22f);
            visualResources.Add(material);
            return material;
        }

        private void CreateShoe(Transform parent, float centerX, Material upper, Material sole, Material lace)
        {
            float[] z = { -0.21f, -0.08f, 0.10f, 0.24f };
            float[] widths = { 0.075f, 0.09f, 0.11f, 0.135f };
            CreateMeshPart("RubberSole", parent, centerX, Loft(z, widths, widths, 0.015f,
                new[] { 0.06f, 0.06f, 0.055f, 0.045f }), sole);
            CreateMeshPart("ShapedUpper", parent, centerX, Loft(z, Scale(widths, 0.92f), Scale(widths, 0.68f),
                0.05f, new[] { 0.145f, 0.17f, 0.145f, 0.095f }), upper);
            CreateMeshPart("PaddedTongue", parent, centerX, Loft(
                new[] { -0.07f, 0.11f }, new[] { 0.052f, 0.065f }, new[] { 0.048f, 0.055f },
                0.13f, new[] { 0.205f, 0.16f }), upper);
            for (int i = 0; i < 3; i++)
                CreateLace(parent, centerX, -0.025f + i * 0.055f, 0.172f - i * 0.008f, lace);
        }

        private static float[] Scale(float[] values, float multiplier)
        {
            var result = new float[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = values[i] * multiplier;
            return result;
        }

        private static Mesh Loft(float[] z, float[] bottomWidths, float[] topWidths, float bottomY, float[] topY)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < z.Length; i++)
            {
                vertices.Add(new Vector3(-bottomWidths[i], bottomY, z[i]));
                vertices.Add(new Vector3(bottomWidths[i], bottomY, z[i]));
                vertices.Add(new Vector3(-topWidths[i], topY[i], z[i]));
                vertices.Add(new Vector3(topWidths[i], topY[i], z[i]));
            }
            for (int i = 0; i < z.Length - 1; i++)
            {
                int a = i * 4, b = (i + 1) * 4;
                Quad(triangles, a, a + 1, b + 1, b);
                Quad(triangles, a + 2, b + 2, b + 3, a + 3);
                Quad(triangles, a, b, b + 2, a + 2);
                Quad(triangles, a + 1, a + 3, b + 3, b + 1);
            }
            Quad(triangles, 0, 2, 3, 1);
            int end = (z.Length - 1) * 4;
            Quad(triangles, end, end + 1, end + 3, end + 2);
            var mesh = new Mesh { name = "CEVR_LowPolySafetyShoe" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static void Quad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
        }

        private void CreateLace(Transform parent, float centerX, float z, float y, Material material)
        {
            var mesh = new Mesh { name = "CEVR_ShoeLace" };
            mesh.vertices = new[]
            {
                new Vector3(-0.073f, 0f, -0.008f), new Vector3(0.073f, 0f, -0.008f),
                new Vector3(-0.073f, 0f, 0.008f), new Vector3(0.073f, 0f, 0.008f)
            };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var lace = CreateMeshPart("Lace", parent, centerX, mesh, material);
            lace.transform.localPosition += new Vector3(0f, y, z);
        }

        private GameObject CreateMeshPart(string objectName, Transform parent, float centerX, Mesh mesh, Material material)
        {
            visualResources.Add(mesh);
            var part = new GameObject(objectName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = Vector3.right * centerX;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        public void Equip(PlayerHealth wearer, Transform wearerRoot)
        {
            if (IsEquipped || wearer == null || wearerRoot == null) return;
            IsEquipped = true;
            wearer.EquipProtectiveFootwear();
            GameplayAudioDirector.PlayCue(GameplayAudioCue.FootwearEquipped, 0.30f);
            transform.SetParent(wearerRoot, false);
            transform.localPosition = new Vector3(0f, 0.07f, 0.08f);
            transform.localRotation = Quaternion.identity;
            foreach (Collider itemCollider in GetComponentsInChildren<Collider>()) itemCollider.enabled = false;
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
            logger?.LogEvent("footwear_equipped", $"{{\"footwearId\":\"{footwearId}\"}}");
        }

        private void OnDestroy()
        {
            foreach (Object resource in visualResources) if (resource != null) Destroy(resource);
        }
    }
}
