using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Visual-only wallpaper for House. It creates no colliders and never changes saved furniture,
    /// chair interaction, physics, or House.unity.
    /// </summary>
    public sealed class HouseWallpaperDecorator : MonoBehaviour
    {
        private const string RootName = "CEVR_HouseWallpaper";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.ToLowerInvariant().Contains("house") == false)
                return;

            if (GameObject.Find(RootName) != null) return;

            GameObject root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HouseWallpaperDecorator>().Build();
        }

        private void Build()
        {
            Material warm = CreateMaterial(
                "House Wallpaper - Warm Linen",
                new Color(0.91f, 0.87f, 0.816f),
                0.12f);

            Material accent = CreateMaterial(
                "House Wallpaper - Living Taupe",
                new Color(0.68f, 0.56f, 0.46f),
                0.10f);

            Material trim = CreateMaterial(
                "House Wallpaper - Accent Trim",
                new Color(0.80f, 0.70f, 0.60f),
                0.08f);

            // Ground floor interior wall faces. Panels are slightly inside the authored walls
            // and have no collider, so they cannot affect chairs, the cat, player movement or VR.
            Panel("Wallpaper_North_Warm",
                new Vector3(1.00f, 2.50f, 6.73f),
                new Vector3(10.40f, 2.90f, 0.025f), warm);

            Panel("Wallpaper_East_Warm",
                new Vector3(6.23f, 2.50f, 0.00f),
                new Vector3(0.025f, 2.90f, 13.40f), warm);

            Panel("Wallpaper_West_SouthWarm",
                new Vector3(-4.23f, 2.50f, -3.90f),
                new Vector3(0.025f, 2.90f, 5.60f), warm);

            Panel("Wallpaper_West_LivingAccent",
                new Vector3(-4.23f, 2.50f, 1.40f),
                new Vector3(0.025f, 2.90f, 4.80f), accent);

            Panel("Wallpaper_West_NorthWarm",
                new Vector3(-4.23f, 2.50f, 5.30f),
                new Vector3(0.025f, 2.90f, 2.80f), warm);

            // South wall is deliberately split so the existing window and front door stay clear.
            Panel("Wallpaper_South_Left",
                new Vector3(-3.50f, 2.50f, -6.73f),
                new Vector3(1.40f, 2.90f, 0.025f), warm);

            Panel("Wallpaper_South_Middle",
                new Vector3(-0.60f, 2.50f, -6.73f),
                new Vector3(1.20f, 2.90f, 0.025f), warm);

            Panel("Wallpaper_South_Right",
                new Vector3(3.775f, 2.50f, -6.73f),
                new Vector3(4.85f, 2.90f, 0.025f), warm);

            // A few thin vertical trims on the living-room accent wall make it read as designed
            // wallpaper instead of a single flat block of color.
            float[] trimZ = { -0.45f, 0.35f, 1.15f, 1.95f, 2.75f, 3.55f };
            foreach (float z in trimZ)
            {
                Panel("Wallpaper_LivingTrim",
                    new Vector3(-4.205f, 2.50f, z),
                    new Vector3(0.018f, 2.72f, 0.045f), trim);
            }
        }

        private GameObject Panel(
            string objectName,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = objectName;
            panel.transform.SetParent(transform, true);
            panel.transform.position = position;
            panel.transform.localScale = scale;

            Collider collider = panel.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }

            Renderer renderer = panel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }

            return panel;
        }

        private static Material CreateMaterial(
            string materialName,
            Color color,
            float smoothness)
        {
            string shaderName =
                UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                    ? "Standard"
                    : "Universal Render Pipeline/Lit";

            Shader shader = Shader.Find(shaderName) ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = materialName,
                color = color
            };

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);

            return material;
        }
    }
}
