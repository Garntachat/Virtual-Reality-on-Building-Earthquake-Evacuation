using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// House-only visual correction that snaps first-floor hanging hazards and ceiling fixtures to
    /// the measured underside of the y=4.0 ProBuilder slab. Bounds alignment is used instead of mesh
    /// pivots so Kenney replacement books/lamps cannot float or clip through the ceiling.
    /// </summary>
    [DefaultExecutionOrder(26000)]
    public sealed class HouseCeilingAttachmentFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (FindFirstObjectByType<HouseCeilingAttachmentFix>() != null) return;
            new GameObject("CEVR_HouseCeilingAttachmentFix").AddComponent<HouseCeilingAttachmentFix>();
        }

        private IEnumerator Start()
        {
            // Let UniversalSceneGameplayBootstrap, FurnitureSceneDressing and the measured furniture
            // polish finish creating/replacing all visible children first.
            yield return null;
            yield return null;
            yield return null;

            float targetTop = HouseSceneLayout.FirstFloorCeilingUndersideY;
            int hazards = 0;
            int lamps = 0;

            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null || !go.name.StartsWith("HouseFallingObject_", StringComparison.Ordinal)) continue;
                if (SnapVisibleTop(go.transform, targetTop)) hazards++;
            }

            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null) continue;
                bool generatedCeilingLamp = t.name == "Kenney_lampSquareCeiling";
                bool authoredHangingLamp = t.name.StartsWith("HangingLamp_", StringComparison.Ordinal);
                if (!generatedCeilingLamp && !authoredHangingLamp) continue;

                if (!TryVisibleBounds(t, out Bounds bounds)) continue;
                // Only touch fixtures belonging to the ground floor. Do not pull upper-storey lights
                // down to the first-floor slab.
                if (bounds.center.y < HouseSceneLayout.FloorY + 1.0f ||
                    bounds.center.y > HouseSceneLayout.FirstFloorCeilingY + 0.75f) continue;

                if (SnapVisibleTop(t, targetTop)) lamps++;
            }

            Debug.Log($"CEVR HOUSE CEILING ATTACHMENTS READY: snapped {hazards} falling/book hazard(s) and {lamps} lamp fixture(s) to y={targetTop:F2} underside.");
        }

        private static bool SnapVisibleTop(Transform target, float targetTopY)
        {
            if (target == null || !TryVisibleBounds(target, out Bounds bounds)) return false;
            float delta = targetTopY - bounds.max.y;
            if (Mathf.Abs(delta) < 0.002f) return true;
            target.position += Vector3.up * delta;
            return true;
        }

        private static bool TryVisibleBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }
    }
}
