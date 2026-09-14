using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Corrects world dimensions for stable visuals parented beneath scaled gameplay bodies
    /// (especially movable chairs). Runs after HouseFurnitureStabilityPass.
    /// </summary>
    [DefaultExecutionOrder(31000)]
    public sealed class HouseFurnitureWorldScaleFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (FindFirstObjectByType<HouseFurnitureWorldScaleFix>() != null) return;
            new GameObject("CEVR_HouseFurnitureWorldScaleFix").AddComponent<HouseFurnitureWorldScaleFix>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            yield return null;

            foreach (MovableFurniture chair in FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None))
            {
                if (!chair.name.StartsWith("HouseChair_", StringComparison.Ordinal)) continue;
                Transform visual = chair.transform.Find("Stable_chairCushion");
                if (visual != null)
                    FitWorld(visual, new Vector3(0.62f, 0.98f, 0.62f), chair.transform.position);
            }

            FitChild("HouseSturdyTableTop", "Stable_table", new Vector3(3.40f, 0.95f, 1.80f), true);
            FitChild("HouseTallCabinet_Left", "Stable_kitchenFridge", new Vector3(1.05f, 2.30f, 0.72f), true);
            FitChild("HouseBookcase_Right", "Stable_bookcaseOpen", new Vector3(1.05f, 2.30f, 0.72f), true);

            GameObject pillow = GameObject.Find("ProtectivePillow");
            if (pillow != null)
            {
                Transform visual = pillow.transform.Find("Stable_pillowBlue");
                if (visual != null)
                    FitWorld(visual, new Vector3(0.78f, 0.20f, 0.50f), pillow.transform.position - Vector3.up * 0.10f);
            }
        }

        private static void FitChild(string parentName, string childName, Vector3 dimensions, bool ground)
        {
            GameObject parent = GameObject.Find(parentName);
            if (parent == null) return;
            Transform child = parent.transform.Find(childName);
            if (child == null) return;
            Vector3 bottom = ground
                ? new Vector3(parent.transform.position.x, 0f, parent.transform.position.z)
                : parent.transform.position;
            FitWorld(child, dimensions, bottom);
        }

        private static void FitWorld(Transform visual, Vector3 desired, Vector3 bottomCenter)
        {
            if (visual == null || !TryBounds(visual.gameObject, out Bounds bounds)) return;
            Vector3 size = bounds.size;
            if (size.x < 0.001f || size.y < 0.001f || size.z < 0.001f) return;

            Vector3 localScale = visual.localScale;
            visual.localScale = new Vector3(
                localScale.x * desired.x / size.x,
                localScale.y * desired.y / size.y,
                localScale.z * desired.z / size.z);

            if (!TryBounds(visual.gameObject, out bounds)) return;
            visual.position += bottomCenter - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private static bool TryBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }
    }
}
