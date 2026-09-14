using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    public sealed class WindowView : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        private Renderer glassRenderer;
        private bool built;

        private void Start() => EnsureBuilt();

        public void Configure() => EnsureBuilt();

        private void EnsureBuilt()
        {
            if (built) return;
            built = true;
            glassRenderer = GetComponent<Renderer>();
            ApplyTransparentGlass();
            BuildExteriorView();
        }

        private void ApplyTransparentGlass()
        {
            if (glassRenderer == null) return;
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;

            var glass = new Material(shader)
            {
                name = "CEVR Transparent Window Glass",
                color = new Color(0.42f, 0.72f, 0.92f, 0.18f),
                renderQueue = (int)RenderQueue.Transparent
            };
            glass.SetOverrideTag("RenderType", "Transparent");
            if (glass.HasProperty("_Mode")) glass.SetFloat("_Mode", 3f);
            if (glass.HasProperty("_Surface")) glass.SetFloat("_Surface", 1f);
            if (glass.HasProperty("_SrcBlend")) glass.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (glass.HasProperty("_DstBlend")) glass.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (glass.HasProperty("_ZWrite")) glass.SetInt("_ZWrite", 0);
            glass.DisableKeyword("_ALPHATEST_ON");
            glass.EnableKeyword("_ALPHABLEND_ON");
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glassRenderer.sharedMaterial = glass;
            materials.Add(glass);
        }

        private void BuildExteriorView()
        {
            if (transform.Find("ExteriorView") != null) return;
            var root = new GameObject("ExteriorView").transform;
            root.SetParent(transform, false);

            Material sky = ViewMaterial("Exterior Sky", new Color(0.28f, 0.62f, 0.90f));
            Material horizon = ViewMaterial("Exterior Horizon", new Color(0.72f, 0.86f, 0.91f));
            Material building = ViewMaterial("Exterior Buildings", new Color(0.16f, 0.22f, 0.30f));
            Material greenery = ViewMaterial("Exterior Greenery", new Color(0.12f, 0.42f, 0.25f));
            Material sun = ViewMaterial("Exterior Sun", new Color(1f, 0.82f, 0.38f));

            Panel("Sky", root, new Vector3(0f, 0f, 0.44f), new Vector3(0.94f, 0.90f, 0.08f), sky);
            Panel("Horizon", root, new Vector3(0f, -0.14f, 0.35f), new Vector3(0.94f, 0.34f, 0.08f), horizon);
            Panel("BuildingLeft", root, new Vector3(-0.55f, -0.22f, 0.27f), new Vector3(0.22f, 0.42f, 0.08f), building);
            Panel("BuildingCenter", root, new Vector3(-0.12f, -0.30f, 0.27f), new Vector3(0.30f, 0.28f, 0.08f), building);
            Panel("BuildingRight", root, new Vector3(0.50f, -0.25f, 0.27f), new Vector3(0.26f, 0.36f, 0.08f), building);
            Panel("TreeLine", root, new Vector3(0f, -0.45f, 0.19f), new Vector3(0.94f, 0.14f, 0.08f), greenery);
            Panel("Sun", root, new Vector3(0.57f, 0.28f, 0.27f), new Vector3(0.11f, 0.15f, 0.08f), sun);
        }

        private Material ViewMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Unlit/Color" : "Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var material = new Material(shader) { name = materialName, color = color };
            materials.Add(material);
            return material;
        }

        private static void Panel(
            string objectName, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = objectName;
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = localPosition;
            panel.transform.localScale = localScale;
            Collider collider = panel.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = panel.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
        }

        private void OnDestroy()
        {
            foreach (Material material in materials)
                if (material != null) Destroy(material);
            materials.Clear();
        }
    }
}
