using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Final House layout enforcer. It waits for runtime dressing, then fixes the actual visible
    /// Pleng/Kenney objects. It keeps retrying briefly because those visuals are created during Start.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class HouseLiveLayoutEnforcer : MonoBehaviour
    {
        private bool diningDone;
        private bool bedDone;
        private bool tvDone;
        private bool ceilingDone;
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
            deadline = Time.unscaledTime + 8f;
            Debug.Log("CEVR HOUSE LIVE LAYOUT ENFORCER STARTED");
        }

        private void LateUpdate()
        {
            if (!diningDone) diningDone = TryFixDining();
            if (!bedDone) bedDone = TryFixBed();
            if (!tvDone) tvDone = TryFixTelevision();
            if (!ceilingDone) ceilingDone = TryFixCeilingObjects();

            if (diningDone && bedDone && tvDone && ceilingDone)
            {
                Debug.Log("CEVR HOUSE LIVE LAYOUT FINAL: dining=True, bed=True, tv=True, ceiling=True");
                enabled = false;
                return;
            }

            if (Time.unscaledTime > deadline)
            {
                Debug.LogWarning($"CEVR HOUSE LIVE LAYOUT PARTIAL: dining={diningDone}, bed={bedDone}, tv={tvDone}, ceiling={ceilingDone}");
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

            PlaceChair(south, southVisual, new Vector3(c.x, HouseSceneLayout.FloorY, c.z - 0.80f), 180f);
            PlaceChair(north, northVisual, new Vector3(c.x, HouseSceneLayout.FloorY, c.z + 0.80f), 0f);
            PlaceChair(west, westVisual, new Vector3(c.x - 1.08f, HouseSceneLayout.FloorY, c.z), -90f);
            PlaceChair(east, eastVisual, new Vector3(c.x + 1.08f, HouseSceneLayout.FloorY, c.z), 90f);
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

            // The upper stair occupies x<=5 and z>=1.5. Put the bed crosswise in the clear strip
            // z=0.25..1.35, x=4.35..6.05 so no part of the bed touches the stair footprint.
            Vector3 bottom = new Vector3(5.20f, HouseSceneLayout.SecondFloorY, 0.80f);
            bed.rotation = Quaternion.Euler(0f, 90f, 0f);
            FitWorldBounds(bed, new Vector3(1.70f, 0.88f, 0.90f), bottom);

            GameObject protective = GameObject.Find("ProtectivePillow");
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null || t.name != "TeamFurniture_Bed_Pillow") continue;
                if (protective != null && t.IsChildOf(protective.transform)) continue;
                t.rotation = Quaternion.Euler(0f, 90f, 0f);
                FitWorldBounds(t, new Vector3(0.48f, 0.085f, 0.24f),
                    new Vector3(5.70f, HouseSceneLayout.SecondFloorY + 0.54f, 0.80f));
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

            // FurnitureSceneDressing creates both at -90 degrees. The model's visible screen is on
            // the opposite face, so +90 is the exact 180-degree correction requested by the user.
            tv.rotation = Quaternion.Euler(0f, 90f, 0f);
            cabinet.rotation = Quaternion.Euler(0f, 90f, 0f);
            return true;
        }

        private static bool TryFixCeilingObjects()
        {
            int found = 0;
            const float targetTop = 3.96f;

            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null || !go.name.StartsWith("HouseFallingObject_", StringComparison.Ordinal)) continue;
                if (SnapTop(go.transform, targetTop)) found++;
            }

            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t == null) continue;
                if (t.name != "Kenney_lampSquareCeiling" &&
                    !t.name.StartsWith("HangingLamp_", StringComparison.Ordinal)) continue;
                if (!TryBounds(t, out Bounds b)) continue;
                if (b.center.y < 2f || b.center.y > 4.8f) continue;
                if (SnapTop(t, targetTop)) found++;
            }

            // Bootstrap creates four falling hazards. Requiring at least four prevents us from
            // declaring success before those runtime objects are present.
            return found >= 4;
        }

        private static bool SnapTop(Transform target, float targetTop)
        {
            if (!TryBounds(target, out Bounds b)) return false;
            target.position += Vector3.up * (targetTop - b.max.y);
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
