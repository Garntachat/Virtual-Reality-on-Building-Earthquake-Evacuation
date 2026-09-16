using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Adds tasteful wallpaper panels to House and Tutorial without modifying authored ProBuilder
    /// meshes or their colliders. Panels are placed only on real wall hits found by raycasts, so they
    /// follow the actual room geometry instead of guessed coordinates.
    /// </summary>
    [DefaultExecutionOrder(28000)]
    public sealed class SceneWallpaperDecorator : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private Scene scene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene s = SceneManager.GetActiveScene();
            if (!s.IsValid() || !s.isLoaded) return;
            string n = s.name.ToLowerInvariant();
            if (!n.Contains("house") && !n.Contains("tutorial")) return;
            if (FindFirstObjectByType<SceneWallpaperDecorator>() != null) return;

            GameObject host = new GameObject("CEVR_SceneWallpaperDecorator");
            SceneManager.MoveGameObjectToScene(host, s);
            host.AddComponent<SceneWallpaperDecorator>();
        }

        private IEnumerator Start()
        {
            scene = SceneManager.GetActiveScene();
            yield return null;
            yield return null;

            string n = scene.name.ToLowerInvariant();
            if (n.Contains("house")) DecorateHouse();
            else DecorateTutorial();
        }

        private void DecorateHouse()
        {
            Texture2D texture = MakeWallpaperTexture(
                new Color(0.80f, 0.83f, 0.86f),
                new Color(0.69f, 0.74f, 0.79f),
                new Color(0.90f, 0.73f, 0.79f));
            Material material = WallpaperMaterial("House Wallpaper", texture, new Vector2(2.2f, 2.0f));

            // Use the measured living/dining zone as the sampling origin. Each panel is only created
            // when the actual House collider confirms a wall at that direction.
            Vector3 origin = new Vector3(-1.90f, 2.10f, -1.10f);
            int count = 0;
            count += TryWallpaperWall(origin, Vector3.left, 4.5f, 2.65f, 2.15f, material) ? 1 : 0;
            count += TryWallpaperWall(origin, Vector3.right, 6.0f, 2.65f, 2.15f, material) ? 1 : 0;
            count += TryWallpaperWall(origin, Vector3.forward, 7.5f, 2.85f, 2.15f, material) ? 1 : 0;
            count += TryWallpaperWall(new Vector3(-2.0f, 2.10f, -4.2f), Vector3.back, 4.0f, 2.35f, 2.15f, material) ? 1 : 0;

            Debug.Log($"CEVR House wallpaper ready: {count} measured wall panel(s) added.");
        }

        private void DecorateTutorial()
        {
            Texture2D texture = MakeWallpaperTexture(
                new Color(0.73f, 0.79f, 0.84f),
                new Color(0.58f, 0.67f, 0.75f),
                new Color(0.94f, 0.76f, 0.38f));
            Material material = WallpaperMaterial("Tutorial Wallpaper", texture, new Vector2(2.4f, 2.1f));

            GameObject table = GameObject.Find("SturdyCoverTableTop");
            Vector3 center = table != null ? table.transform.position : Vector3.zero;
            center.y = 1.45f;

            int count = 0;
            count += TryWallpaperWall(center, Vector3.left, 10f, 3.2f, 2.25f, material) ? 1 : 0;
            count += TryWallpaperWall(center, Vector3.right, 10f, 3.2f, 2.25f, material) ? 1 : 0;
            count += TryWallpaperWall(center, Vector3.forward, 10f, 3.6f, 2.25f, material) ? 1 : 0;
            count += TryWallpaperWall(center, Vector3.back, 10f, 3.6f, 2.25f, material) ? 1 : 0;

            Debug.Log($"CEVR Tutorial wallpaper ready: {count} measured wall panel(s) added.");
        }

        private bool TryWallpaperWall(Vector3 origin, Vector3 direction, float maxDistance,
            float width, float height, Material material)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject.scene != scene) continue;
                if (Mathf.Abs(hit.normal.y) > 0.35f) continue; // floor/ceiling, not wall
                if (IsGeneratedGameplay(hit.collider.transform)) continue;

                Vector3 normal = hit.normal.normalized;
                Vector3 tangent = Vector3.Cross(Vector3.up, normal).normalized;
                if (tangent.sqrMagnitude < 0.5f) continue;

                // Verify enough uninterrupted authored wall exists around the planned panel. This
                // prevents wallpaper from bridging over doors/windows/openings.
                if (!WallSupportExists(hit.point, normal, tangent, width, height)) continue;

                GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
                panel.name = "CEVR_WallpaperPanel";
                SceneManager.MoveGameObjectToScene(panel, scene);
                panel.transform.position = hit.point + normal * 0.012f;
                panel.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
                panel.transform.localScale = new Vector3(width, height, 1f);

                Collider collider = panel.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                MeshRenderer renderer = panel.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                return true;
            }
            return false;
        }

        private bool WallSupportExists(Vector3 center, Vector3 inwardNormal, Vector3 tangent,
            float width, float height)
        {
            float[] horizontal = { -0.42f, 0f, 0.42f };
            float[] vertical = { -0.35f, 0f, 0.35f };
            int valid = 0;
            int total = 0;

            foreach (float hx in horizontal)
            foreach (float vy in vertical)
            {
                total++;
                Vector3 sample = center + tangent * (hx * width) + Vector3.up * (vy * height) + inwardNormal * 0.20f;
                RaycastHit[] hits = Physics.RaycastAll(sample, -inwardNormal, 0.45f, ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit h in hits)
                {
                    if (h.collider == null || h.collider.gameObject.scene != scene) continue;
                    if (Mathf.Abs(h.normal.y) > 0.35f) continue;
                    if (IsGeneratedGameplay(h.collider.transform)) continue;
                    valid++;
                    break;
                }
            }
            return valid >= total - 1;
        }

        private static bool IsGeneratedGameplay(Transform t)
        {
            while (t != null)
            {
                string n = t.name;
                if (n.StartsWith("CEVR_", StringComparison.Ordinal) ||
                    n.StartsWith("HouseFinal_", StringComparison.Ordinal) ||
                    n.StartsWith("TeamFurniture_", StringComparison.Ordinal) ||
                    n.StartsWith("Kenney_", StringComparison.Ordinal)) return true;
                t = t.parent;
            }
            return false;
        }

        private Texture2D MakeWallpaperTexture(Color baseColor, Color stripeColor, Color accent)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "CEVR Procedural Wallpaper",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float stripe = Mathf.SmoothStep(0f, 1f, Mathf.Abs(Mathf.Sin(u * Mathf.PI * 8f)));
                Color c = Color.Lerp(baseColor, stripeColor, stripe * 0.16f);

                // Small understated diamond motif every 32 px. It reads as wallpaper up close but
                // stays calm enough for the earthquake-training HUD and furniture.
                float gx = Mathf.Repeat(x, 32f) - 16f;
                float gy = Mathf.Repeat(y, 32f) - 16f;
                float diamond = Mathf.Abs(gx) + Mathf.Abs(gy);
                if (diamond > 8.5f && diamond < 10.5f)
                    c = Color.Lerp(c, accent, 0.22f);

                texture.SetPixel(x, y, c);
            }
            texture.Apply(true, false);
            owned.Add(texture);
            return texture;
        }

        private Material WallpaperMaterial(string name, Texture2D texture, Vector2 tiling)
        {
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit");
            if (shader == null || !shader.isSupported) shader = Shader.Find("Unlit/Texture");
            Material material = new Material(shader) { name = name, mainTexture = texture };
            material.mainTextureScale = tiling;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.08f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.08f);
            owned.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            foreach (UnityEngine.Object item in owned)
                if (item != null) Destroy(item);
            owned.Clear();
        }
    }
}
