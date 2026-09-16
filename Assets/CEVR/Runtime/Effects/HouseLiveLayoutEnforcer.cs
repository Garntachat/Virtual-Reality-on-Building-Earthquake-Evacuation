using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// One authoritative House-only correction pass for runtime-created furniture.
    /// Earlier post-passes were removed so this is the only component allowed to reposition the
    /// final Pleng/Kenney furniture after FurnitureSceneDressing has created it.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class HouseLiveLayoutEnforcer : MonoBehaviour
    {
        private Scene houseScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0) return;
            if (FindFirstObjectByType<HouseLiveLayoutEnforcer>() != null) return;
            GameObject host = new GameObject("CEVR_HouseLiveLayoutEnforcer");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<HouseLiveLayoutEnforcer>();
        }

        private IEnumerator Start()
        {
            houseScene = SceneManager.GetActiveScene();
            Debug.Log("CEVR HOUSE AUTHORITATIVE LAYOUT STARTED");

            // FurnitureSceneDressing creates its final Pleng/Kenney objects during Start(). Wait for
            // three rendered frames, then retry for up to ~3 seconds in case asset instantiation is late.
            yield return null;
            yield return null;
            yield return null;

            bool dining = false;
            bool bed = false;
            bool tv = false;
            bool ceiling = false;

            for (int frame = 0; frame < 180; frame++)
            {
                if (!dining) dining = FixDiningSet();
                if (!bed) bed = FixBed();
                if (!tv) tv = FixTelevision();
                if (!ceiling) ceiling = FixCeilingObjects();

                if (dining && bed && tv && ceiling) break;
                yield return null;
            }

            LogFinalState(dining, bed, tv, ceiling);
            enabled = false;
        }

        private bool FixDiningSet()
        {
            GameObject table = FindSceneObject("HouseSturdyTableTop");
            GameObject south = FindSceneObject("HouseChair_CoverObstacle");
            GameObject north = FindSceneObject("HouseChair_Spare");
            GameObject west = FindSceneObject("HouseChair_DiningLeft");
            GameObject east = FindSceneObject("HouseChair_DiningRight");
            if (table == null || south == null || north == null || west == null || east == null) return false;

            Transform tableVisual = FindDescendantContains(table.transform, "DiningTable");
            Transform southVisual = FindDescendantContains(south.transform, "DiningChair");
            Transform northVisual = FindDescendantContains(north.transform, "DiningChair");
            Transform westVisual = FindDescendantContains(west.transform, "DiningChair");
            Transform eastVisual = FindDescendantContains(east.transform, "DiningChair");
            if (tableVisual == null || southVisual == null || northVisual == null || westVisual == null || eastVisual == null)
                return false;

            Vector3 center = HouseSceneLayout.DiningTable;

            // Measured Pleng table source = 1.500 x 0.734 x 0.850 m. Keep almost exactly that size.
            table.transform.SetPositionAndRotation(center + Vector3.up * 0.76f, Quaternion.identity);
            tableVisual.rotation = Quaternion.identity;
            FitWorldBounds(tableVisual, new Vector3(1.55f, 0.76f, 0.88f), center);

            // Perfectly symmetric dining arrangement. Chair source width/depth ~= 0.42/0.40 m.
            // The seat backs point outward and every chair's sitting direction points at table center.
            PlaceChair(south, southVisual, new Vector3(center.x, HouseSceneLayout.FloorY, center.z - 0.78f), 180f);
            PlaceChair(north, northVisual, new Vector3(center.x, HouseSceneLayout.FloorY, center.z + 0.78f), 0f);
            PlaceChair(west, westVisual, new Vector3(center.x - 1.03f, HouseSceneLayout.FloorY, center.z), -90f);
            PlaceChair(east, eastVisual, new Vector3(center.x + 1.03f, HouseSceneLayout.FloorY, center.z), 90f);
            return true;
        }

        private static void PlaceChair(GameObject anchor, Transform visual, Vector3 bottomCenter, float yaw)
        {
            anchor.transform.SetPositionAndRotation(bottomCenter, Quaternion.Euler(0f, yaw, 0f));
            visual.rotation = Quaternion.Euler(0f, yaw, 0f);
            FitWorldBounds(visual, new Vector3(0.44f, 0.94f, 0.42f), bottomCenter);
        }

        private bool FixBed()
        {
            List<Transform> beds = FindActiveSceneTransformsExact("TeamFurniture_Bed");
            if (beds.Count == 0) return false;

            // The analyzer measured the second-floor walkable strip as x=4.25..6.25, z=0.25..3.00.
            // The upper stair reaches x<=5.0 for z>=1.5. Therefore use the guaranteed-clear strip
            // z<1.5 and orient the bed east-west. This footprint is fully inside that clear rectangle:
            // x=4.75..6.15, z=0.28..1.10.
            Vector3 bedBottom = new Vector3(5.45f, HouseSceneLayout.SecondFloorY, 0.69f);
            Transform keeper = beds[0];
            ReparentToGameplay(keeper);
            keeper.rotation = Quaternion.Euler(0f, 90f, 0f);
            FitWorldBounds(keeper, new Vector3(1.40f, 0.82f, 0.82f), bedBottom);

            // Never allow duplicate decorative beds to survive at the old stair/hallway location.
            for (int i = 1; i < beds.Count; i++)
                beds[i].gameObject.SetActive(false);

            GameObject protectivePillow = FindSceneObject("ProtectivePillow");
            List<Transform> pillows = FindActiveSceneTransformsExact("TeamFurniture_Bed_Pillow");
            foreach (Transform pillow in pillows)
            {
                if (protectivePillow != null && pillow.IsChildOf(protectivePillow.transform)) continue;
                ReparentToGameplay(pillow);
                pillow.rotation = Quaternion.Euler(0f, 90f, 0f);
                FitWorldBounds(pillow, new Vector3(0.48f, 0.08f, 0.24f),
                    new Vector3(5.78f, HouseSceneLayout.SecondFloorY + 0.51f, 0.69f));
            }
            return true;
        }

        private bool FixTelevision()
        {
            List<Transform> televisions = FindActiveSceneTransformsExact("Kenney_televisionModern");
            List<Transform> cabinets = FindActiveSceneTransformsExact("Kenney_cabinetTelevision");
            if (televisions.Count == 0 || cabinets.Count == 0) return false;

            // FurnitureSceneDressing creates these at -90 degrees. The user-confirmed visible screen
            // is on the opposite face, so +90 is exactly 180 degrees from the authored runtime yaw.
            foreach (Transform tv in televisions)
            {
                tv.position = HouseSceneLayout.Television + Vector3.up * 0.65f;
                tv.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            foreach (Transform cabinet in cabinets)
            {
                cabinet.position = HouseSceneLayout.Television;
                cabinet.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            return true;
        }

        private bool FixCeilingObjects()
        {
            const float ceilingUnderside = 3.96f;
            int fallingHazards = 0;

            foreach (GameObject go in FindSceneGameObjects())
            {
                if (!go.name.StartsWith("HouseFallingObject_", StringComparison.Ordinal)) continue;
                if (SnapVisibleTop(go.transform, ceilingUnderside)) fallingHazards++;
            }

            // Also snap every actual replacement book/lamp visual. This catches pivot offsets inside
            // the generated hazard parent instead of assuming the parent transform equals mesh top.
            foreach (Transform t in FindSceneTransforms())
            {
                if (!t.gameObject.activeInHierarchy) continue;
                bool books = t.name == "Kenney_books" && HasAncestorPrefix(t, "HouseFallingObject_");
                bool lamp = t.name == "Kenney_lampSquareCeiling";
                if (!books && !lamp) continue;
                if (!TryBounds(t, out Bounds bounds)) continue;
                if (lamp && (bounds.center.y < 2f || bounds.center.y > 5f)) continue;
                SnapVisibleTop(t, ceilingUnderside);
            }

            return fallingHazards >= 4;
        }

        private void LogFinalState(bool dining, bool bed, bool tv, bool ceiling)
        {
            Transform bedObject = FirstOrNull(FindActiveSceneTransformsExact("TeamFurniture_Bed"));
            Transform tvObject = FirstOrNull(FindActiveSceneTransformsExact("Kenney_televisionModern"));
            string bedState = bedObject == null ? "missing" : $"pos={bedObject.position}, rotY={bedObject.eulerAngles.y:F1}";
            string tvState = tvObject == null ? "missing" : $"pos={tvObject.position}, rotY={tvObject.eulerAngles.y:F1}";
            Debug.Log($"CEVR HOUSE AUTHORITATIVE LAYOUT FINAL: dining={dining}, bed={bed}, tv={tv}, ceiling={ceiling}; BED[{bedState}]; TV[{tvState}]");
        }

        private void ReparentToGameplay(Transform item)
        {
            GameObject gameplay = FindSceneObject("CEVR_UniversalGameplay");
            if (gameplay != null && item.parent != gameplay.transform)
                item.SetParent(gameplay.transform, true);
        }

        private GameObject FindSceneObject(string exactName)
        {
            foreach (GameObject go in FindSceneGameObjects())
                if (go.name == exactName) return go;
            return null;
        }

        private List<GameObject> FindSceneGameObjects()
        {
            var result = new List<GameObject>();
            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go.scene == houseScene) result.Add(go);
            return result;
        }

        private List<Transform> FindSceneTransforms()
        {
            var result = new List<Transform>();
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.gameObject.scene == houseScene) result.Add(t);
            return result;
        }

        private List<Transform> FindActiveSceneTransformsExact(string exactName)
        {
            var result = new List<Transform>();
            foreach (Transform t in FindSceneTransforms())
                if (t.name == exactName && t.gameObject.activeInHierarchy && HasVisibleRenderer(t)) result.Add(t);
            return result;
        }

        private static Transform FindDescendantContains(Transform root, string token)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 && HasVisibleRenderer(t)) return t;
            return null;
        }

        private static bool HasVisibleRenderer(Transform root)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                if (r.enabled) return true;
            return false;
        }

        private static bool HasAncestorPrefix(Transform child, string prefix)
        {
            Transform current = child.parent;
            while (current != null)
            {
                if (current.name.StartsWith(prefix, StringComparison.Ordinal)) return true;
                current = current.parent;
            }
            return false;
        }

        private static bool SnapVisibleTop(Transform target, float targetTop)
        {
            if (!TryBounds(target, out Bounds bounds)) return false;
            target.position += Vector3.up * (targetTop - bounds.max.y);
            return true;
        }

        private static void FitWorldBounds(Transform target, Vector3 desiredWorldSize, Vector3 bottomCenter)
        {
            if (target == null || !TryBounds(target, out Bounds before)) return;
            if (before.size.x < 0.0001f || before.size.y < 0.0001f || before.size.z < 0.0001f) return;

            Vector3 scale = target.localScale;
            target.localScale = new Vector3(
                scale.x * desiredWorldSize.x / before.size.x,
                scale.y * desiredWorldSize.y / before.size.y,
                scale.z * desiredWorldSize.z / before.size.z);

            if (!TryBounds(target, out Bounds after)) return;
            target.position += bottomCenter - new Vector3(after.center.x, after.min.y, after.center.z);
        }

        private static bool TryBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || CollisionNamed(renderer.transform, root)) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }

        private static bool CollisionNamed(Transform candidate, Transform root)
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

        private static Transform FirstOrNull(List<Transform> items) => items.Count == 0 ? null : items[0];
    }
}
