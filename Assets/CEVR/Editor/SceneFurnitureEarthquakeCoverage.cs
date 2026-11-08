using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>
    /// Non-destructive, one-time physics/visual inspector setup for saved Tutorial
    /// and Condo furniture. Does not move, scale, regenerate or reparent anything.
    /// </summary>
    public static class SceneFurnitureEarthquakeCoverage
    {
        private const string Tutorial = "CEVR_ChulaEngineering_Tutorial";
        private const string Condo = "Condo";

        public static bool Supports(Scene scene) =>
            scene.IsValid() && scene.isLoaded &&
            (scene.name == Tutorial || scene.name == Condo);

        public static bool HasPreparedFurniture(Scene scene)
        {
            if (!Supports(scene)) return false;
            if (scene.name == Condo)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    CondoGameplayLayout layout = root.GetComponentInChildren<CondoGameplayLayout>(true);
                    if (layout != null && layout.IsPrepared) return true;
                }
                return false;
            }
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                FurnitureSceneDressing dressing = root.GetComponentInChildren<FurnitureSceneDressing>(true);
                if (dressing != null && dressing.SavedInEditor) return true;
            }
            return false;
        }

        [MenuItem("CEVR/Earthquake/Check Tutorial or Condo furniture controls")]
        private static void CheckCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!Supports(scene))
            {
                EditorUtility.DisplayDialog("Open Tutorial or Condo",
                    "Open the editable Tutorial or Condo scene in Edit mode.", "OK");
                return;
            }
            if (!HasPreparedFurniture(scene))
            {
                EditorUtility.DisplayDialog("Finish scene preparation first",
                    "The furnished scene has not been baked and saved yet. Open the scene " +
                    "in Edit mode, or use CEVR > Tutorial/Condo > Prepare editable gameplay.", "OK");
                return;
            }
            List<Transform> candidates = Candidates(scene);
            int physical = 0, visual = 0, missing = 0;
            foreach (Transform item in candidates)
            {
                Rigidbody body = item.GetComponent<Rigidbody>();
                if (body != null)
                {
                    if (item.GetComponent<InertialRigidbody>() != null) physical++;
                    else
                    {
                        missing++;
                        Debug.LogWarning("CEVR: missing physical quake controls: " + item.name, item);
                    }
                }
                else if (item.GetComponent<DecorativeQuakeResponse>() != null ||
                         item.GetComponent<VisualQuakeSway>() != null)
                    visual++;
                else
                {
                    missing++;
                    Debug.LogWarning("CEVR: missing furniture visual response: " + item.name, item);
                }
            }

            EditorUtility.DisplayDialog("Tutorial / Condo furniture settings",
                "Editable physical furniture: " + physical +
                "\nVisual-only furniture: " + visual +
                "\nMissing controls: " + missing +
                "\n\nPhysical objects use actual Unity Rigidbody Mass and Damping. " +
                "Static furniture uses Visual Shake Strength; it cannot meaningfully have physics Mass." +
                (missing == 0 ? "\n\nCoverage looks complete." :
                    "\n\nRun CEVR > Earthquake > Add simple physics settings to scene."),
                "OK");
        }

        public static bool NeedsSetup(Scene scene)
        {
            if (!Supports(scene) || !HasPreparedFurniture(scene)) return false;
            foreach (Transform item in Candidates(scene))
            {
                if (RequiresEditableTutorialPhysics(scene, item))
                {
                    if (item.GetComponent<Rigidbody>() == null ||
                        item.GetComponent<InertialRigidbody>() == null ||
                        item.GetComponent<DecorativeQuakeResponse>() != null ||
                        item.GetComponent<Collider>() == null)
                        return true;
                }
                else if (item.GetComponent<Rigidbody>() != null)
                {
                    if (item.GetComponent<InertialRigidbody>() == null) return true;
                }
                else if (item.GetComponent<DecorativeQuakeResponse>() == null &&
                         item.GetComponent<VisualQuakeSway>() == null)
                    return true;
            }
            return false;
        }

        // Returns number of new settings components. Existing artist-edited
        // strengths, forces, masses, colliders and dampings are NEVER touched.
        // Saved tutorial model roots get native Inspector physics. The frame,
        // walls, windows, markers, lamps/fixture anchors and player are excluded.
        private static bool RequiresEditableTutorialPhysics(Scene scene, Transform item)
        {
            if (scene.name != Tutorial || item == null) return false;
            return item.name.StartsWith("Kenney_", StringComparison.Ordinal) ||
                   item.name.StartsWith("TeamFurniture_", StringComparison.Ordinal);
        }

        private static bool EnsureRootCollider(Transform item)
        {
            if (item.GetComponent<Collider>() != null) return false;
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
            bool haveBounds = false;
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                Bounds bounds = renderer.bounds;
                // Convert all world AABB corners into local space to respect
                // the authored object's original transform and rotation.
                foreach (float x in new[] {bounds.min.x, bounds.max.x})
                foreach (float y in new[] {bounds.min.y, bounds.max.y})
                foreach (float z in new[] {bounds.min.z, bounds.max.z})
                {
                    Vector3 corner = item.InverseTransformPoint(new Vector3(x, y, z));
                    min = Vector3.Min(min, corner);
                    max = Vector3.Max(max, corner);
                    haveBounds = true;
                }
            }
            if (!haveBounds) return false;
            BoxCollider collider = Undo.AddComponent<BoxCollider>(item.gameObject);
            collider.center = (min + max) * 0.5f;
            Vector3 size = max - min;
            collider.size = new Vector3(Mathf.Max(0.08f, size.x),
                Mathf.Max(0.08f, size.y), Mathf.Max(0.08f, size.z));
            return true;
        }

        // Only edit real saved furniture roots, never transform hierarchy/meshes.
        public static int Apply(Scene scene)
        {
            if (!Supports(scene) || !HasPreparedFurniture(scene)) return 0;
            int added = 0;
            foreach (Transform item in Candidates(scene))
            {
                if (item == null) continue;
                bool physicalTutorial = RequiresEditableTutorialPhysics(scene, item);
                Rigidbody body = item.GetComponent<Rigidbody>();
                if (physicalTutorial)
                {
                    if (body == null)
                    {
                        EarthquakeStarterPresets.Values settings = EarthquakeStarterPresets.Suggest(item.name);
                        body = Undo.AddComponent<Rigidbody>(item.gameObject);
                        body.mass = settings.Mass;
                        body.linearDamping = settings.LinearDamping;
                        body.angularDamping = settings.AngularDamping;
                        body.useGravity = false;
                        body.isKinematic = true;
                        added++;
                    }
                    if (item.GetComponent<Collider>() == null && EnsureRootCollider(item)) added++;
                    InertialRigidbody inertial = item.GetComponent<InertialRigidbody>();
                    if (inertial == null)
                    {
                        var settings = EarthquakeStarterPresets.Suggest(item.name);
                        inertial = Undo.AddComponent<InertialRigidbody>(item.gameObject);
                        inertial.Configure(null, 0.65f, false);
                        inertial.ConfigureForces(settings.ForceNewtons, settings.TorqueNewtonMeters);
                        inertial.ConfigureQuakeActivation(true);
                        added++;
                    }
                    DecorativeQuakeResponse decorator = item.GetComponent<DecorativeQuakeResponse>();
                    if (decorator != null)
                    {
                        Undo.DestroyObjectImmediate(decorator);
                        added++;
                    }
                    continue;
                }
                if (body != null)
                {
                    if (item.GetComponent<InertialRigidbody>() != null) continue;
                    bool vertical = body.mass < 12f &&
                        (body.constraints & RigidbodyConstraints.FreezePositionY) == 0;
                    Undo.AddComponent<InertialRigidbody>(item.gameObject)
                        .Configure(null, 1f, vertical);
                    added++;
                    continue;
                }
                if (item.GetComponent<DecorativeQuakeResponse>() != null ||
                    item.GetComponent<VisualQuakeSway>() != null) continue;
                string name = item.name;
                DecorativeQuakeMode mode = ModeFor(name);
                float initialStrength = FixedCoverOrFurniture(name) ? 0f :
                    mode == DecorativeQuakeMode.Hanging ? 0.45f : 0.65f;
                Undo.AddComponent<DecorativeQuakeResponse>(item.gameObject)
                    .Configure(null, mode, initialStrength);
                added++;
            }
            return added;
        }

        private static List<Transform> Candidates(Scene scene)
        {
            var all = new List<Transform>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Transform t in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t == null || !IsFurniture(t, scene.name)) continue;
                    all.Add(t);
                }
            }
            // Parents first, so a single furniture root owns the response, not
            // a separate competing shaker on every mesh, seat, leg or surface.
            all.Sort((a, b) => Depth(a).CompareTo(Depth(b)));
            var result = new List<Transform>();
            foreach (Transform candidate in all)
            {
                if (HasPhysicsOrVisualAncestor(candidate)) continue;
                if (result.Exists(parent => candidate.IsChildOf(parent))) continue;
                result.Add(candidate);
            }
            return result;
        }

        private static bool IsFurniture(Transform t, string sceneName)
        {
            if (t.parent == null || IsExcluded(t)) return false;
            if (t.GetComponentInParent<PlayerHealth>() != null ||
                t.GetComponentInParent<Camera>() != null ||
                t.GetComponentInParent<Canvas>() != null) return false;
            if (t.GetComponent<HouseProxyEarthquakeSettings>() != null) return false;

            bool hasVisual = HasEnabledRenderer(t);
            if (t.GetComponent<Rigidbody>() != null && (hasVisual ||
                t.GetComponent<FallingHazard>() != null ||
                t.GetComponent<MovableFurniture>() != null ||
                t.GetComponent<ProtectivePillow>() != null)) return true;
            if (t.GetComponent<WearableShoes>() != null) return true;
            if (!hasVisual) return false;

            string name = t.name;
            if (sceneName == Condo)
            {
                // Condo's imported pb_Mesh/Cube/Stairs assets are the building,
                // not loose furniture. Never attach quake physics to the shell.
                if (ParentNamed(t, "Condo Gameplay"))
                    return t.parent.name == "Condo Gameplay" &&
                        !name.StartsWith("Player Spawn", StringComparison.Ordinal) &&
                        !name.StartsWith("Gameplay Safety Floor", StringComparison.Ordinal) &&
                        !name.StartsWith("Condo Assembly Point", StringComparison.Ordinal);
                return HasFurnitureWord(name) && !name.StartsWith("pb_Mesh", StringComparison.Ordinal);
            }

            if (name.StartsWith("TeamFurniture_", StringComparison.Ordinal) ||
                name.StartsWith("Kenney_", StringComparison.Ordinal))
                return true;

            // Saved original tutorial workstations, cover table and dress props.
            if (name == "SturdyCoverTableTop" || name.StartsWith("LabBenchTop", StringComparison.Ordinal) ||
                name.StartsWith("HangingLamp_", StringComparison.Ordinal) ||
                name.StartsWith("TaskItem_", StringComparison.Ordinal) ||
                name.StartsWith("UnsecuredTallCabinet", StringComparison.Ordinal) ||
                name.StartsWith("StagedFallingHazard_", StringComparison.Ordinal))
                return true;

            // TutorialVisualPolish also contains UI and safety markers. Only
            // recognize independently adjustable artwork and workstation decor.
            if (ParentNamed(t, "CEVR_FinalVisualPolish"))
                return HasFurnitureWord(name) && !IsVisualSafetyMarker(name);
            return HasFurnitureWord(name) && !IsVisualSafetyMarker(name);
        }

        private static bool HasEnabledRenderer(Transform item)
        {
            // Hidden fallback meshes and disabled construction parts are not
            // separate furniture items. Only the visible authored replacement
            // should get a setting.
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && renderer.enabled &&
                    renderer.gameObject.activeInHierarchy)
                    return true;
            return false;
        }

        private static bool IsExcluded(Transform item)
        {
            string name = item.name.ToLowerInvariant();
            bool fixture = name.Contains("lamp") || name.Contains("light");
            if ((name.Contains("floor") && !fixture) || name.Contains("ground") ||
                name.Contains("wall") || name.Contains("column") ||
                (name.Contains("ceiling") && !fixture) ||
                name.Contains("player") || name.Contains("camera") ||
                name.Contains("canvas") || name.Contains("hud") ||
                name.Contains("zone") || name.Contains("assembly") ||
                name.Contains("outline") || name.Contains("beacon") ||
                name.Contains("trigger") || name.Contains("exit") ||
                name.Contains("wayfinding") || name.Contains("template") ||
                name.Contains("quakevisual_ceiling"))
                return true;
            if (item.GetComponent<CoverZone>() != null) return true;
            if (item.GetComponent<HouseVisualRuntimeFollower>() != null) return true;
            return false;
        }

        private static bool IsVisualSafetyMarker(string value)
        {
            string name = value.ToLowerInvariant();
            return name.Contains("hazardboundary") || name.Contains("hazardfront") ||
                name.Contains("hazardleft") || name.Contains("hazardright") ||
                name.Contains("marker") || name.Contains("indicator") ||
                name.Contains("emergencystop") || name.Contains("path") ||
                name.Contains("coveroutline") || name.Contains("coverscrawl") ||
                name.Contains("cabinetwarningstripe");
        }

        private static bool HasFurnitureWord(string value)
        {
            string lower = value.ToLowerInvariant();
            foreach (string word in new[]
            {
                "chair", "table", "desk", "sofa", "couch", "bench",
                "cabinet", "wardrobe", "fridge", "shelf", "bookcase",
                "bed", "pillow", "books", "book", "plant", "vase",
                "monitor", "keyboard", "laptop", "computer",
                "lamp", "light", "storage", "tooltray",
                "fireextinguisher", "manual", "whiteboardmarker",
                "safety shoes", "shoes", "cup", "bottle",
                "screen", "television"
            })
                if (lower.Contains(word)) return true;
            return false;
        }

        private static DecorativeQuakeMode ModeFor(string name)
        {
            string lower = name.ToLowerInvariant();
            if (lower.Contains("lamp") || lower.Contains("light")) return DecorativeQuakeMode.Hanging;
            if (lower.Contains("monitor") || lower.Contains("screen") ||
                lower.Contains("television")) return DecorativeQuakeMode.Mounted;
            if (lower.Contains("chair") || lower.Contains("plant") ||
                lower.Contains("shelf")) return DecorativeQuakeMode.Rocking;
            if (FixedCoverOrFurniture(name)) return DecorativeQuakeMode.Heavy;
            return DecorativeQuakeMode.Loose;
        }

        private static bool FixedCoverOrFurniture(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("sturdy") || lower.Contains("labbench") ||
                lower.Contains("table") || lower.Contains("sofa") ||
                lower.Contains("bed") || lower.Contains("storage") ||
                lower.Contains("cabinet") || lower.Contains("desk") ||
                lower.Contains("bookcase") || lower.Contains("fridge") ||
                lower.Contains("shelf");
        }

        private static bool ParentNamed(Transform t, string rootName)
        {
            for (Transform current = t.parent; current != null; current = current.parent)
                if (current.name == rootName) return true;
            return false;
        }

        private static bool HasPhysicsOrVisualAncestor(Transform t)
        {
            for (Transform parent = t.parent; parent != null; parent = parent.parent)
                if (parent.GetComponent<Rigidbody>() != null ||
                    parent.GetComponent<DecorativeQuakeResponse>() != null ||
                    parent.GetComponent<VisualQuakeSway>() != null)
                    return true;
            return false;
        }

        private static int Depth(Transform t)
        {
            int depth = 0;
            while (t.parent != null) { depth++; t = t.parent; }
            return depth;
        }
    }
}
