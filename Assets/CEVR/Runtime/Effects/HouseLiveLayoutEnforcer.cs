using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Final House layout enforcer. This exists because the House visual dressing is created at
    /// runtime and earlier one-shot correction passes could execute before those objects existed.
    /// The enforcer waits for the actual Pleng/Kenney objects, applies the correction once, and
    /// then stops touching gameplay furniture so grabbing/earthquake physics remain free.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class HouseLiveLayoutEnforcer : MonoBehaviour
    {
        private bool diningDone;
        private bool bedDone;
        private bool tvDone;
        private float deadline;

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
            new GameObject("CEVR_HouseLiveLayoutEnforcer").AddComponent<HouseLiveLayoutEnforcer>();
        }

        private void Awake()
        {
            deadline = Time.unscaledTime + 5f;
            Debug.Log("CEVR HOUSE LIVE LAYOUT ENFORCER STARTED");
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime > deadline)
            {
                Debug.Log($"CEVR HOUSE LIVE LAYOUT FINAL: dining={diningDone}, bed={bedDone}, tv={tvDone}");
                enabled = false;
                return;
            }

            if (!diningDone) diningDone = TryFixDining();
            if (!bedDone) bedDone = TryFixBed();
            if (!tvDone) tvDone = TryFixTelevision();

            if (diningDone && bedDone && tvDone)
            {
                Debug.Log("CEVR HOUSE LIVE LAYOUT FINAL: dining=True, bed=True, tv=True");
                enabled = false;
            }
        }

        private static bool TryFixDining()
        {
            GameObject table = GameObject.Find("HouseSturdyTableTop");
            GameObject south = GameObject.Find("HouseChair_CoverObstacle");
            GameObject north = GameObject.Find("HouseChair_Spare");
            GameObject west = GameObject.Find("HouseChair_DiningLeft");
            GameObject east = GameObject.Find("HouseChair_DiningRight");
            if (table == null || south == null || north == null || west == null || east == null) return false;

            Transform tableVisual = FindDeep(table.transform, "TeamFurniture_DiningTable");
            Transform southVisual = FindDeep(south.transform, "TeamFurniture_DiningChair");
            Transform northVisual = FindDeep(north.transform, "TeamFurniture_DiningChair");
            Transform westVisual = FindDeep(west.transform, "TeamFurniture_DiningChair");
            Transform eastVisual = FindDeep(east.transform, "TeamFurniture_DiningChair");
            if (tableVisual == null || southVisual == null || northVisual == null || westVisual == null || eastVisual == null)
                return false;

            Vector3 c = HouseSceneLayout.DiningTable;
            table.transform.SetPositionAndRotation(c + Vector3.up * 0.82f, Quaternion.identity);
            tableVisual.rotation = Quaternion.identity;
            FitWorldBounds(tableVisual, new Vector3(1.65f, 0.81f, 0.94f), c);

            // Equal offsets from table center. All chairs face inward.
            PlaceChair(south, southVisual, new Vector3(c.x, HouseSceneLayout.FloorY, c.z - 0.82f), 180f);
            PlaceChair(north, northVisual, new Vector3(c.x, HouseSceneLayout.FloorY, c.z + 0.82f), 0f);
            PlaceChair(west, westVisual, new Vector3(c.x - 1.12f, HouseSceneLayout.FloorY, c.z), -90f);
            PlaceChair(east, eastVisual, new Vector3(c.x + 1.12f, HouseSceneLayout.FloorY, c.z), 90f);
            return true;
        }

        private static void PlaceChair(GameObject anchor, Transform visual, Vector3 bottomCenter, float yaw)
        {
            anchor.transform.SetPositionAndRotation(bottomCenter, Quaternion.Euler(0f, yaw, 0f));
            visual.rotation = anchor.transform.rotation;
            FitWorldBounds(visual, new Vector3(0.46f, 0.98f, 0.44f), bottomCenter);
        }

        private static bool TryFixBed()
        {
            Transform bed = FindSceneTransform("TeamFurniture_Bed");
            if (bed == null) return false;

            // Analyzer: upper-floor walkable area x=4.25..6.25, z=0.25..3.00 at y=4.0.
            // Stairs (1) reaches x=1..5 and z=1.5..3.0. Keep the entire bed east of x=5.05,
            // with a 1.10m width, so it cannot overlap the stair mesh.
            Vector3 bottom = new Vector3(5.68f, HouseSceneLayout.SecondFloorY, 1.33f);
            bed.rotation = Quaternion.identity;
            FitWorldBounds(bed, new Vector3(1.10f, 0.88f, 1.85f), bottom);

            GameObject protective = GameObject.Find("ProtectivePillow");
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null || t.name != "TeamFurniture_Bed_Pillow") continue;
                if (protective != null && t.IsChildOf(protective.transform)) continue;
                t.rotation = Quaternion.identity;
                FitWorldBounds(t, new Vector3(0.58f, 0.085f, 0.27f),
                    new Vector3(5.68f, HouseSceneLayout.SecondFloorY + 0.55f, 1.92f));
            }
            return true;
        }

        private static bool TryFixTelevision()
        {
            Transform tv = FindSceneTransform("Kenney_televisionModern");
            Transform cabinet = FindSceneTransform("Kenney_cabinetTelevision");
            if (tv == null || cabinet == null) return false;

            tv.position = HouseSceneLayout.Television + Vector3.up * 0.65f;
            cabinet.position = HouseSceneLayout.Television;

            // User's Game view shows the screen facing away from the sofa. Rotate both exactly
            // 180 degrees from the orientation created by FurnitureSceneDressing, rather than
            // guessing another absolute yaw.
            tv.rotation = tv.rotation * Quaternion.Euler(0f, 180f, 0f);
            cabinet.rotation = cabinet.rotation * Quaternion.Euler(0f, 180f, 0f);
            return true;
        }

        private static Transform FindSceneTransform(string exactName)
        {
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t != null && t.name == exactName) return t;
            return null;
        }

        private static Transform FindDeep(Transform root, string exactName)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == exactName) return t;
            return null;
        }

        private static void FitWorldBounds(Transform target, Vector3 desiredWorldSize, Vector3 bottomCenter)
        {
            if (target == null || !TryBounds(target, out Bounds before)) return;
            if (before.size.x < 0.0001f || before.size.y < 0.0001f || before.size.z < 0.0001f) return;

            Vector3 s = target.localScale;
            target.localScale = new Vector3(
                s.x * desiredWorldSize.x / before.size.x,
                s.y * desiredWorldSize.y / before.size.y,
                s.z * desiredWorldSize.z / before.size.z);

            if (!TryBounds(target, out Bounds after)) return;
            target.position += bottomCenter - new Vector3(after.center.x, after.min.y, after.center.z);
        }

        private static bool TryBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled || CollisionNamed(r.transform, root)) continue;
                if (!found) { bounds = r.bounds; found = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return found;
        }

        private static bool CollisionNamed(Transform t, Transform root)
        {
            while (t != null)
            {
                if (t.name.IndexOf("Collision", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (t == root) break;
                t = t.parent;
            }
            return false;
        }
    }
}
