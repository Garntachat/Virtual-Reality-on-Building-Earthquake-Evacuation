using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Final house-only furniture pass. FurnitureSceneDressing owns the normal visual replacement;
    /// this guard runs one frame later to make the complete Pleng furniture set deterministic,
    /// snap free-standing pieces back to the intended condo layout, remove accidental duplicate
    /// standalone visuals, and verify gameplay replacements without touching the authored house mesh.
    /// </summary>
    [DefaultExecutionOrder(20000)]
    public sealed class HouseFurnitureLayoutGuard : MonoBehaviour
    {
        private const string TeamFurniturePath = "PlengFurniture/";
        private const string UniversalRootName = "CEVR_UniversalGameplay";
        private readonly List<Material> ownedMaterials = new List<Material>();
        private Transform gameplayRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForHouse()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (FindFirstObjectByType<HouseFurnitureLayoutGuard>() != null) return;

            var guard = new GameObject("CEVR_HouseFurnitureLayoutGuard");
            guard.AddComponent<HouseFurnitureLayoutGuard>();
        }

        private IEnumerator Start()
        {
            // UniversalSceneGameplayBootstrap creates FurnitureSceneDressing after scene load and
            // FurnitureSceneDressing performs its work in Start(). Waiting a frame guarantees that
            // this pass observes the final generated placeholders and replacement models.
            yield return null;
            RepairAndValidate();
        }

        private void RepairAndValidate()
        {
            GameObject rootObject = GameObject.Find(UniversalRootName);
            if (rootObject == null)
            {
                Debug.LogWarning("CEVR house furniture guard could not find the universal gameplay root.");
                return;
            }
            gameplayRoot = rootObject.transform;

            // Gameplay-owned furniture: retain the original Rigidbody/colliders/scripts and only
            // guarantee that the Pleng visual exists as the object's child.
            int chairs = 0;
            foreach (MovableFurniture chair in FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None))
            {
                if (!chair.name.StartsWith("HouseChair_", StringComparison.Ordinal)) continue;
                chairs++;
                EnsureReplacement(chair.gameObject, "DiningChair", new Vector3(0.64f, 0.96f, 0.64f),
                    chair.transform.position, true);
            }

            GameObject table = GameObject.Find("HouseSturdyTableTop");
            if (table != null)
                EnsureReplacement(table, "DiningTable", new Vector3(3.4f, 0.99f, 1.8f),
                    new Vector3(table.transform.position.x, 0f, table.transform.position.z), false);

            GameObject fridge = GameObject.Find("HouseTallCabinet_Left");
            if (fridge != null)
                EnsureReplacement(fridge, "Fridge", new Vector3(1.05f, 2.3f, 0.72f),
                    new Vector3(fridge.transform.position.x, 0f, fridge.transform.position.z), false);

            GameObject wardrobe = GameObject.Find("HouseBookcase_Right");
            if (wardrobe != null)
                EnsureReplacement(wardrobe, "Wandrobe", new Vector3(1.05f, 2.3f, 0.72f),
                    new Vector3(wardrobe.transform.position.x, 0f, wardrobe.transform.position.z), false);

            GameObject protectivePillow = GameObject.Find("ProtectivePillow");
            if (protectivePillow != null)
            {
                Vector3 pillowBottom = protectivePillow.transform.position - Vector3.up * 0.10f;
                EnsureReplacement(protectivePillow, "Bed_Pillow", new Vector3(0.78f, 0.20f, 0.50f),
                    pillowBottom, false);
            }

            // Static room dressing. These coordinates deliberately leave the spawn -> cover -> exit
            // corridor open: living room stays on the far left, bedroom stays at +Z, and kitchen at right.
            GameObject sofa = EnsureStandalone("Sofa", new Vector3(-3.50f, 0f, -4.60f),
                new Vector3(2.20f, 0.95f, 1.00f), 0f, false);
            HidePlaceholderRenderers("HouseSofaBase", "HouseSofaBack");

            float sofaCushionBottom = 0.50f;
            if (sofa != null && TryGetVisibleBounds(sofa, out Bounds sofaBounds))
                sofaCushionBottom = Mathf.Clamp(sofaBounds.min.y + sofaBounds.size.y * 0.50f, 0.42f, 0.62f);
            EnsureStandalone("Sofa_Pillows", new Vector3(-3.50f, sofaCushionBottom, -4.83f),
                new Vector3(1.45f, 0.42f, 0.28f), 0f, false);

            GameObject bed = EnsureStandalone("Bed", new Vector3(-3.80f, 0f, 4.55f),
                new Vector3(1.65f, 0.68f, 2.10f), 0f, true);
            float bedTop = 0.68f;
            if (bed != null && TryGetVisibleBounds(bed, out Bounds bedBounds)) bedTop = bedBounds.max.y;
            EnsureStandalone("Bed_Pillow", new Vector3(-3.80f, bedTop, 5.15f),
                new Vector3(0.72f, 0.18f, 0.42f), 0f, false);

            float coffeeTop = 0.49f;
            GameObject coffee = GameObject.Find("HouseCoffeeTable");
            if (coffee != null)
            {
                Collider coffeeCollider = coffee.GetComponent<Collider>();
                if (coffeeCollider != null && coffeeCollider.enabled) coffeeTop = coffeeCollider.bounds.max.y + 0.01f;
            }
            GameObject vase = EnsureStandalone("Vase", new Vector3(-3.45f, coffeeTop, -3.25f),
                new Vector3(0.28f, 0.62f, 0.28f), 0f, true);
            EnsureVasePhysics(vase);

            // Hide only legacy/generated visuals. Their colliders remain where they are needed for
            // gameplay, so this pass cannot accidentally remove cover, hazard or navigation collision.
            HideByPrefix("HouseSofa", "HousePlant", "HouseShelfBook", "HousePhoto", "HouseRug");

            ValidateLayout(chairs);
        }

        private void EnsureReplacement(
            GameObject target, string modelName, Vector3 dimensions, Vector3 bottomCenter, bool bottomAtTarget)
        {
            if (target == null) return;
            Transform existing = target.transform.Find("TeamFurniture_" + modelName);
            if (existing != null)
            {
                SnapToBottom(existing.gameObject, bottomAtTarget ? target.transform.position : bottomCenter,
                    target.transform.rotation);
                return;
            }

            Renderer[] oldRenderers = target.GetComponentsInChildren<Renderer>(true);
            GameObject replacement = CreateTeamModel(modelName, target.transform,
                bottomAtTarget ? target.transform.position : bottomCenter, dimensions, target.transform.rotation);
            if (replacement == null) return;

            foreach (Renderer renderer in oldRenderers)
            {
                if (renderer == null || renderer.transform.IsChildOf(replacement.transform)) continue;
                if (renderer.name.IndexOf("WarningStripe", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                renderer.enabled = false;
            }
        }

        private GameObject EnsureStandalone(
            string modelName, Vector3 bottomCenter, Vector3 dimensions, float yaw, bool addCollider)
        {
            List<GameObject> matches = DirectTeamChildren(modelName);
            GameObject result = matches.Count > 0 ? matches[0] :
                CreateTeamModel(modelName, gameplayRoot, bottomCenter, dimensions, Quaternion.Euler(0f, yaw, 0f));
            if (result == null) return null;

            // A previous experimental dressing pass could have spawned the same standalone model
            // more than once. Keep one deterministic copy; gameplay-owned child replacements are not touched.
            for (int i = 1; i < matches.Count; i++)
                if (matches[i] != null) Destroy(matches[i]);

            SnapToBottom(result, bottomCenter, Quaternion.Euler(0f, yaw, 0f));
            if (addCollider) EnsureBoundsCollider(result);
            return result;
        }

        private List<GameObject> DirectTeamChildren(string modelName)
        {
            var matches = new List<GameObject>();
            if (gameplayRoot == null) return matches;
            string wanted = "TeamFurniture_" + modelName;
            for (int i = 0; i < gameplayRoot.childCount; i++)
            {
                Transform child = gameplayRoot.GetChild(i);
                if (child.name == wanted) matches.Add(child.gameObject);
            }
            return matches;
        }

        private GameObject CreateTeamModel(
            string modelName, Transform parent, Vector3 bottomCenter, Vector3 dimensions, Quaternion rotation)
        {
            GameObject source = Resources.Load<GameObject>(TeamFurniturePath + modelName);
            if (source == null)
            {
                Debug.LogWarning("CEVR house layout could not load Pleng furniture: " + modelName + ".");
                return null;
            }

            GameObject model = Instantiate(source);
            model.name = "TeamFurniture_" + modelName;
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;

            foreach (Collider imported in model.GetComponentsInChildren<Collider>(true)) imported.enabled = false;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                if (IsCollisionVisual(renderer.transform, model.transform)) renderer.enabled = false;

            ConvertMaterials(model);
            if (!TryGetVisibleBounds(model, out Bounds sourceBounds) || !ValidSize(sourceBounds.size))
            {
                Debug.LogWarning("CEVR house layout found invalid visible bounds for Pleng furniture: " + modelName + ".");
                Destroy(model);
                return null;
            }

            model.transform.localScale = new Vector3(
                dimensions.x / sourceBounds.size.x,
                dimensions.y / sourceBounds.size.y,
                dimensions.z / sourceBounds.size.z);
            model.transform.rotation = rotation;
            if (!TryGetVisibleBounds(model, out Bounds scaledBounds) || !ValidSize(scaledBounds.size))
            {
                Destroy(model);
                return null;
            }

            model.transform.position += bottomCenter -
                new Vector3(scaledBounds.center.x, scaledBounds.min.y, scaledBounds.center.z);
            model.transform.SetParent(parent, true);
            return model;
        }

        private void SnapToBottom(GameObject model, Vector3 bottomCenter, Quaternion rotation)
        {
            if (model == null) return;
            model.transform.rotation = rotation;
            if (!TryGetVisibleBounds(model, out Bounds bounds)) return;
            model.transform.position += bottomCenter - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private static bool ValidSize(Vector3 size)
        {
            return IsFinite(size.x) && IsFinite(size.y) && IsFinite(size.z) &&
                   size.x > 0.001f && size.y > 0.001f && size.z > 0.001f;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private void ConvertMaterials(GameObject model)
        {
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null
                ? "Standard" : "Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;

            var replacements = new Dictionary<Material, Material>();
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material original = slots[i];
                    if (original != null && replacements.TryGetValue(original, out Material cached))
                    {
                        slots[i] = cached;
                        continue;
                    }

                    Color color = new Color(0.72f, 0.72f, 0.72f);
                    Texture texture = null;
                    if (original != null)
                    {
                        if (original.HasProperty("_BaseColor")) color = original.GetColor("_BaseColor");
                        else if (original.HasProperty("_Color")) color = original.GetColor("_Color");
                        if (original.HasProperty("_BaseMap")) texture = original.GetTexture("_BaseMap");
                        else if (original.HasProperty("_MainTex")) texture = original.GetTexture("_MainTex");
                    }

                    var material = new Material(shader)
                    {
                        name = "CEVR House " + (original == null ? model.name : original.name),
                        color = color
                    };
                    if (texture != null)
                    {
                        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                    }
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
                    if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.22f);
                    ownedMaterials.Add(material);
                    if (original != null) replacements[original] = material;
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

        private static void EnsureBoundsCollider(GameObject root)
        {
            if (root == null) return;
            BoxCollider existing = root.GetComponent<BoxCollider>();
            if (existing != null && existing.enabled) return;
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

        private static void EnsureVasePhysics(GameObject vase)
        {
            if (vase == null) return;
            Rigidbody body = vase.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = vase.AddComponent<Rigidbody>();
                body.mass = 1.1f;
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            if (vase.GetComponent<InertialRigidbody>() == null)
            {
                GroundMotionPlayer motion = FindFirstObjectByType<GroundMotionPlayer>();
                vase.AddComponent<InertialRigidbody>().Configure(motion, 1.05f, true);
            }
            if (vase.GetComponent<FurnitureImpactAudio>() == null) vase.AddComponent<FurnitureImpactAudio>();
        }

        private static void HidePlaceholderRenderers(params string[] names)
        {
            foreach (string name in names)
            {
                GameObject target = GameObject.Find(name);
                if (target == null) continue;
                Renderer renderer = target.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;
            }
        }

        private static void HideByPrefix(params string[] prefixes)
        {
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                foreach (string prefix in prefixes)
                {
                    if (renderer.name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        renderer.enabled = false;
                        break;
                    }
                }
            }
        }

        private void ValidateLayout(int chairCount)
        {
            var missing = new List<string>();
            if (chairCount != 4) missing.Add("4 house chairs (found " + chairCount + ")");
            RequireChild("HouseSturdyTableTop", "DiningTable", missing);
            RequireChild("HouseTallCabinet_Left", "Fridge", missing);
            RequireChild("HouseBookcase_Right", "Wandrobe", missing);
            RequireChild("ProtectivePillow", "Bed_Pillow", missing);
            RequireStandalone("Sofa", missing);
            RequireStandalone("Sofa_Pillows", missing);
            RequireStandalone("Bed", missing);
            RequireStandalone("Bed_Pillow", missing);
            RequireStandalone("Vase", missing);

            foreach (MovableFurniture chair in FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None))
                if (chair.name.StartsWith("HouseChair_", StringComparison.Ordinal) &&
                    chair.transform.Find("TeamFurniture_DiningChair") == null)
                    missing.Add(chair.name + " -> DiningChair");

            int invalidBounds = 0;
            foreach (Renderer renderer in gameplayRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.name.StartsWith("TeamFurniture_", StringComparison.Ordinal) &&
                    renderer.transform.root.name != "TeamFurniture") continue;
                Bounds bounds = renderer.bounds;
                if (!ValidSize(bounds.size) || !IsFinite(bounds.center.x) || !IsFinite(bounds.center.y) || !IsFinite(bounds.center.z))
                    invalidBounds++;
            }

            if (missing.Count == 0 && invalidBounds == 0)
                Debug.Log("CEVR house furniture layout ready: all Pleng furniture is placed/replaced while original gameplay collision and house mesh remain intact.");
            else
                Debug.LogWarning("CEVR house furniture layout completed with checks to review: " +
                                 string.Join(", ", missing) +
                                 (invalidBounds > 0 ? $"; invalid renderer bounds={invalidBounds}" : string.Empty));
        }

        private static void RequireChild(string targetName, string modelName, List<string> missing)
        {
            GameObject target = GameObject.Find(targetName);
            if (target == null || target.transform.Find("TeamFurniture_" + modelName) == null)
                missing.Add(targetName + " -> " + modelName);
        }

        private void RequireStandalone(string modelName, List<string> missing)
        {
            if (DirectTeamChildren(modelName).Count == 0) missing.Add("standalone " + modelName);
        }

        private void OnDestroy()
        {
            foreach (Material material in ownedMaterials)
                if (material != null) Destroy(material);
            ownedMaterials.Clear();
        }
    }
}
