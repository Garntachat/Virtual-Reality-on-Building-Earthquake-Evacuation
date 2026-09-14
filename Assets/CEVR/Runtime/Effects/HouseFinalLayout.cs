using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// One authoritative visual/layout pass for the House scene.
    /// It deliberately removes the older overlapping dressing passes, preserves gameplay anchors,
    /// places every room group at a unique world position, and aligns a single functional window
    /// with the actual wall opening detected from the authored house collision.
    /// </summary>
    [DefaultExecutionOrder(40000)]
    public sealed class HouseFinalLayout : MonoBehaviour
    {
        private const string GameplayRoot = "CEVR_UniversalGameplay";
        private readonly List<Material> ownedMaterials = new List<Material>();
        private Transform root;
        private GroundMotionPlayer motion;
        private SessionLogger logger;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (FindFirstObjectByType<HouseFinalLayout>() != null) return;
            new GameObject("CEVR_HouseFinalLayout").AddComponent<HouseFinalLayout>();
        }

        private IEnumerator Start()
        {
            // Wait until bootstrap + previous compatibility passes have finished, then take ownership.
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            GameObject gameplay = GameObject.Find(GameplayRoot);
            if (gameplay == null)
            {
                Debug.LogError("CEVR final House layout could not find the gameplay root.");
                yield break;
            }
            root = gameplay.transform;
            motion = FindFirstObjectByType<GroundMotionPlayer>();
            logger = FindFirstObjectByType<SessionLogger>();

            RemoveCompetingVisuals();
            PlaceGameplayFurniture();
            BuildLivingRoom();
            BuildKitchen();
            BuildBedroom();
            BuildWallWindow();

            Debug.Log("CEVR FINAL HOUSE LAYOUT READY: furniture groups separated and the functional window aligned to the wall opening.");
        }

        private void RemoveCompetingVisuals()
        {
            // Remove only generated visuals. Gameplay objects/colliders/scripts remain intact.
            string[] destroyPrefixes =
            {
                "TeamFurniture_", "Stable_House", "StableTemplate_"
            };

            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null || t == transform || t == root) continue;
                bool destroy = false;
                foreach (string prefix in destroyPrefixes)
                    if (t.name.StartsWith(prefix, StringComparison.Ordinal)) { destroy = true; break; }
                if (destroy) Destroy(t.gameObject);
            }

            // Hide old placeholder decoration that otherwise visually stacks with the final room.
            HideRendererByPrefix("HouseSofa", "HouseRug", "HousePlant", "HouseShelfBook", "HousePhoto");

            // Old generated windows are replaced by one correctly aligned functional window below.
            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go != null && go.name.StartsWith("HouseWindow_", StringComparison.Ordinal)) Destroy(go);
        }

        private void PlaceGameplayFurniture()
        {
            // These positions are intentionally distinct and preserve the original training route.
            PlaceChair("HouseChair_CoverObstacle", new Vector3(0f, 0f, -4.15f), 0f);
            PlaceChair("HouseChair_DiningLeft", new Vector3(-1.65f, 0f, -2.70f), 90f);
            PlaceChair("HouseChair_DiningRight", new Vector3(1.65f, 0f, -2.70f), -90f);
            PlaceChair("HouseChair_Spare", new Vector3(3.65f, 0f, -4.55f), -35f);

            GameObject table = GameObject.Find("HouseSturdyTableTop");
            if (table != null)
            {
                table.transform.position = new Vector3(0f, 0.90f, -2.70f);
                HideRenderer(table);
                BuildTableVisual(table.transform, Vector3.zero);
            }

            GameObject fridge = GameObject.Find("HouseTallCabinet_Left");
            if (fridge != null)
            {
                fridge.transform.position = new Vector3(-4.35f, 1.15f, 0.35f);
                HideRenderer(fridge);
                BuildFridgeVisual(fridge.transform);
            }

            GameObject wardrobe = GameObject.Find("HouseBookcase_Right");
            if (wardrobe != null)
            {
                wardrobe.transform.position = new Vector3(4.35f, 1.15f, 0.55f);
                HideRenderer(wardrobe);
                BuildWardrobeVisual(wardrobe.transform);
            }

            GameObject pillow = GameObject.Find("ProtectivePillow");
            if (pillow != null)
            {
                pillow.transform.position = new Vector3(-3.75f, 0.78f, 4.35f);
                HideRenderer(pillow);
                Part("Final_Pillow", PrimitiveType.Sphere, pillow.transform, Vector3.zero,
                    new Vector3(0.42f, 0.10f, 0.30f), Mat("Pillow", new Color(0.72f, 0.84f, 0.94f)), false);
            }
        }

        private void PlaceChair(string name, Vector3 position, float yaw)
        {
            GameObject chair = GameObject.Find(name);
            if (chair == null) return;
            chair.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            foreach (Renderer r in chair.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

            Material wood = Mat("Chair Wood", new Color(0.30f, 0.17f, 0.09f));
            Material cushion = Mat("Chair Cushion", new Color(0.50f, 0.16f, 0.23f));
            Part("Final_ChairSeat", PrimitiveType.Cube, chair.transform, new Vector3(0f, 0.48f, 0f), new Vector3(0.66f, 0.10f, 0.62f), cushion, false);
            Part("Final_ChairBack", PrimitiveType.Cube, chair.transform, new Vector3(0f, 0.84f, 0.27f), new Vector3(0.62f, 0.48f, 0.09f), cushion, false);
            foreach (float x in new[] { -0.25f, 0.25f })
            foreach (float z in new[] { -0.23f, 0.23f })
                Part("Final_ChairLeg", PrimitiveType.Cube, chair.transform, new Vector3(x, 0.23f, z), new Vector3(0.07f, 0.46f, 0.07f), wood, false);
        }

        private void BuildTableVisual(Transform parent, Vector3 offset)
        {
            Material wood = Mat("Dining Table", new Color(0.36f, 0.20f, 0.11f));
            Part("Final_TableTop", PrimitiveType.Cube, parent, offset, new Vector3(3.30f, 0.16f, 1.72f), wood, false);
            foreach (float x in new[] { -1.38f, 1.38f })
            foreach (float z in new[] { -0.68f, 0.68f })
                Part("Final_TableLeg", PrimitiveType.Cube, parent, new Vector3(x, -0.42f, z), new Vector3(0.14f, 0.84f, 0.14f), wood, false);
        }

        private void BuildFridgeVisual(Transform parent)
        {
            Material body = Mat("Fridge Body", new Color(0.76f, 0.79f, 0.82f));
            Material dark = Mat("Fridge Trim", new Color(0.08f, 0.10f, 0.12f));
            Part("Final_FridgeBody", PrimitiveType.Cube, parent, Vector3.zero, new Vector3(1.02f, 2.24f, 0.68f), body, false);
            Part("Final_FridgeSplit", PrimitiveType.Cube, parent, new Vector3(0f, 0.18f, -0.35f), new Vector3(0.92f, 0.025f, 0.025f), dark, false);
            Part("Final_FridgeHandle", PrimitiveType.Cube, parent, new Vector3(0.37f, 0.30f, -0.38f), new Vector3(0.06f, 0.72f, 0.05f), dark, false);
        }

        private void BuildWardrobeVisual(Transform parent)
        {
            Material wood = Mat("Wardrobe", new Color(0.30f, 0.18f, 0.11f));
            Material trim = Mat("Wardrobe Trim", new Color(0.12f, 0.08f, 0.06f));
            Part("Final_WardrobeBody", PrimitiveType.Cube, parent, Vector3.zero, new Vector3(1.02f, 2.24f, 0.68f), wood, false);
            Part("Final_WardrobeSplit", PrimitiveType.Cube, parent, new Vector3(0f, 0f, -0.35f), new Vector3(0.025f, 2.06f, 0.025f), trim, false);
            Part("Final_WardrobeHandleL", PrimitiveType.Sphere, parent, new Vector3(-0.10f, 0f, -0.38f), new Vector3(0.035f, 0.035f, 0.025f), trim, false);
            Part("Final_WardrobeHandleR", PrimitiveType.Sphere, parent, new Vector3(0.10f, 0f, -0.38f), new Vector3(0.035f, 0.035f, 0.025f), trim, false);
        }

        private void BuildLivingRoom()
        {
            Material sofa = Mat("Living Sofa", new Color(0.36f, 0.48f, 0.58f));
            Material wood = Mat("Living Wood", new Color(0.34f, 0.19f, 0.10f));
            Material rug = Mat("Living Rug", new Color(0.23f, 0.43f, 0.50f));
            Transform group = Group("Final_LivingRoom", new Vector3(-3.45f, 0f, -4.65f));

            Part("SofaSeat", PrimitiveType.Cube, group, new Vector3(0f, 0.42f, 0f), new Vector3(2.20f, 0.42f, 0.82f), sofa, true);
            Part("SofaBack", PrimitiveType.Cube, group, new Vector3(0f, 0.88f, 0.32f), new Vector3(2.20f, 0.66f, 0.18f), sofa, true);
            Part("SofaArmL", PrimitiveType.Cube, group, new Vector3(-1.02f, 0.62f, 0f), new Vector3(0.18f, 0.55f, 0.82f), sofa, true);
            Part("SofaArmR", PrimitiveType.Cube, group, new Vector3(1.02f, 0.62f, 0f), new Vector3(0.18f, 0.55f, 0.82f), sofa, true);

            Part("Rug", PrimitiveType.Cube, root, new Vector3(-3.45f, 0.02f, -3.45f), new Vector3(2.8f, 0.025f, 1.7f), rug, false);
            Part("CoffeeTop", PrimitiveType.Cube, root, new Vector3(-3.45f, 0.43f, -3.35f), new Vector3(1.50f, 0.10f, 0.72f), wood, true);
            foreach (float x in new[] { -0.60f, 0.60f })
            foreach (float z in new[] { -0.26f, 0.26f })
                Part("CoffeeLeg", PrimitiveType.Cube, root, new Vector3(-3.45f + x, 0.21f, -3.35f + z), new Vector3(0.08f, 0.42f, 0.08f), wood, true);

            Material pot = Mat("Plant Pot", new Color(0.43f, 0.22f, 0.12f));
            Material leaf = Mat("Plant Leaf", new Color(0.10f, 0.42f, 0.20f));
            Part("PlantPot", PrimitiveType.Cylinder, root, new Vector3(-4.70f, 0.28f, -3.00f), new Vector3(0.32f, 0.28f, 0.32f), pot, true);
            for (int i = 0; i < 5; i++)
            {
                Transform l = Part("PlantLeaf", PrimitiveType.Sphere, root,
                    new Vector3(-4.70f + (i - 2) * 0.09f, 0.67f + (i % 2) * 0.13f, -3.00f),
                    new Vector3(0.14f, 0.42f, 0.12f), leaf, false);
                l.localRotation = Quaternion.Euler(0f, 0f, (i - 2) * 12f);
            }
        }

        private void BuildKitchen()
        {
            Material cabinet = Mat("Kitchen Cabinet", new Color(0.73f, 0.69f, 0.60f));
            Material top = Mat("Kitchen Counter", new Color(0.16f, 0.17f, 0.18f));
            Material metal = Mat("Kitchen Metal", new Color(0.63f, 0.67f, 0.70f));

            for (int i = 0; i < 3; i++)
            {
                float x = 2.25f + i * 1.05f;
                Part("KitchenBase", PrimitiveType.Cube, root, new Vector3(x, 0.45f, 1.10f), new Vector3(0.96f, 0.88f, 0.62f), cabinet, true);
                Part("KitchenTop", PrimitiveType.Cube, root, new Vector3(x, 0.91f, 1.10f), new Vector3(1.02f, 0.06f, 0.66f), top, true);
            }
            Part("Sink", PrimitiveType.Cube, root, new Vector3(3.30f, 0.95f, 1.10f), new Vector3(0.55f, 0.05f, 0.38f), metal, false);
            for (int i = 0; i < 4; i++)
                Part("StoveRing", PrimitiveType.Cylinder, root,
                    new Vector3(4.35f + (i % 2 == 0 ? -0.20f : 0.20f), 0.96f, 1.10f + (i < 2 ? -0.17f : 0.17f)),
                    new Vector3(0.13f, 0.015f, 0.13f), top, false);
        }

        private void BuildBedroom()
        {
            Material frame = Mat("Bed Frame", new Color(0.29f, 0.17f, 0.10f));
            Material mattress = Mat("Mattress", new Color(0.88f, 0.88f, 0.86f));
            Material blanket = Mat("Blanket", new Color(0.29f, 0.47f, 0.66f));
            Transform bed = Group("Final_BedroomBed", new Vector3(-3.65f, 0f, 4.65f));
            Part("BedFrame", PrimitiveType.Cube, bed, new Vector3(0f, 0.22f, 0f), new Vector3(1.75f, 0.30f, 2.15f), frame, true);
            Part("Mattress", PrimitiveType.Cube, bed, new Vector3(0f, 0.48f, 0f), new Vector3(1.65f, 0.24f, 2.03f), mattress, false);
            Part("Blanket", PrimitiveType.Cube, bed, new Vector3(0f, 0.63f, -0.24f), new Vector3(1.58f, 0.06f, 1.30f), blanket, false);
            Part("Headboard", PrimitiveType.Cube, bed, new Vector3(0f, 0.73f, 1.04f), new Vector3(1.75f, 0.92f, 0.12f), frame, true);
            Part("BedPillow", PrimitiveType.Sphere, bed, new Vector3(0f, 0.69f, 0.58f), new Vector3(0.37f, 0.10f, 0.25f), mattress, false);
        }

        private void BuildWallWindow()
        {
            // Detect the authored rear-wall opening instead of guessing a hard-coded spot.
            Vector3 opening = DetectRearWallOpening(out float wallZ);
            Material frame = Mat("Window Frame", new Color(0.10f, 0.12f, 0.15f));
            Material glass = TransparentMat("Window Glass", new Color(0.45f, 0.72f, 0.90f, 0.22f));

            GameObject window = Part("HouseWindow_Final", PrimitiveType.Cube, root,
                new Vector3(opening.x, opening.y, wallZ - 0.025f), new Vector3(1.85f, 1.32f, 0.035f), glass, false).gameObject;
            window.AddComponent<BreakableWindow>().Configure("house-window-final", motion, logger);
            window.AddComponent<WindowView>().Configure();

            Part("WindowFrameTop", PrimitiveType.Cube, root, new Vector3(opening.x, opening.y + 0.70f, wallZ - 0.04f), new Vector3(2.05f, 0.08f, 0.10f), frame, false);
            Part("WindowFrameBottom", PrimitiveType.Cube, root, new Vector3(opening.x, opening.y - 0.70f, wallZ - 0.04f), new Vector3(2.05f, 0.08f, 0.10f), frame, false);
            Part("WindowFrameLeft", PrimitiveType.Cube, root, new Vector3(opening.x - 0.99f, opening.y, wallZ - 0.04f), new Vector3(0.08f, 1.48f, 0.10f), frame, false);
            Part("WindowFrameRight", PrimitiveType.Cube, root, new Vector3(opening.x + 0.99f, opening.y, wallZ - 0.04f), new Vector3(0.08f, 1.48f, 0.10f), frame, false);
            Part("WindowMullion", PrimitiveType.Cube, root, new Vector3(opening.x, opening.y, wallZ - 0.05f), new Vector3(0.055f, 1.32f, 0.10f), frame, false);
        }

        private Vector3 DetectRearWallOpening(out float wallZ)
        {
            // The authored House rear wall is near +Z. Sample horizontal rays through that wall.
            // A window hole appears as a compact no-hit region surrounded by wall hits.
            const float rayStartZ = 0.35f;
            const float rayDistance = 5.0f;
            const float fallbackZ = 2.24f;
            float bestScore = -1f;
            Vector3 best = new Vector3(0f, 1.65f, fallbackZ);
            float bestWallZ = fallbackZ;

            for (float y = 1.15f; y <= 2.15f; y += 0.10f)
            {
                for (float x = -4.2f; x <= 4.2f; x += 0.10f)
                {
                    if (HitsAuthoredWall(new Vector3(x, y, rayStartZ), rayDistance, out _)) continue;

                    // Candidate must have wall immediately around it, which rejects doors/open room space.
                    int boundary = 0;
                    float[] dx = { -1.05f, 1.05f, 0f, 0f };
                    float[] dy = { 0f, 0f, -0.78f, 0.78f };
                    float zAccum = 0f;
                    for (int k = 0; k < 4; k++)
                    {
                        if (HitsAuthoredWall(new Vector3(x + dx[k], y + dy[k], rayStartZ), rayDistance, out float hitZ))
                        {
                            boundary++;
                            zAccum += hitZ;
                        }
                    }
                    if (boundary < 3) continue;

                    // Prefer openings around normal window height and away from the extreme corners.
                    float score = boundary * 10f - Mathf.Abs(y - 1.65f) * 2f - Mathf.Abs(x) * 0.05f;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = new Vector3(x, y, fallbackZ);
                    bestWallZ = zAccum / boundary;
                }
            }

            wallZ = bestScore >= 0f ? bestWallZ : fallbackZ;
            Debug.Log($"CEVR window opening resolved at x={best.x:F2}, y={best.y:F2}, wallZ={wallZ:F2}." +
                      (bestScore < 0f ? " Using safe fallback because authored wall collision could not be sampled." : string.Empty));
            return best;
        }

        private bool HitsAuthoredWall(Vector3 origin, float distance, out float hitZ)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.forward, distance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                Transform t = hit.collider.transform;
                if (t.IsChildOf(root) || t == root) continue; // ignore all CEVR-generated gameplay props
                string n = t.name.ToLowerInvariant();
                if (n.Contains("floor") || n.Contains("ceiling") || n.Contains("furniture")) continue;
                hitZ = hit.point.z;
                return true;
            }
            hitZ = 0f;
            return false;
        }

        private Transform Group(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = position;
            return go.transform;
        }

        private Transform Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            Collider c = go.GetComponent<Collider>();
            if (c != null) c.enabled = collider;
            return go.transform;
        }

        private Material Mat(string name, Color color)
        {
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            Material m = new Material(shader) { name = name, color = color };
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.16f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.16f);
            ownedMaterials.Add(m);
            return m;
        }

        private Material TransparentMat(string name, Color color)
        {
            Material m = Mat(name, color);
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3f);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return m;
        }

        private static void HideRenderer(GameObject go)
        {
            if (go == null) return;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        private static void HideRendererByPrefix(params string[] prefixes)
        {
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                foreach (string prefix in prefixes)
                {
                    if (!r.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    r.enabled = false;
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Material m in ownedMaterials)
                if (m != null) Destroy(m);
            ownedMaterials.Clear();
        }
    }
}
