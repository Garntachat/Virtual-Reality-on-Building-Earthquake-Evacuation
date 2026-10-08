using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Play-only bridge from the House scene the user saved in the Unity editor to the runtime
    /// gameplay systems.
    ///
    /// Safety rules:
    /// 1) Saved House furniture is NEVER rebuilt, deleted, hidden, or repositioned during setup.
    /// 2) Only objects created under CEVR_UniversalGameplay are hidden/moved as runtime proxies.
    /// 3) No scene saving or editor APIs are used here.
    /// 4) A transform snapshot is restored after setup, so Play starts from the exact saved layout.
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

            // Preferred current hierarchy.
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "HouseFurniture")
                    return root.transform;

            // Saved layouts produced by the old baker carry this marker.
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponent<HouseBakedLayoutMarker>() != null)
                    return root.transform;

            // Compatibility with the older editor-visible hierarchy.
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "CEVR_HouseEditorFurniturePreview")
                    return root.transform;

            // Do not guess from runtime objects. Search only scene roots that already contain several
            // furniture objects that existed before Play.
            string[] furnitureTokens =
            {
                "HouseLayout_DiningTable", "EDITOR_DiningTable",
                "HouseLayout_Sofa", "EDITOR_Sofa",
                "HouseLayout_Bed", "EDITOR_Bed",
                "HouseLayout_Fridge", "EDITOR_Fridge",
                "HouseLayout_Wandrobe", "EDITOR_Wandrobe"
            };

            Transform best = null;
            int bestScore = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "CEVR_UniversalGameplay" ||
                    root.name == "CEVR_HouseSourceLayoutOverride")
                    continue;

                int score = 0;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    foreach (string token in furnitureTokens)
                    {
                        if (t.name != token) continue;
                        score++;
                        break;
                    }
                }

                if (score <= bestScore) continue;
                bestScore = score;
                best = root.transform;
            }

            return bestScore >= 2 ? best : null;
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
                report = "No saved House furniture hierarchy was found.";
                return false;
            }

            Transform gameplayRoot = FindRuntimeGameplayRoot(scene);
            if (gameplayRoot == null)
            {
                report = "CEVR_UniversalGameplay runtime root was not found.";
                return false;
            }

            List<TransformSnapshot> snapshots = CaptureTransforms(savedRoot);
            string signatureBefore = ComputeSnapshotSignature(snapshots);

            // IMPORTANT: every object changed below is either under CEVR_UniversalGameplay or is a
            // temporary Play-mode component on a saved visual. No editor scene object is rebuilt.
            HideRuntimePlaceholderVisuals(gameplayRoot);

            Transform diningTable = FindFurniture(savedRoot,
                "HouseLayout_DiningTable", "EDITOR_DiningTable");
            SyncDiningTableProxy(gameplayRoot, diningTable);

            BindNamedChair(savedRoot, gameplayRoot,
                "HouseLayout_DiningChair_CoverObstacle", "HouseChair_CoverObstacle");
            BindNamedChair(savedRoot, gameplayRoot,
                "HouseLayout_DiningChair_Spare", "HouseChair_Spare");
            BindNamedChair(savedRoot, gameplayRoot,
                "HouseLayout_DiningChair_Left", "HouseChair_DiningLeft");
            BindNamedChair(savedRoot, gameplayRoot,
                "HouseLayout_DiningChair_Right", "HouseChair_DiningRight");
            BindLegacyEditorChairsIfNeeded(savedRoot, gameplayRoot);

            BindCenterPhysicsVisual(savedRoot, gameplayRoot,
                new[] { "HouseLayout_Fridge", "EDITOR_Fridge" },
                "HouseTallCabinet_Left");
            BindCenterPhysicsVisual(savedRoot, gameplayRoot,
                new[] { "HouseLayout_Wandrobe", "EDITOR_Wandrobe" },
                "HouseBookcase_Right");

            for (int i = 1; i <= HouseSceneLayout.OverheadHazards.Length; i++)
            {
                BindCenterPhysicsVisual(savedRoot, gameplayRoot,
                    new[] { "HouseLayout_HazardBooks_" + i },
                    "HouseFallingObject_" + i);
            }

            BuildRuntimeStaticCollider(gameplayRoot, savedRoot,
                "HouseLayout_Sofa", "EDITOR_Sofa");
            BuildRuntimeStaticCollider(gameplayRoot, savedRoot,
                "HouseLayout_tableCoffee", "EDITOR_tableCoffee", "EDITOR_CoffeeTable");
            BuildRuntimeStaticCollider(gameplayRoot, savedRoot,
                "HouseLayout_cabinetTelevision", "EDITOR_cabinetTelevision", "EDITOR_TV");
            BuildRuntimeStaticCollider(gameplayRoot, savedRoot,
                "HouseLayout_kitchenCabinet", "EDITOR_kitchenCabinet");
            BuildRuntimeStaticCollider(gameplayRoot, savedRoot,
                "HouseLayout_kitchenSink", "EDITOR_kitchenSink");
            BuildRuntimeStaticCollider(gameplayRoot, savedRoot,
                "HouseLayout_kitchenStove", "EDITOR_kitchenStove");
            BuildRuntimeStaticCollider(gameplayRoot, savedRoot,
                "HouseLayout_Bed", "EDITOR_Bed");
            BuildRuntimeStaticCollider(gameplayRoot, savedRoot,
                "HouseLayout_bookcaseOpen", "EDITOR_bookcaseOpen");

            ConfigureSavedWindow(savedRoot, motion, logger);

            // No setup code is allowed to change the saved layout. Restore exact local transforms
            // before the first rendered Play frame, then verify parity.
            int restored = RestoreChangedTransforms(snapshots);
            string signatureAfter = ComputeSnapshotSignature(snapshots);
            bool exact = signatureBefore == signatureAfter;

            if (restored > 0)
                Debug.LogWarning(
                    $"CEVR House sync restored {restored} saved furniture transform change(s) during Play setup.");

            report =
                $"saved-root={savedRoot.name}; editor-signature={signatureBefore}; " +
                $"play-start-signature={signatureAfter}; parity={(exact ? "PASS" : "FAILED")}";

            return exact;
        }

        private static void HideRuntimePlaceholderVisuals(Transform gameplayRoot)
        {
            foreach (Transform t in gameplayRoot.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                string n = go.name;

                bool generatedVisual =
                    n == "HouseSturdyTableTop" ||
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
                    n.StartsWith("HousePhoto", StringComparison.Ordinal);

                if (generatedVisual)
                    SetRenderers(go, false);

                // The runtime bootstrap window is a duplicate visual. The saved editor window remains.
                if (n.StartsWith("HouseWindow_", StringComparison.Ordinal))
                    go.SetActive(false);
            }
        }

        private static void SyncDiningTableProxy(Transform gameplayRoot, Transform visual)
        {
            if (visual == null || !TryLocalVisibleBounds(visual, out Bounds local)) return;

            GameObject top = FindRuntimeObject(gameplayRoot, "HouseSturdyTableTop");
            if (top != null)
            {
                Vector3 localTopCenter =
                    new Vector3(local.center.x, local.max.y - 0.07f, local.center.z);
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
            foreach (Transform t in gameplayRoot.GetComponentsInChildren<Transform>(true))
                if (t.name == "HouseSturdyTableLeg")
                    legs.Add(t.gameObject);

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
                legs[i].transform.SetPositionAndRotation(
                    visual.TransformPoint(offsets[i]),
                    visual.rotation);
                legs[i].transform.localScale = new Vector3(0.10f, legHeight, 0.10f);

                BoxCollider box = legs[i].GetComponent<BoxCollider>();
                if (box != null) box.size = Vector3.one;
            }

            Vector3 floorCenter =
                visual.TransformPoint(new Vector3(local.center.x, local.min.y, local.center.z));

            GameObject cover = FindRuntimeObject(gameplayRoot, "HouseCoverZone");
            if (cover != null)
            {
                cover.transform.SetPositionAndRotation(
                    floorCenter + Vector3.up * 0.40f,
                    Quaternion.Euler(0f, visual.eulerAngles.y, 0f));

                BoxCollider trigger = cover.GetComponent<BoxCollider>();
                if (trigger != null)
                    trigger.size = new Vector3(
                        Mathf.Max(0.50f, local.size.x * 0.90f),
                        0.80f,
                        Mathf.Max(0.50f, local.size.z * 0.90f));
            }

            GameObject crawl = FindRuntimeObject(gameplayRoot, "HouseCrawlHereMarker");
            if (crawl != null)
            {
                crawl.transform.SetPositionAndRotation(
                    floorCenter + Vector3.up * 0.012f,
                    Quaternion.Euler(0f, visual.eulerAngles.y, 0f));
                crawl.transform.localScale = new Vector3(
                    Mathf.Max(0.50f, local.size.x * 0.95f),
                    0.018f,
                    Mathf.Max(0.50f, local.size.z * 0.95f));
            }
        }

        private static void BindNamedChair(
            Transform savedRoot,
            Transform gameplayRoot,
            string visualName,
            string proxyName)
        {
            Transform visual = FindFurniture(savedRoot, visualName);
            GameObject proxy = FindRuntimeObject(gameplayRoot, proxyName);
            if (visual == null || proxy == null) return;

            AlignBottomProxyToVisual(proxy, visual);
            AttachRuntimeFollower(visual, proxy);
        }

        private static void BindLegacyEditorChairsIfNeeded(
            Transform savedRoot,
            Transform gameplayRoot)
        {
            bool hasNamed =
                FindFurniture(savedRoot, "HouseLayout_DiningChair_CoverObstacle") != null ||
                FindFurniture(savedRoot, "HouseLayout_DiningChair_Spare") != null ||
                FindFurniture(savedRoot, "HouseLayout_DiningChair_Left") != null ||
                FindFurniture(savedRoot, "HouseLayout_DiningChair_Right") != null;

            if (hasNamed) return;

            var chairs = new List<Transform>();
            foreach (Transform t in savedRoot.GetComponentsInChildren<Transform>(true))
                if (t.name == "EDITOR_DiningChair")
                    chairs.Add(t);

            string[] proxyNames =
            {
                "HouseChair_CoverObstacle",
                "HouseChair_Spare",
                "HouseChair_DiningLeft",
                "HouseChair_DiningRight"
            };

            var unused = new List<Transform>(chairs);
            foreach (string proxyName in proxyNames)
            {
                GameObject proxy = FindRuntimeObject(gameplayRoot, proxyName);
                if (proxy == null || unused.Count == 0) continue;

                Transform nearest = null;
                float bestDistance = float.PositiveInfinity;
                foreach (Transform candidate in unused)
                {
                    float d = (candidate.position - proxy.transform.position).sqrMagnitude;
                    if (d >= bestDistance) continue;
                    bestDistance = d;
                    nearest = candidate;
                }

                if (nearest == null) continue;
                unused.Remove(nearest);

                AlignBottomProxyToVisual(proxy, nearest);
                AttachRuntimeFollower(nearest, proxy);
            }
        }

        private static void AlignBottomProxyToVisual(GameObject proxy, Transform visual)
        {
            if (proxy == null || visual == null) return;

            if (TryLocalVisibleBounds(visual, out Bounds local))
            {
                Vector3 bottom =
                    visual.TransformPoint(new Vector3(local.center.x, local.min.y, local.center.z));
                proxy.transform.SetPositionAndRotation(bottom, visual.rotation);
            }
            else
            {
                proxy.transform.SetPositionAndRotation(visual.position, visual.rotation);
            }
        }

        private static void BindCenterPhysicsVisual(
            Transform savedRoot,
            Transform gameplayRoot,
            string[] visualNames,
            string proxyName)
        {
            Transform visual = FindFurniture(savedRoot, visualNames);
            GameObject proxy = FindRuntimeObject(gameplayRoot, proxyName);
            if (visual == null || proxy == null) return;

            if (HouseFurnitureGeometry.TryVisibleBounds(visual, out Bounds world))
            {
                proxy.transform.SetPositionAndRotation(world.center, visual.rotation);
                proxy.transform.localScale = new Vector3(
                    Mathf.Max(0.05f, world.size.x),
                    Mathf.Max(0.05f, world.size.y),
                    Mathf.Max(0.05f, world.size.z));

                BoxCollider box = proxy.GetComponent<BoxCollider>();
                if (box != null) box.size = Vector3.one;
            }
            else
            {
                proxy.transform.SetPositionAndRotation(visual.position, visual.rotation);
            }

            AttachRuntimeFollower(visual, proxy);
        }

        private static void AttachRuntimeFollower(Transform visual, GameObject proxy)
        {
            if (visual == null || proxy == null) return;

            // Physics belongs to the invisible runtime proxy. Disable any saved furniture collider
            // only during Play so duplicate collision bodies do not fight each other.
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            HouseVisualRuntimeFollower follower =
                visual.GetComponent<HouseVisualRuntimeFollower>();
            if (follower == null)
                follower = visual.gameObject.AddComponent<HouseVisualRuntimeFollower>();
            follower.Configure(proxy.transform);
        }

        private static void BuildRuntimeStaticCollider(
            Transform gameplayRoot,
            Transform savedRoot,
            params string[] visualNames)
        {
            Transform visual = FindFurniture(savedRoot, visualNames);
            if (visual == null ||
                !HouseFurnitureGeometry.TryVisibleBounds(visual, out Bounds world))
                return;

            GameObject proxy = new GameObject(
                "CEVR_SavedCollider_" + visual.name);
            proxy.transform.SetParent(gameplayRoot, true);
            proxy.transform.position = world.center;
            proxy.transform.rotation = Quaternion.identity;

            BoxCollider box = proxy.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = world.size;
        }

        private static void ConfigureSavedWindow(
            Transform savedRoot,
            GroundMotionPlayer motion,
            SessionLogger logger)
        {
            Transform glass = FindFurniture(
                savedRoot,
                "HouseLayout_WindowGlass",
                "EDITOR_WindowGlass");

            if (glass == null) return;

            // Keep the exact Scene-view material and exterior geometry. WindowView is intentionally
            // not configured here because it would change how the saved window looks in Play.
            WindowView view = glass.GetComponent<WindowView>();
            if (view != null) view.enabled = false;

            BreakableWindow breakable = glass.GetComponent<BreakableWindow>();
            if (breakable == null)
                breakable = glass.gameObject.AddComponent<BreakableWindow>();
            breakable.Configure("house-window-saved", motion, logger);
        }

        private static Transform FindRuntimeGameplayRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "CEVR_UniversalGameplay")
                    return root.transform;
            return null;
        }

        private static GameObject FindRuntimeObject(
            Transform gameplayRoot,
            string exactName)
        {
            if (gameplayRoot == null) return null;
            foreach (Transform t in gameplayRoot.GetComponentsInChildren<Transform>(true))
                if (t.name == exactName)
                    return t.gameObject;
            return null;
        }

        private static Transform FindFurniture(
            Transform root,
            params string[] names)
        {
            if (root == null) return null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (string name in names)
                    if (t.name == name)
                        return t;
            }
            return null;
        }

        private static bool TryLocalVisibleBounds(
            Transform root,
            out Bounds localBounds)
        {
            localBounds = default;
            bool found = false;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled ||
                    HouseFurnitureGeometry.IsCollisionVisual(renderer.transform, root))
                    continue;

                Bounds world = renderer.bounds;
                Vector3 min = world.min;
                Vector3 max = world.max;
                Vector3[] corners =
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, max.y, max.z)
                };

                foreach (Vector3 corner in corners)
                {
                    Vector3 local = root.InverseTransformPoint(corner);
                    if (!found)
                    {
                        localBounds = new Bounds(local, Vector3.zero);
                        found = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(local);
                    }
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

        private static int RestoreChangedTransforms(
            List<TransformSnapshot> snapshots)
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

        private static string ComputeSnapshotSignature(
            List<TransformSnapshot> snapshots)
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
    }
}
