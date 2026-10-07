using System;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Shared placement math for both the edit-mode House preview and the runtime House.
    /// Dimensions are interpreted in the model's LOCAL axes, then yaw is applied. This avoids the
    /// old 90-degree scaling bug where rotated beds/sofas/pillows became stretched or flattened.
    /// </summary>
    public static class HouseFurnitureGeometry
    {
        public static bool FitAndPlace(Transform target, Vector3 localSize, Vector3 bottomCenter, float yaw)
        {
            if (target == null) return false;

            target.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            target.localScale = Vector3.one;

            if (!TryVisibleBounds(target, out Bounds source) ||
                source.size.x < 0.0001f || source.size.y < 0.0001f || source.size.z < 0.0001f)
                return false;

            target.localScale = new Vector3(
                localSize.x / source.size.x,
                localSize.y / source.size.y,
                localSize.z / source.size.z);

            // Rotate only after sizing in model-local axes.
            target.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (!TryVisibleBounds(target, out Bounds placed)) return false;
            target.position += bottomCenter -
                               new Vector3(placed.center.x, placed.min.y, placed.center.z);
            return true;
        }

        public static bool TryVisibleBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || IsCollisionVisual(renderer.transform, root)) continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        public static bool IsCollisionVisual(Transform candidate, Transform root)
        {
            Transform current = candidate;
            while (current != null)
            {
                if (current.name.IndexOf("collision", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (current == root) break;
                current = current.parent;
            }
            return false;
        }
    }
}
