using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    // Authoritative House visual pass. Runs last and fixes the world/local-position bug that caused
    // multiple furniture visuals to collapse to the same world location.
    [DefaultExecutionOrder(50000)]
    public sealed class HouseFinalLayoutV2 : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        private Transform root;
        private GroundMotionPlayer motion;
        private SessionLogger logger;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (FindFirstObjectByType<HouseFinalLayoutV2>() != null) return;
            new GameObject("CEVR_HouseFinalLayoutV2").AddComponent<HouseFinalLayoutV2>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 6; i++) yield return null;
            GameObject gameplay = GameObject.Find("CEVR_UniversalGameplay");
            if (gameplay == null) yield break;
            root = gameplay.transform;
            motion = FindFirstObjectByType<GroundMotionPlayer>();
            logger = FindFirstObjectByType<SessionLogger>();

            ClearOldVisualPasses();
            RebuildGameplayFurniture();
            BuildLivingRoom();
            BuildKitchen();
            BuildBedroom();
            BuildWindowInWallOpening();
            Debug.Log("CEVR HOUSE V2 READY: furniture uses correct local coordinates and no longer stacks at one location; window snapped to wall opening.");
        }

        private void ClearOldVisualPasses()
        {
            var kill = new List<GameObject>();
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null) continue;
                string n = t.name;
                if (n.StartsWith("TeamFurniture_", StringComparison.Ordinal) ||
                    n.StartsWith("Stable_", StringComparison.Ordinal) ||
                    n.StartsWith("Final_", StringComparison.Ordinal) ||
                    n == "SofaSeat" || n == "SofaBack" || n == "SofaArmL" || n == "SofaArmR" ||
                    n == "CoffeeTop" || n == "CoffeeLeg" || n == "PlantPot" || n == "PlantLeaf" ||
                    n == "KitchenBase" || n == "KitchenTop" || n == "Sink" || n == "StoveRing" ||
                    n == "BedFrame" || n == "Mattress" || n == "Blanket" || n == "Headboard" || n == "BedPillow" ||
                    n == "HouseWindow_Final" || n.StartsWith("WindowFrame", StringComparison.Ordinal) || n == "WindowMullion")
                    kill.Add(t.gameObject);
            }
            foreach (GameObject go in kill) if (go != null) Destroy(go);

            HideLegacy("HouseSofa", "HouseRug", "HousePlant", "HouseShelfBook", "HousePhoto");
            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go != null && go.name.StartsWith("HouseWindow_", StringComparison.Ordinal)) Destroy(go);
        }

        private void RebuildGameplayFurniture()
        {
            BuildChair("HouseChair_CoverObstacle", new Vector3(0f, 0f, -4.15f), 0f);
            BuildChair("HouseChair_DiningLeft", new Vector3(-1.65f, 0f, -2.70f), 90f);
            BuildChair("HouseChair_DiningRight", new Vector3(1.65f, 0f, -2.70f), -90f);
            BuildChair("HouseChair_Spare", new Vector3(3.55f, 0f, -4.55f), -35f);

            GameObject table = GameObject.Find("HouseSturdyTableTop");
            if (table != null)
            {
                table.transform.SetPositionAndRotation(new Vector3(0f, 0.90f, -2.70f), Quaternion.identity);
                HideSelf(table);
                Material wood = Mat("DiningTableWood", new Color(0.36f, 0.20f, 0.11f));
                PartLocal("V2_TableTop", table.transform, Vector3.zero, new Vector3(3.30f, 0.16f, 1.72f), wood, false);
                foreach (float x in new[] { -1.38f, 1.38f }) foreach (float z in new[] { -0.68f, 0.68f })
                    PartLocal("V2_TableLeg", table.transform, new Vector3(x, -0.42f, z), new Vector3(0.14f, 0.84f, 0.14f), wood, false);
            }

            GameObject fridge = GameObject.Find("HouseTallCabinet_Left");
            if (fridge != null)
            {
                fridge.transform.position = new Vector3(-4.35f, 1.15f, 0.35f);
                HideSelf(fridge);
                Material body = Mat("Fridge", new Color(0.76f, 0.80f, 0.84f));
                Material trim = Mat("FridgeTrim", new Color(0.08f, 0.10f, 0.12f));
                PartLocal("V2_FridgeBody", fridge.transform, Vector3.zero, new Vector3(1.02f, 2.24f, 0.68f), body, false);
                PartLocal("V2_FridgeHandle", fridge.transform, new Vector3(0.37f, 0.25f, -0.37f), new Vector3(0.06f, 0.72f, 0.05f), trim, false);
            }

            GameObject wardrobe = GameObject.Find("HouseBookcase_Right");
            if (wardrobe != null)
            {
                wardrobe.transform.position = new Vector3(4.35f, 1.15f, 0.55f);
                HideSelf(wardrobe);
                Material wood = Mat("Wardrobe", new Color(0.31f, 0.18f, 0.10f));
                Material trim = Mat("WardrobeTrim", new Color(0.10f, 0.07f, 0.05f));
                PartLocal("V2_WardrobeBody", wardrobe.transform, Vector3.zero, new Vector3(1.02f, 2.24f, 0.68f), wood, false);
                PartLocal("V2_WardrobeSplit", wardrobe.transform, new Vector3(0f, 0f, -0.35f), new Vector3(0.025f, 2.02f, 0.025f), trim, false);
            }

            GameObject pillow = GameObject.Find("ProtectivePillow");
            if (pillow != null)
            {
                pillow.transform.position = new Vector3(-3.65f, 0.76f, 4.45f);
                HideSelf(pillow);
                PartLocal("V2_ProtectivePillow", pillow.transform, Vector3.zero, new Vector3(0.42f, 0.10f, 0.29f), Mat("Pillow", new Color(0.76f, 0.87f, 0.95f)), false, PrimitiveType.Sphere);
            }
        }

        private void BuildChair(string objectName, Vector3 worldPosition, float yaw)
        {
            GameObject chair = GameObject.Find(objectName);
            if (chair == null) return;
            chair.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0f, yaw, 0f));
            foreach (Renderer r in chair.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            Material seat = Mat("ChairSeat", new Color(0.48f, 0.15f, 0.23f));
            Material frame = Mat("ChairFrame", new Color(0.27f, 0.15f, 0.08f));
            PartLocal("V2_ChairSeat", chair.transform, new Vector3(0f, 0.48f, 0f), new Vector3(0.66f, 0.10f, 0.62f), seat, false);
            PartLocal("V2_ChairBack", chair.transform, new Vector3(0f, 0.84f, 0.27f), new Vector3(0.62f, 0.48f, 0.09f), seat, false);
            foreach (float x in new[] { -0.25f, 0.25f }) foreach (float z in new[] { -0.23f, 0.23f })
                PartLocal("V2_ChairLeg", chair.transform, new Vector3(x, 0.23f, z), new Vector3(0.07f, 0.46f, 0.07f), frame, false);
        }

        private void BuildLivingRoom()
        {
            Transform group = Group("V2_LivingRoom", new Vector3(-3.45f, 0f, -4.65f));
            Material sofa = Mat("Sofa", new Color(0.35f, 0.48f, 0.59f));
            PartLocal("V2_SofaSeat", group, new Vector3(0f, 0.42f, 0f), new Vector3(2.15f, 0.42f, 0.82f), sofa, true);
            PartLocal("V2_SofaBack", group, new Vector3(0f, 0.88f, 0.32f), new Vector3(2.15f, 0.66f, 0.18f), sofa, true);
            PartLocal("V2_SofaArmL", group, new Vector3(-1.00f, 0.62f, 0f), new Vector3(0.18f, 0.55f, 0.82f), sofa, true);
            PartLocal("V2_SofaArmR", group, new Vector3(1.00f, 0.62f, 0f), new Vector3(0.18f, 0.55f, 0.82f), sofa, true);

            Material wood = Mat("CoffeeWood", new Color(0.33f, 0.18f, 0.09f));
            PartWorld("V2_CoffeeTop", new Vector3(-3.45f, 0.43f, -3.32f), new Vector3(1.45f, 0.10f, 0.72f), wood, true);
            PartWorld("V2_Rug", new Vector3(-3.45f, 0.018f, -3.56f), new Vector3(2.75f, 0.025f, 1.90f), Mat("Rug", new Color(0.21f, 0.40f, 0.48f)), false);
            PartWorld("V2_PlantPot", new Vector3(-4.70f, 0.28f, -3.00f), new Vector3(0.32f, 0.28f, 0.32f), Mat("Pot", new Color(0.43f, 0.22f, 0.12f)), true, PrimitiveType.Cylinder);
            for (int i = 0; i < 4; i++)
                PartWorld("V2_PlantLeaf", new Vector3(-4.82f + i * 0.08f, 0.68f + (i % 2) * 0.12f, -3.00f), new Vector3(0.14f, 0.38f, 0.12f), Mat("Leaf" + i, new Color(0.10f, 0.42f, 0.20f)), false, PrimitiveType.Sphere);
        }

        private void BuildKitchen()
        {
            Material baseMat = Mat("KitchenBase", new Color(0.72f, 0.68f, 0.59f));
            Material counter = Mat("Counter", new Color(0.16f, 0.17f, 0.18f));
            for (int i = 0; i < 3; i++)
            {
                float x = 2.25f + i * 1.05f;
                PartWorld("V2_KitchenBase", new Vector3(x, 0.45f, 1.12f), new Vector3(0.96f, 0.88f, 0.62f), baseMat, true);
                PartWorld("V2_KitchenCounter", new Vector3(x, 0.91f, 1.12f), new Vector3(1.02f, 0.06f, 0.66f), counter, true);
            }
            PartWorld("V2_Sink", new Vector3(3.30f, 0.95f, 1.12f), new Vector3(0.55f, 0.05f, 0.38f), Mat("SinkMetal", new Color(0.63f, 0.67f, 0.70f)), false);
        }

        private void BuildBedroom()
        {
            Transform bed = Group("V2_Bed", new Vector3(-3.65f, 0f, 4.65f));
            Material frame = Mat("BedFrame", new Color(0.29f, 0.17f, 0.10f));
            Material mattress = Mat("Mattress", new Color(0.88f, 0.88f, 0.86f));
            PartLocal("V2_BedFrame", bed, new Vector3(0f, 0.22f, 0f), new Vector3(1.75f, 0.30f, 2.15f), frame, true);
            PartLocal("V2_Mattress", bed, new Vector3(0f, 0.48f, 0f), new Vector3(1.65f, 0.24f, 2.03f), mattress, false);
            PartLocal("V2_Blanket", bed, new Vector3(0f, 0.63f, -0.24f), new Vector3(1.58f, 0.06f, 1.30f), Mat("Blanket", new Color(0.29f, 0.47f, 0.66f)), false);
            PartLocal("V2_Headboard", bed, new Vector3(0f, 0.73f, 1.04f), new Vector3(1.75f, 0.92f, 0.12f), frame, true);
            PartLocal("V2_BedPillow", bed, new Vector3(0f, 0.69f, 0.58f), new Vector3(0.37f, 0.10f, 0.25f), mattress, false, PrimitiveType.Sphere);
        }

        private void BuildWindowInWallOpening()
        {
            Vector3 center = DetectOpening(out float wallZ);
            Material glass = Transparent("WindowGlass", new Color(0.44f, 0.73f, 0.92f, 0.20f));
            Material frame = Mat("WindowFrame", new Color(0.10f, 0.12f, 0.15f));

            GameObject pane = PartWorld("V2_HouseWindow", new Vector3(center.x, center.y, wallZ - 0.025f), new Vector3(1.85f, 1.32f, 0.035f), glass, false).gameObject;
            pane.AddComponent<BreakableWindow>().Configure("house-window-v2", motion, logger);
            pane.AddComponent<WindowView>().Configure();
            PartWorld("V2_WindowTop", new Vector3(center.x, center.y + 0.70f, wallZ - 0.04f), new Vector3(2.05f, 0.08f, 0.10f), frame, false);
            PartWorld("V2_WindowBottom", new Vector3(center.x, center.y - 0.70f, wallZ - 0.04f), new Vector3(2.05f, 0.08f, 0.10f), frame, false);
            PartWorld("V2_WindowLeft", new Vector3(center.x - 0.99f, center.y, wallZ - 0.04f), new Vector3(0.08f, 1.48f, 0.10f), frame, false);
            PartWorld("V2_WindowRight", new Vector3(center.x + 0.99f, center.y, wallZ - 0.04f), new Vector3(0.08f, 1.48f, 0.10f), frame, false);
            PartWorld("V2_WindowMullion", new Vector3(center.x, center.y, wallZ - 0.05f), new Vector3(0.055f, 1.32f, 0.10f), frame, false);
        }

        private Vector3 DetectOpening(out float wallZ)
        {
            const float startZ = 0.35f;
            const float fallbackZ = 2.24f;
            Vector3 best = new Vector3(0f, 1.65f, fallbackZ);
            float scoreBest = -999f;
            float zBest = fallbackZ;

            for (float y = 1.15f; y <= 2.15f; y += 0.10f)
            for (float x = -4.2f; x <= 4.2f; x += 0.10f)
            {
                if (HitsHouse(new Vector3(x, y, startZ), out _)) continue;
                int walls = 0; float z = 0f;
                Vector2[] around = { new Vector2(-1.02f, 0f), new Vector2(1.02f, 0f), new Vector2(0f, -0.76f), new Vector2(0f, 0.76f) };
                foreach (Vector2 d in around)
                {
                    if (!HitsHouse(new Vector3(x + d.x, y + d.y, startZ), out float hitZ)) continue;
                    walls++; z += hitZ;
                }
                if (walls < 3) continue;
                float score = walls * 10f - Mathf.Abs(y - 1.65f) * 2f - Mathf.Abs(x) * 0.04f;
                if (score <= scoreBest) continue;
                scoreBest = score; best = new Vector3(x, y, fallbackZ); zBest = z / walls;
            }
            wallZ = scoreBest > 0f ? zBest : fallbackZ;
            Debug.Log($"CEVR V2 window target: ({best.x:F2}, {best.y:F2}, {wallZ:F2})" + (scoreBest <= 0f ? " [fallback]" : " [detected wall opening]"));
            return best;
        }

        private bool HitsHouse(Vector3 origin, out float hitZ)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.forward, 5f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit h in hits)
            {
                if (h.collider == null) continue;
                Transform t = h.collider.transform;
                if (t == root || t.IsChildOf(root)) continue;
                string n = t.name.ToLowerInvariant();
                if (n.Contains("floor") || n.Contains("ceiling") || n.Contains("furniture")) continue;
                hitZ = h.point.z; return true;
            }
            hitZ = 0f; return false;
        }

        private Transform Group(string name, Vector3 worldPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(root, true);
            go.transform.position = worldPosition;
            return go.transform;
        }

        private Transform PartWorld(string name, Vector3 worldPosition, Vector3 scale, Material material, bool collider, PrimitiveType type = PrimitiveType.Cube)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(root, true);
            go.transform.position = worldPosition;
            go.transform.localScale = scale;
            Finish(go, material, collider);
            return go.transform;
        }

        private Transform PartLocal(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool collider, PrimitiveType type = PrimitiveType.Cube)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            Finish(go, material, collider);
            return go.transform;
        }

        private static void Finish(GameObject go, Material material, bool collider)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) { r.sharedMaterial = material; r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            Collider c = go.GetComponent<Collider>();
            if (c != null) c.enabled = collider;
        }

        private Material Mat(string name, Color color)
        {
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            Material m = new Material(shader) { name = name, color = color };
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.16f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.16f);
            materials.Add(m); return m;
        }

        private Material Transparent(string name, Color color)
        {
            Material m = Mat(name, color);
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3f);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON"); return m;
        }

        private static void HideSelf(GameObject go)
        {
            Renderer r = go == null ? null : go.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        private static void HideLegacy(params string[] prefixes)
        {
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (string prefix in prefixes)
                    if (r.name.StartsWith(prefix, StringComparison.Ordinal)) { r.enabled = false; break; }
        }

        private void OnDestroy()
        {
            foreach (Material m in materials) if (m != null) Destroy(m);
            materials.Clear();
        }
    }
}
