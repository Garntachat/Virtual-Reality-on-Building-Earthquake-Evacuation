using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Runtime-only adapter that makes Play mode start from the furniture already saved in House.unity.
    ///
    /// IMPORTANT:
    /// - Never creates replacement furniture.
    /// - Never writes/saves the scene.
    /// - Never moves the saved furniture while binding Play mode.
    /// - Invisible gameplay proxies are moved to the saved furniture, not the other way around.
    /// </summary>
    public static class HouseSavedSceneRuntimeBinder
    {
        private sealed class TransformSnapshot
        {
            public Transform transform;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        public static Transform FindSavedFurnitureRoot(Scene scene)
        {
            if (!scene.IsValid()) return null;

            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "HouseFurniture")
                    return root.transform;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                HouseBakedLayoutMarker marker = root.GetComponent<HouseBakedLayoutMarker>();
                if (marker != null) return root.transform;
            }

            // Recovery compatibility: if an older saved scene still has the former preview root,
            // use it as the source rather than generating or deleting anything.
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "CEVR_HouseEditorFurniturePreview")
                    return root.transform;

            // Last safe discovery path: find a root that already contains several saved HouseLayout
            // objects. We never construct a visual layout from code here.
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                int count = 0;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("HouseLayout_", StringComparison.Ordinal)) continue;
                    if (++count >= 4) return root.transform;
                }
            }

            return null;
        }

        public static bool Bind(
            Scene scene,
            Transform savedRoot,
            GroundMotionPlayer motion,
            SessionLogger logger,
            out string report)
        {
            if (savedRoot == null)
            {
                report = "No saved House furniture root was found.";
                return false;
            }

            List<TransformSnapshot> snapshots = CaptureTransforms(savedRoot);
            string signatureBefore = ComputeSnapshotSignature(snapshots);

            // Runtime-generated shapes are retained only as invisible gameplay/physics proxies.
            HideGeneratedPlaceholderVisuals(scene);

            // The proxies move to the furniture that the user positioned in the Scene editor.
            SyncGameplayProxiesFromSavedFurniture(scene, savedRoot);

            AddStaticCollidersWithoutMovingFurniture(savedRoot);

            BindFollower(savedRoot, "HouseLayout_DiningChair_CoverObstacle", scene, "HouseChair_CoverObstacle");
            BindFollower(savedRoot, "HouseLayout_DiningChair_Spare", scene, "HouseChair_Spare");
            BindFollower(savedRoot, "HouseLayout_DiningChair_Left", scene, "HouseChair_DiningLeft");
            BindFollower(savedRoot, "HouseLayout_DiningChair_Right", scene, "HouseChair_DiningRight");

            // Older saved editor layouts used four identical EDITOR_DiningChair names. Pair them to
            // gameplay proxies by nearest starting position so those manually edited scenes still work.
            BindLegacyEditorChairsIfNeeded(savedRoot, scene);

            BindFollower(savedRoot, "HouseLayout_Fridge", scene, "HouseTallCabinet_Left");
            BindFollower(savedRoot, "HouseLayout_Wandrobe", scene, "HouseBookcase_Right");

            for (int i = 1; i <= HouseSceneLayout.OverheadHazards.Length; i++)
                BindFollower(savedRoot, "HouseLayout_HazardBooks_" + i, scene, "HouseFallingObject_" + i);

            ConfigureSavedWindow(savedRoot, motion, logger);

            // Defensive guarantee: setup is not allowed to shift any furniture that existed when
            // Play began. Restore exact serialized transforms if another setup component touched them.
            int restored = RestoreChangedTransforms(snapshots);
            string signatureAfter = ComputeSnapshotSignature(snapshots);

            if (restored > 0)
                Debug.LogWarning(
                    $"CEVR House sync prevented {restored} saved furniture transform change(s) during Play setup.");

            bool exact = signatureBefore == signatureAfter;
            report =
                $"saved-root={savedRoot.name}; visual-signature={signatureAfter}; " +
                $"startup-transform-parity={(exact ? "PASS" : "FAILED")}";

            return true;
        }

        private static void HideGeneratedPlaceholderVisuals(Scene scene)
        {
            foreach (GameObject go in FindSceneObjects(scene))
            {
                string n = go.name;

                if (n == "HouseSturdyTableTop" ||
                    n == "HouseSturdyTableLeg" ||
                    n.StartsWith("HouseChair_", StringComparison.Ordinal) ||
                    n == "HouseTallCabinet_Left" ||
                    n == "HouseBookcase_Right" ||
                    n.StartsWith("HouseFallingObject_", StringComparison.Ordinal) ||
                    n.StartsWith("HouseSofa", StringComparison.Ordinal) ||
                    n.StartsWith("HouseCoffeeTable", StringComparison.Ordinal) ||
                    n.StartsWith("HouseRug", StringComparison.Ordinal) ||
                    n.StartsWith("HousePlant", StringComparison.Ordinal) ||
                    n.StartsWith("HouseShelfBook", StringComparison.Ordinal) ||
                    n.StartsWith("HousePhoto", StringComparison.Ordinal))
                {
                    SetRenderers(go, false);
                }

                // The saved House window is the visual window. The bootstrap window is only a
                // duplicate runtime visual and must stay hidden.
                if (n.StartsWith("HouseWindow_", StringComparison.Ordinal))
                    go.SetActive(false);
            }
        }

        private static void SyncGameplayProxiesFromSavedFurniture(Scene scene, Transform root)
        {
            SyncDiningTableProxy(scene, FindFurniture(root, "HouseLayout_DiningTable", "EDITOR_DiningTable"));

            SyncBottomPivotProxy(scene, root,
                new[] { "HouseLayout_DiningChair_CoverObstacle" }, "HouseChair_CoverObstacle");
            SyncBottomPivotProxy(scene, root,
                new[] { "HouseLayout_DiningChair_Spare" }, "HouseChair_Spare");
            SyncBottomPivotProxy(scene, root,
                new[] { "HouseLayout_DiningChair_Left" }, "HouseChair_DiningLeft");
            SyncBottomPivotProxy(scene, root,
                new[] { "HouseLayout_DiningChair_Right" }, "HouseChair_DiningRight");

            SyncBoundsCenterProxy(scene, FindFurniture(root, "HouseLayout_Fridge", "EDITOR_Fridge"),
                "HouseTallCabinet_Left");
            SyncBoundsCenterProxy(scene, FindFurniture(root, "HouseLayout_Wandrobe", "EDITOR_Wandrobe"),
                "HouseBookcase_Right");

            for (int i = 1; i <= HouseSceneLayout.OverheadHazards.Length; i++)
            {
                Transform visual = FindFurniture(root, "HouseLayout_HazardBooks_" + i);
                SyncBoundsCenterProxy(scene, visual, "HouseFallingObject_" + i);
            }
        }

        private static void SyncDiningTableProxy(Scene scene, Transform visual)
        {
            if (visual == null || !TryLocalVisibleBounds(visual, out Bounds local)) return;

            GameObject top = FindSceneObject(scene, "HouseSturdyTableTop");
            if (top != null)
            {
                Vector3 localTopCenter = new Vector3(local.center.x, local.max.y - 0.07f, local.center.z);
                top.transform.SetPositionAndRotation(
                    visual.TransformPoint(localTopCenter),
                    visual.rotation);
                top.transform.localScale = new Vector3(
                    Mathf.Max(0.20f, local.size.x),
                    0.14f,
                    Mathf.Max(0.20f, local.size.z));

                BoxCollider box = top.GetComponent<BoxCollider>();
                if (box != null) box.size = Vector3.one;
            }

            var legs = new List<GameObject>();
            foreach (GameObject go in FindSceneObjects(scene))
                if (go.name == "HouseSturdyTableLeg") legs.Add(go);

            float lx = Mathf.Max(0.08f, local.extents.x - 0.10f);
            float lz = Mathf.Max(0.08f, local.extents.z - 0.10f);
            float legHeight = Mathf.Max(0.25f, local.size.y - 0.12f);
            Vector3[] offsets =
            {
                new Vector3(local.center.x - lx, local.min.y + legHeight * 0.5f, local.center.z - lz),
                new Vector3(local.center.x + lx, local.min.y + legHeight * 0.5f, local.center.z - lz),
                new Vector3(local.center.x - lx, local.min.y + legHeight * 0.5f, local.center.z + lz),
                new Vector3(local.center.x + lx, local.min.y + legHeight * 0.5f, local.center.z + lz)
            };

            for (int i = 0; i < legs.Count && i < offsets.Length; i++)
            {
                legs[i].transform.SetPositionAndRotation(visual.TransformPoint(offsets[i]), visual.rotation);
                legs[i].transform.localScale = new Vector3(0.10f, legHeight, 0.10f);
                BoxCollider box = legs[i].GetComponent<BoxCollider>();
                if (box != null) box.size = Vector3.one;
            }

            Vector3 floorCenter = visual.TransformPoint(new Vector3(local.center.x, local.min.y, local.center.z));
            GameObject cover = FindSceneObject(scene, "HouseCoverZone");
            if (cover != null)
            {
                cover.transform.SetPositionAndRotation(
                    floorCenter + Vector3.up * 0.40f,
                    Quaternion.Euler(0f, visual.eulerAngles.y, 0f));
                BoxCollider trigger = cover.GetComponent<BoxCollider>();
                if (trigger != null)
                    trigger.size = new Vector3(
                        Mathf.Max(0.5f, local.size.x * 0.90f),
                        0.80f,
                        Mathf.Max(0.5f, local.size.z * 0.90f));
            }

            GameObject crawl = FindSceneObject(scene, "HouseCrawlHereMarker");
            if (crawl != null)
            {
                crawl.transform.SetPositionAndRotation(
                    floorCenter + Vector3.up * 0.012f,
                    Quaternion.Euler(0f, visual.eulerAngles.y, 0f));
                crawl.transform.localScale = new Vector3(
                    Mathf.Max(0.5f, local.size.x * 0.95f),
                    0.018f,
                    Mathf.Max(0.5f, local.size.z * 0.95f));
            }
        }

        private static void SyncBottomPivotProxy(
            Scene scene,
            Transform root,
            string[] visualNames,
            string proxyName)
        {
            Transform visual = FindFurniture(root, visualNames);
            GameObject proxy = FindSceneObject(scene, proxyName);
            if (visual == null || proxy == null) return;

            if (TryLocalVisibleBounds(visual, out Bounds local))
            {
                Vector3 bottom = visual.TransformPoint(
                    new Vector3(local.center.x, local.min.y, local.center.z));
                proxy.transform.SetPositionAndRotation(bottom, visual.rotation);
            }
            else
            {
                proxy.transform.SetPositionAndRotation(visual.position, visual.rotation);
            }
        }

        private static void SyncBoundsCenterProxy(
            Scene scene,
            Transform visual,
            string proxyName)
        {
            GameObject proxy = FindSceneObject(scene, proxyName);
            if (visual == null || proxy == null || !TryLocalVisibleBounds(visual, out Bounds local)) return;

            proxy.transform.SetPositionAndRotation(visual.TransformPoint(local.center), visual.rotation);
            proxy.transform.localScale = new Vector3(
                Mathf.Max(0.05f, local.size.x),
                Mathf.Max(0.05f, local.size.y),
                Mathf.Max(0.05f, local.size.z));

            BoxCollider box = proxy.GetComponent<BoxCollider>();
            if (box != null) box.size = Vector3.one;
        }

        private static void AddStaticCollidersWithoutMovingFurniture(Transform root)
        {
            string[] names =
            {
                "HouseLayout_Sofa",
                "HouseLayout_tableCoffee",
                "HouseLayout_cabinetTelevision",
                "HouseLayout_kitchenCabinet",
                "HouseLayout_kitchenSink",
                "HouseLayout_kitchenStove",
                "HouseLayout_Bed",
                "HouseLayout_bookcaseOpen"
            };

            foreach (string name in names)
            {
                Transform visual = FindFurniture(root, name);
                if (visual == null || visual.GetComponent<Collider>() != null) continue;
                if (!TryLocalVisibleBounds(visual, out Bounds local)) continue;

                BoxCollider box = visual.gameObject.AddComponent<BoxCollider>();
                box.center = local.center;
                box.size = local.size;
            }
        }

        private static void BindLegacyEditorChairsIfNeeded(Transform root, Scene scene)
        {
            if (FindFurniture(root, "HouseLayout_DiningChair_CoverObstacle") != null ||
                FindFurniture(root, "HouseLayout_DiningChair_Spare") != null ||
                FindFurniture(root, "HouseLayout_DiningChair_Left") != null ||
                FindFurniture(root, "HouseLayout_DiningChair_Right") != null)
                return;

            var chairs = new List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "EDITOR_DiningChair")
                    chairs.Add(t);

            if (chairs.Count == 0) return;

            string[] proxies =
            {
                "HouseChair_CoverObstacle",
                "HouseChair_Spare",
                "HouseChair_DiningLeft",
                "HouseChair_DiningRight"
            };

            var unused = new List<Transform>(chairs);
            foreach (string proxyName in proxies)
            {
                GameObject proxy = FindSceneObject(scene, proxyName);
                if (proxy == null || unused.Count == 0) continue;

                Transform nearest = null;
                float best = float.PositiveInfinity;
                foreach (Transform candidate in unused)
                {
                    float distance = (candidate.position - proxy.transform.position).sqrMagnitude;
                    if (distance >= best) continue;
                    best = distance;
                    nearest = candidate;
                }

                if (nearest == null) continue;
                unused.Remove(nearest);

                if (TryLocalVisibleBounds(nearest, out Bounds local))
                {
                    Vector3 bottom = nearest.TransformPoint(
                        new Vector3(local.center.x, local.min.y, local.center.z));
                    proxy.transform.SetPositionAndRotation(bottom, nearest.rotation);
                }
                else
                {
                    proxy.transform.SetPositionAndRotation(nearest.position, nearest.rotation);
                }

                BindFollower(nearest, proxy);
            }
        }

        private static void BindFollower(
            Transform root,
            string visualName,
            Scene scene,
            string proxyName)
        {
            Transform visual = FindFurniture(root, visualName);
            GameObject proxy = FindSceneObject(scene, proxyName);
            if (visual == null || proxy == null) return;
            BindFollower(visual, proxy);
        }

        private static void BindFollower(Transform visual, GameObject proxy)
        {
            if (visual == null || proxy == null) return;

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            HouseVisualRuntimeFollower follower = visual.GetComponent<HouseVisualRuntimeFollower>();
            if (follower == null)
                follower = visual.gameObject.AddComponent<HouseVisualRuntimeFollower>();
            follower.Configure(proxy.transform);
        }

        private static void ConfigureSavedWindow(
            Transform root,
            GroundMotionPlayer motion,
            SessionLogger logger)
        {
            Transform glass = FindFurniture(root, "HouseLayout_WindowGlass", "EDITOR_WindowGlass");
            if (glass == null) return;

            // Preserve exactly what the Scene editor shows. Do not run WindowView at Play because
            // it can replace the material or create extra exterior geometry.
            WindowView view = glass.GetComponent<WindowView>();
            if (view != null) view.enabled = false;

            BreakableWindow breakable = glass.GetComponent<BreakableWindow>();
            if (breakable == null) breakable = glass.gameObject.AddComponent<BreakableWindow>();
            breakable.Configure("house-window-saved", motion, logger);
        }

        private static Transform FindFurniture(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (string name in names)
                    if (t.name == name) return t;
            }
            return null;
        }

        private static bool TryLocalVisibleBounds(Transform root, out Bounds localBounds)
        {
            localBounds = default;
            bool found = false;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || HouseFurnitureGeometry.IsCollisionVisual(renderer.transform, root))
                    continue;

                Bounds world = renderer.bounds;
                Vector3 min = world.min;
                Vector3 max = world.max;
                Vector3[] corners =
                {
                    new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z),
                    new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z),
                    new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z),
                    new Vector3(min.x, max.y, max.z), new Vector3(max.x, max.y, max.z)
                };

                foreach (Vector3 corner in corners)
                {
                    Vector3 local = root.InverseTransformPoint(corner);
                    if (!found)
                    {
                        localBounds = new Bounds(local, Vector3.zero);
                        found = true;
                    }
                    else localBounds.Encapsulate(local);
                }
            }

            return found;
        }

        private static List<TransformSnapshot> CaptureTransforms(Transform root)
        {
            var snapshots = new List<TransformSnapshot>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                snapshots.Add(new TransformSnapshot
                {
                    transform = t,
                    localPosition = t.localPosition,
                    localRotation = t.localRotation,
                    localScale = t.localScale
                });
            }
            return snapshots;
        }

        private static int RestoreChangedTransforms(List<TransformSnapshot> snapshots)
        {
            int restored = 0;
            foreach (TransformSnapshot snapshot in snapshots)
            {
                Transform t = snapshot.transform;
                if (t == null) continue;

                bool changed =
                    (t.localPosition - snapshot.localPosition).sqrMagnitude > 0.00000001f ||
                    Quaternion.Angle(t.localRotation, snapshot.localRotation) > 0.001f ||
                    (t.localScale - snapshot.localScale).sqrMagnitude > 0.00000001f;

                if (!changed) continue;
                t.localPosition = snapshot.localPosition;
                t.localRotation = snapshot.localRotation;
                t.localScale = snapshot.localScale;
                restored++;
            }
            return restored;
        }

        private static string ComputeSnapshotSignature(List<TransformSnapshot> snapshots)
        {
            var entries = new List<string>();
            foreach (TransformSnapshot snapshot in snapshots)
            {
                Transform t = snapshot.transform;
                if (t == null) continue;

                Vector3 p = t.localPosition;
                Vector3 s = t.localScale;
                Quaternion r = t.localRotation;
                entries.Add(
                    $"{t.name}|" +
                    $"{p.x:F5},{p.y:F5},{p.z:F5}|" +
                    $"{r.x:F6},{r.y:F6},{r.z:F6},{r.w:F6}|" +
                    $"{s.x:F5},{s.y:F5},{s.z:F5}");
            }
            entries.Sort(StringComparer.Ordinal);

            unchecked
            {
                uint hash = 2166136261u;
                foreach (string entry in entries)
                {
                    foreach (char ch in entry)
                    {
                        hash ^= ch;
                        hash *= 16777619u;
                    }
                }
                return $"{entries.Count}:{hash:X8}";
            }
        }

        private static void SetRenderers(GameObject go, bool enabled)
        {
            if (go == null) return;
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = enabled;
        }

        private static GameObject FindSceneObject(Scene scene, string exactName)
        {
            foreach (GameObject go in FindSceneObjects(scene))
                if (go.name == exactName) return go;
            return null;
        }

        private static List<GameObject> FindSceneObjects(Scene scene)
        {
            var result = new List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                result.Add(root);
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.gameObject != root) result.Add(t.gameObject);
            }
            return result;
        }
    }
}
