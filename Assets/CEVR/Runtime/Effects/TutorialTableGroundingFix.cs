using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Keeps the tutorial cover-table visual on the tutorial floor. HouseSceneLayout.FloorY is
    /// intentionally 1 m for the ProBuilder House and must never be reused as the tutorial floor.
    /// </summary>
    [DefaultExecutionOrder(30000)]
    public sealed class TutorialTableGroundingFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;
            string sceneName = scene.name.ToLowerInvariant();
            if (sceneName.Contains("house") || !sceneName.Contains("tutorial")) return;
            if (FindFirstObjectByType<TutorialTableGroundingFix>() != null) return;

            GameObject host = new GameObject("CEVR_TutorialTableGroundingFix");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<TutorialTableGroundingFix>();
        }

        private IEnumerator Start()
        {
            // FurnitureSceneDressing creates/replaces the visual in Start, so wait until it exists.
            for (int frame = 0; frame < 120; frame++)
            {
                GameObject table = GameObject.Find("SturdyCoverTableTop");
                if (table != null)
                {
                    Transform visual = FindDiningTableVisual(table.transform);
                    if (visual != null && TryVisibleBounds(visual, out Bounds bounds))
                    {
                        // Tutorial geometry is authored around world Y=0. Ground the visible Pleng
                        // table by its actual rendered bounds so FBX pivot offsets cannot make it float.
                        visual.position += Vector3.up * (0f - bounds.min.y);
                        Debug.Log($"CEVR tutorial table grounded: bottomY={0f:F2}, visual={visual.name}");
                        yield break;
                    }
                }
                yield return null;
            }

            Debug.LogWarning("CEVR tutorial table grounding could not find the final DiningTable visual.");
        }

        private static Transform FindDiningTableVisual(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.IndexOf("DiningTable", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (TryVisibleBounds(t, out _)) return t;
            }
            return null;
        }

        private static bool TryVisibleBounds(Transform root, out Bounds bounds)
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
    }
}
