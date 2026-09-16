using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Final, measured House-only correction pass.
    /// Uses HouseProBuilderLayoutAnalyzer results and the measured Pleng FBX visible bounds:
    /// ground floor y=1.0; upper landing y=4.0, x~4.25..6.25, z~0.25..3.0.
    /// It does not invent a second furniture set. It corrects the furniture already instantiated by
    /// FurnitureSceneDressing after that component has finished its normal Start pass.
    /// </summary>
    [DefaultExecutionOrder(25000)]
    public sealed class HouseMeasuredLayoutPolish : MonoBehaviour
    {
        private readonly List<Material> ownedMaterials = new List<Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (FindFirstObjectByType<HouseMeasuredLayoutPolish>() != null) return;
            new GameObject("CEVR_HouseMeasuredLayoutPolish").AddComponent<HouseMeasuredLayoutPolish>();
        }

        private IEnumerator Start()
        {
            // Bootstrap and FurnitureSceneDressing are synchronous, but two frames guarantees all
            // generated Pleng/Kenney children and WindowView objects exist before correction.
            yield return null;
            yield return null;

            CorrectDiningSet();
            CorrectLargePlengFurniture();
            MoveBedroomUpstairs();
            FaceTelevisionTowardSofa();
            RebuildMeasuredWindow();

            Debug.Log("CEVR HOUSE MEASURED POLISH READY: compact Pleng dining set, upstairs bed, TV facing sofa, and fitted south-wall window applied.");
        }

        private static void CorrectDiningSet()
        {
            SetAnchor("HouseChair_CoverObstacle", HouseSceneLayout.CoverObstacleChair, 0f);
            SetAnchor("HouseChair_DiningLeft", HouseSceneLayout.DiningLeftChair, 90f);
            SetAnchor("HouseChair_DiningRight", HouseSceneLayout.DiningRightChair, -90f);
            SetAnchor("HouseChair_Spare", HouseSceneLayout.SpareChair, -45f);

            // Measured source chair = 0.421 x 0.900 x 0.401 m. Keep it close to authored size.
            foreach (MovableFurniture chair in FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None))
            {
                if (chair == null || !chair.name.StartsWith("HouseChair_", StringComparison.Ordinal)) continue;
                Transform visual = FindChildDeep(chair.transform, "TeamFurniture_DiningChair");
                if (visual != null)
                    FitWorldBounds(visual, new Vector3(0.46f, 0.98f, 0.44f), chair.transform.position);
            }

            GameObject tableTop = GameObject.Find("HouseSturdyTableTop");
            if (tableTop != null)
            {
                // Measured source table = 1.500 x 0.734 x 0.850 m. Previous 3.4 m visual was more
                // than twice the authored model width and dominated the hallway.
                tableTop.transform.position = HouseSceneLayout.DiningTable + Vector3.up * 0.82f;
                Transform visual = FindChildDeep(tableTop.transform, "TeamFurniture_DiningTable");
                if (visual != null)
                    FitWorldBounds(visual, new Vector3(1.65f, 0.81f, 0.94f), HouseSceneLayout.DiningTable);
            }
        }

        private static void CorrectLargePlengFurniture()
        {
            // Sofa source bounds = 1.900 x 0.811 x 0.942 m. Preserve that proportion rather than
            // stretching it. Native sofa front is +Z; yaw +90 points it toward the TV at +X.
            Transform sofa = FindSceneTransform("TeamFurniture_Sofa", null);
            if (sofa != null)
            {
                sofa.rotation = Quaternion.Euler(0f, 90f, 0f);
                FitWorldBounds(sofa, new Vector3(2.05f, 0.88f, 1.02f), HouseSceneLayout.Sofa);
            }

            Transform sofaPillows = FindSceneTransform("TeamFurniture_Sofa_Pillows", null);
            if (sofaPillows != null)
            {
                sofaPillows.rotation = Quaternion.Euler(0f, 90f, 0f);
                FitWorldBounds(sofaPillows, new Vector3(1.55f, 0.21f, 0.25f),
                    HouseSceneLayout.Sofa + Vector3.up * 0.48f);
            }

            GameObject fridgeAnchor = GameObject.Find("HouseTallCabinet_Left");
            if (fridgeAnchor != null)
            {
                Transform fridge = FindChildDeep(fridgeAnchor.transform, "TeamFurniture_Fridge");
                if (fridge != null)
                    FitWorldBounds(fridge, new Vector3(0.78f, 1.95f, 0.76f), HouseSceneLayout.FridgeBottom);
            }

            GameObject wardrobeAnchor = GameObject.Find("HouseBookcase_Right");
            if (wardrobeAnchor != null)
            {
                Transform wardrobe = FindChildDeep(wardrobeAnchor.transform, "TeamFurniture_Wandrobe");
                if (wardrobe != null)
                    FitWorldBounds(wardrobe, new Vector3(1.01f, 2.03f, 0.68f), HouseSceneLayout.WardrobeBottom);
            }
        }

        private static void MoveBedroomUpstairs()
        {
            // Analyzer: upper usable strip is x=4.25..6.25, z=0.25..3.00 at y=4.0.
            // Measured Bed source = 2.109 x 1.506 x 3.805. Uniform 0.65 scale gives
            // ~1.371 x 0.979 x 2.473 m, fitting the landing while leaving ~0.6 m along x=4.25.
            Transform bed = FindSceneTransform("TeamFurniture_Bed", null);
            if (bed != null)
            {
                bed.rotation = Quaternion.identity;
                FitWorldBounds(bed, new Vector3(1.371f, 0.979f, 2.473f), HouseSceneLayout.Bed);
            }

            // Do not touch the gameplay ProtectivePillow; this selects only the decorative bed pillow.
            GameObject protective = GameObject.Find("ProtectivePillow");
            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (candidate == null || candidate.name != "TeamFurniture_Bed_Pillow") continue;
                if (protective != null && candidate.IsChildOf(protective.transform)) continue;
                candidate.rotation = Quaternion.identity;
                FitWorldBounds(candidate, new Vector3(0.651f, 0.091f, 0.300f), HouseSceneLayout.BedPillow);
            }
        }

        private static void FaceTelevisionTowardSofa()
        {
            // Sofa is west of the television. Current Kenney orientation showed the back of the TV
            // to the sofa in the user's Game view, so flip both TV and cabinet by 180 degrees.
            Transform tv = FindSceneTransform("Kenney_televisionModern", null);
            if (tv != null)
            {
                tv.position = HouseSceneLayout.Television + Vector3.up * 0.65f;
                tv.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            Transform cabinet = FindSceneTransform("Kenney_cabinetTelevision", null);
            if (cabinet != null)
            {
                cabinet.position = HouseSceneLayout.Television;
                cabinet.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

        private void RebuildMeasuredWindow()
        {
            // Analyzer found the south-wall aperture around x=-2.0, z=-7.5 with a solid lower sill.
            // Remove the generated window and replace it with one clean frame sized *inside* the hole.
            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (candidate == null) continue;
                if (!candidate.name.StartsWith("HouseWindow_", StringComparison.Ordinal) &&
                    candidate.name != "HouseWindow_FittedAperture") continue;
                candidate.gameObject.SetActive(false);
                Destroy(candidate.gameObject);
            }

            Transform gameplay = GameObject.Find("CEVR_UniversalGameplay")?.transform;
            var root = new GameObject("HouseWindow_FittedAperture");
            if (gameplay != null) root.transform.SetParent(gameplay, true);
            root.transform.SetPositionAndRotation(HouseSceneLayout.WindowCenter, Quaternion.identity);

            Material frame = MaterialFor("CEVR Fitted Window Frame", new Color(0.08f, 0.10f, 0.12f));
            Material temporaryGlass = MaterialFor("CEVR Fitted Window Glass Seed", new Color(0.42f, 0.72f, 0.92f, 0.18f));
            Vector3 size = HouseSceneLayout.WindowSize;
            const float frameWidth = 0.055f;
            const float frameDepth = 0.085f;

            GameObject pane = Cube("WindowGlass_Fitted", root.transform, Vector3.zero,
                size, temporaryGlass, false);
            Cube("WindowFrame_Top", root.transform,
                new Vector3(0f, size.y * 0.5f + frameWidth * 0.5f, 0f),
                new Vector3(size.x + frameWidth * 2f, frameWidth, frameDepth), frame, false);
            Cube("WindowFrame_Bottom", root.transform,
                new Vector3(0f, -size.y * 0.5f - frameWidth * 0.5f, 0f),
                new Vector3(size.x + frameWidth * 2f, frameWidth, frameDepth), frame, false);
            Cube("WindowFrame_Left", root.transform,
                new Vector3(-size.x * 0.5f - frameWidth * 0.5f, 0f, 0f),
                new Vector3(frameWidth, size.y, frameDepth), frame, false);
            Cube("WindowFrame_Right", root.transform,
                new Vector3(size.x * 0.5f + frameWidth * 0.5f, 0f, 0f),
                new Vector3(frameWidth, size.y, frameDepth), frame, false);
            Cube("WindowFrame_Mullion", root.transform, Vector3.zero,
                new Vector3(0.035f, size.y, frameDepth), frame, false);

            GroundMotionPlayer motion = FindFirstObjectByType<GroundMotionPlayer>();
            SessionLogger logger = FindFirstObjectByType<SessionLogger>();
            pane.AddComponent<BreakableWindow>().Configure("house-window-fitted", motion, logger);
            pane.AddComponent<WindowView>().Configure();
        }

        private static void SetAnchor(string name, Vector3 position, float yaw)
        {
            GameObject anchor = GameObject.Find(name);
            if (anchor != null)
                anchor.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        }

        private static Transform FindSceneTransform(string exactName, Transform excludeAncestor)
        {
            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (candidate == null || candidate.name != exactName) continue;
                if (excludeAncestor != null && candidate.IsChildOf(excludeAncestor)) continue;
                return candidate;
            }
            return null;
        }

        private static Transform FindChildDeep(Transform root, string exactName)
        {
            if (root == null) return null;
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                if (candidate.name == exactName) return candidate;
            return null;
        }

        private static void FitWorldBounds(Transform target, Vector3 desiredWorldSize, Vector3 bottomCenter)
        {
            if (target == null || !TryGetVisibleBounds(target, out Bounds before)) return;
            if (before.size.x < 0.0001f || before.size.y < 0.0001f || before.size.z < 0.0001f) return;

            Vector3 scale = target.localScale;
            target.localScale = new Vector3(
                scale.x * desiredWorldSize.x / before.size.x,
                scale.y * desiredWorldSize.y / before.size.y,
                scale.z * desiredWorldSize.z / before.size.z);

            if (!TryGetVisibleBounds(target, out Bounds after)) return;
            target.position += bottomCenter - new Vector3(after.center.x, after.min.y, after.center.z);
        }

        private static bool TryGetVisibleBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || IsCollisionVisual(renderer.transform, root)) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
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

        private Material MaterialFor(string name, Color color)
        {
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader == null) return null;
            Material material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.20f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.20f);
            ownedMaterials.Add(material);
            return material;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            Collider c = go.GetComponent<Collider>();
            if (c != null) c.enabled = collider;
            return go;
        }

        private void OnDestroy()
        {
            foreach (Material material in ownedMaterials)
                if (material != null) Destroy(material);
            ownedMaterials.Clear();
        }
    }
}
