using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>
    /// Read-only geometry audit for the authored House scene and every Pleng FBX.
    /// The analyzer never places furniture and never saves or modifies House.unity.
    /// </summary>
    public static class HouseProBuilderLayoutAnalyzer
    {
        private const string HouseScenePath = "Assets/CEVR/Generated/Scenes/House.unity";
        private const string PlengFolder = "Assets/CEVR/Resources/PlengFurniture";
        private const string ReportPath = "Assets/CEVR/Generated/House_ProBuilder_Layout_Analysis.txt";
        private const float FloorScanGrid = 0.50f;
        private const float WalkGrid = 0.25f;
        private const float LevelBin = 0.25f;
        private const float WalkHeight = 1.75f;
        private const float WalkRadius = 0.28f;
        private const float FloorGap = 0.04f;

        private static readonly string[] PlengNames =
        {
            "Bed", "Bed_Pillow", "DiningChair", "DiningTable", "Fridge",
            "Sofa", "Sofa_Pillows", "Vase", "Wandrobe"
        };

        private sealed class Sample
        {
            public Vector3 floor;
            public float north;
            public float south;
            public float east;
            public float west;
            public float minWall;
        }

        private sealed class LevelCandidate
        {
            public float y;
            public int hits;
        }

        [MenuItem("CEVR/Analyze House ProBuilder + Pleng Furniture", priority = 1)]
        public static void Analyze()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("CEVR House analysis is Editor-only. Exit Play Mode and run it again.");
                return;
            }

            Scene originalActive = SceneManager.GetActiveScene();
            Scene house = SceneManager.GetSceneByPath(HouseScenePath);
            bool openedHouse = !house.IsValid() || !house.isLoaded;

            try
            {
                if (openedHouse)
                    house = EditorSceneManager.OpenScene(HouseScenePath, OpenSceneMode.Additive);

                if (!house.IsValid() || !house.isLoaded)
                    throw new InvalidOperationException("Could not open House scene: " + HouseScenePath);

                Physics.SyncTransforms();

                var report = new StringBuilder(65536);
                report.AppendLine("CEVR HOUSE / PROBUILDER / PLENG FURNITURE ANALYSIS");
                report.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                report.AppendLine("Scene: " + HouseScenePath);
                report.AppendLine("Mode: READ-ONLY. No scene objects are moved, created, deleted, or saved.");
                report.AppendLine(new string('=', 88));

                AnalyzeAuthoredGeometry(house, report, out Bounds houseBounds);
                List<LevelCandidate> levels = DetectHorizontalLevels(house, houseBounds, report);
                AnalyzeWalkableLevels(house, houseBounds, levels, report);
                AnalyzeExteriorOpenings(house, houseBounds, levels, report);
                AnalyzePlengFurniture(report);

                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Assets/CEVR/Generated");
                File.WriteAllText(ReportPath, report.ToString());
                AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();

                TextAsset generated = AssetDatabase.LoadAssetAtPath<TextAsset>(ReportPath);
                Selection.activeObject = generated;
                if (generated != null) EditorGUIUtility.PingObject(generated);

                Debug.Log("CEVR House analysis complete: " + ReportPath +
                          "\nHouse.unity was not modified and no furniture was placed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
            finally
            {
                if (openedHouse && house.IsValid() && house.isLoaded)
                    EditorSceneManager.CloseScene(house, true);

                if (originalActive.IsValid() && originalActive.isLoaded)
                    SceneManager.SetActiveScene(originalActive);
            }
        }

        private static void AnalyzeAuthoredGeometry(Scene scene, StringBuilder report, out Bounds houseBounds)
        {
            report.AppendLine();
            report.AppendLine("1. AUTHORED HOUSE GEOMETRY");
            report.AppendLine("--------------------------");

            MeshFilter[] filters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .Where(filter => filter != null && filter.gameObject.scene == scene && filter.sharedMesh != null)
                .OrderBy(filter => HierarchyPath(filter.transform))
                .ToArray();
            Collider[] colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .Where(collider => collider != null && collider.gameObject.scene == scene)
                .ToArray();

            bool haveBounds = false;
            houseBounds = default;

            foreach (MeshFilter filter in filters)
            {
                Renderer renderer = filter.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled)
                {
                    if (!haveBounds)
                    {
                        houseBounds = renderer.bounds;
                        haveBounds = true;
                    }
                    else
                    {
                        houseBounds.Encapsulate(renderer.bounds);
                    }
                }

                Mesh mesh = filter.sharedMesh;
                string componentTypes = string.Join(", ", filter.GetComponents<Component>()
                    .Where(component => component != null)
                    .Select(component => component.GetType().FullName)
                    .Where(name => name != null &&
                        (name.Contains("ProBuilder") || name.Contains("Collider"))));

                report.AppendLine("OBJECT " + HierarchyPath(filter.transform));
                report.AppendLine("  world position = " + Vec(filter.transform.position));
                report.AppendLine("  world rotation = " + Vec(filter.transform.eulerAngles));
                report.AppendLine("  lossy scale    = " + Vec(filter.transform.lossyScale));
                report.AppendLine($"  mesh           = {mesh.name}; vertices={mesh.vertexCount}; submeshes={mesh.subMeshCount}");
                report.AppendLine("  local bounds   = center " + Vec(mesh.bounds.center) + " size " + Vec(mesh.bounds.size));
                if (renderer != null)
                    report.AppendLine("  world bounds   = center " + Vec(renderer.bounds.center) + " size " + Vec(renderer.bounds.size));
                if (!string.IsNullOrEmpty(componentTypes))
                    report.AppendLine("  geometry comps = " + componentTypes);
            }

            report.AppendLine();
            report.AppendLine("Authored mesh count: " + filters.Length);
            report.AppendLine("Authored collider count: " + colliders.Length);
            if (haveBounds)
            {
                report.AppendLine("Combined visible bounds min=" + Vec(houseBounds.min) +
                                  " max=" + Vec(houseBounds.max) +
                                  " size=" + Vec(houseBounds.size));
            }
            else
            {
                report.AppendLine("ERROR: House has no enabled visible MeshRenderer bounds.");
            }
        }

        private static List<LevelCandidate> DetectHorizontalLevels(Scene scene, Bounds bounds, StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("2. HORIZONTAL SURFACE / STOREY CANDIDATES");
            report.AppendLine("-----------------------------------------");

            var bins = new Dictionary<int, List<float>>();
            if (bounds.size == Vector3.zero)
            {
                report.AppendLine("No house bounds available.");
                return new List<LevelCandidate>();
            }

            float castY = bounds.max.y + 0.75f;
            int xCount = Mathf.CeilToInt(bounds.size.x / FloorScanGrid);
            int zCount = Mathf.CeilToInt(bounds.size.z / FloorScanGrid);

            for (int ix = 0; ix <= xCount; ix++)
            {
                float x = bounds.min.x + ix * FloorScanGrid;
                for (int iz = 0; iz <= zCount; iz++)
                {
                    float z = bounds.min.z + iz * FloorScanGrid;
                    RaycastHit[] hits = Physics.RaycastAll(
                        new Vector3(x, castY, z), Vector3.down,
                        bounds.size.y + 1.5f, ~0, QueryTriggerInteraction.Ignore);

                    foreach (RaycastHit hit in hits)
                    {
                        if (!BelongsToScene(hit.collider, scene) || hit.normal.y < 0.72f) continue;
                        int bin = Mathf.RoundToInt(hit.point.y / LevelBin);
                        if (!bins.TryGetValue(bin, out List<float> values))
                        {
                            values = new List<float>();
                            bins[bin] = values;
                        }
                        values.Add(hit.point.y);
                    }
                }
            }

            List<LevelCandidate> all = bins
                .Select(pair => new LevelCandidate
                {
                    y = pair.Value.Average(),
                    hits = pair.Value.Count
                })
                .OrderBy(candidate => candidate.y)
                .ToList();

            foreach (LevelCandidate candidate in all)
                report.AppendLine($"  y={candidate.y,7:F3} m : {candidate.hits,5} horizontal hits");

            int meaningfulThreshold = Mathf.Max(6, Mathf.RoundToInt((bounds.size.x * bounds.size.z) * 0.015f));
            var meaningful = new List<LevelCandidate>();
            foreach (LevelCandidate candidate in all.OrderByDescending(candidate => candidate.hits))
            {
                if (candidate.hits < meaningfulThreshold) continue;
                if (meaningful.Any(existing => Mathf.Abs(existing.y - candidate.y) < 0.45f)) continue;
                meaningful.Add(candidate);
                if (meaningful.Count >= 6) break;
            }

            meaningful = meaningful.OrderBy(candidate => candidate.y).ToList();
            report.AppendLine();
            report.AppendLine("Meaningful levels selected for walkability analysis:");
            if (meaningful.Count == 0)
                report.AppendLine("  None. Inspect the collider setup before using this report for placement.");
            else
                foreach (LevelCandidate candidate in meaningful)
                    report.AppendLine($"  y={candidate.y:F3} m ({candidate.hits} supporting surface hits)");

            return meaningful;
        }

        private static void AnalyzeWalkableLevels(
            Scene scene, Bounds bounds, List<LevelCandidate> levels, StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("3. WALKABLE CLEARANCE BY LEVEL");
            report.AppendLine("------------------------------");
            report.AppendLine("Clearance uses an elevated 1.75 m capsule. It does NOT ignore an entire floor MeshCollider,");
            report.AppendLine("so walls that share the same ProBuilder mesh/collider as a floor still block the sample.");

            foreach (LevelCandidate level in levels)
            {
                List<Sample> samples = SampleWalkableAtLevel(scene, bounds, level.y);
                report.AppendLine();
                report.AppendLine($"LEVEL y~{level.y:F3} m: {samples.Count} walkable samples at {WalkGrid:F2} m spacing");
                if (samples.Count == 0) continue;

                report.AppendLine($"  X range: {samples.Min(sample => sample.floor.x):F2} .. {samples.Max(sample => sample.floor.x):F2}");
                report.AppendLine($"  Z range: {samples.Min(sample => sample.floor.z):F2} .. {samples.Max(sample => sample.floor.z):F2}");
                report.AppendLine($"  Median floor Y: {Median(samples.Select(sample => sample.floor.y)):F3}");

                var candidates = samples.OrderByDescending(sample => sample.minWall).ToList();
                var selected = new List<Sample>();
                foreach (Sample sample in candidates)
                {
                    if (selected.Any(existing => Vector2.Distance(
                            new Vector2(existing.floor.x, existing.floor.z),
                            new Vector2(sample.floor.x, sample.floor.z)) < 1.5f))
                        continue;
                    selected.Add(sample);
                    if (selected.Count >= 16) break;
                }

                report.AppendLine("  Highest-clearance anchors (analysis only; NOT furniture placements):");
                foreach (Sample sample in selected.OrderBy(sample => sample.floor.z).ThenBy(sample => sample.floor.x))
                {
                    report.AppendLine($"    {Vec(sample.floor)} E/W/N/S={sample.east:F2}/{sample.west:F2}/{sample.north:F2}/{sample.south:F2} m; nearest={sample.minWall:F2} m");
                }

                AppendOccupancyMap(bounds, samples, report);
            }
        }

        private static List<Sample> SampleWalkableAtLevel(Scene scene, Bounds bounds, float levelY)
        {
            var samples = new List<Sample>();
            int xCount = Mathf.CeilToInt(bounds.size.x / WalkGrid);
            int zCount = Mathf.CeilToInt(bounds.size.z / WalkGrid);
            float castStart = Mathf.Min(bounds.max.y + 0.5f, levelY + 2.2f);
            float castDistance = Mathf.Max(2.6f, castStart - levelY + 0.6f);

            for (int ix = 0; ix <= xCount; ix++)
            {
                float x = bounds.min.x + ix * WalkGrid;
                for (int iz = 0; iz <= zCount; iz++)
                {
                    float z = bounds.min.z + iz * WalkGrid;
                    RaycastHit[] hits = Physics.RaycastAll(
                        new Vector3(x, castStart, z), Vector3.down,
                        castDistance, ~0, QueryTriggerInteraction.Ignore);
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    bool foundFloor = false;
                    RaycastHit floorHit = default;
                    foreach (RaycastHit hit in hits)
                    {
                        if (!BelongsToScene(hit.collider, scene) || hit.normal.y < 0.72f) continue;
                        if (Mathf.Abs(hit.point.y - levelY) > 0.32f) continue;
                        floorHit = hit;
                        foundFloor = true;
                        break;
                    }
                    if (!foundFloor) continue;

                    Vector3 bottom = floorHit.point + Vector3.up * (WalkRadius + FloorGap);
                    Vector3 top = floorHit.point + Vector3.up * (WalkHeight - WalkRadius - FloorGap);
                    Collider[] overlaps = Physics.OverlapCapsule(
                        bottom, top, WalkRadius, ~0, QueryTriggerInteraction.Ignore);
                    bool blocked = overlaps.Any(collider => BelongsToScene(collider, scene));
                    if (blocked) continue;

                    var sample = new Sample { floor = floorHit.point };
                    sample.east = WallDistance(sample.floor, Vector3.right, scene);
                    sample.west = WallDistance(sample.floor, Vector3.left, scene);
                    sample.north = WallDistance(sample.floor, Vector3.forward, scene);
                    sample.south = WallDistance(sample.floor, Vector3.back, scene);
                    sample.minWall = Mathf.Min(
                        Mathf.Min(sample.east, sample.west),
                        Mathf.Min(sample.north, sample.south));
                    samples.Add(sample);
                }
            }

            return samples;
        }

        private static float WallDistance(Vector3 floor, Vector3 direction, Scene scene)
        {
            Vector3 origin = floor + Vector3.up * 0.90f;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, 20f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (!BelongsToScene(hit.collider, scene)) continue;
                if (Mathf.Abs(hit.normal.y) > 0.72f) continue;
                return hit.distance;
            }
            return 20f;
        }

        private static void AppendOccupancyMap(Bounds bounds, List<Sample> samples, StringBuilder report)
        {
            const float step = 0.50f;
            int nx = Mathf.CeilToInt(bounds.size.x / step);
            int nz = Mathf.CeilToInt(bounds.size.z / step);
            var occupied = new HashSet<string>(samples.Select(sample => Key(sample.floor.x, sample.floor.z, step)));

            report.AppendLine("  Coarse top-down walkability (0.50 m; #=walkable, .=blocked/outside, +Z at top):");
            for (int iz = nz; iz >= 0; iz--)
            {
                float z = bounds.min.z + iz * step;
                var line = new StringBuilder(nx + 1);
                for (int ix = 0; ix <= nx; ix++)
                {
                    float x = bounds.min.x + ix * step;
                    line.Append(occupied.Contains(Key(x, z, step)) ? '#' : '.');
                }
                report.AppendLine("    " + line);
            }
        }

        private static void AnalyzeExteriorOpenings(
            Scene scene, Bounds bounds, List<LevelCandidate> levels, StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("4. EXTERIOR WALL / OPENING RAY AUDIT");
            report.AppendLine("-----------------------------------");
            report.AppendLine("Rays start OUTSIDE the combined house bounds and travel inward, avoiding the old center-ray");
            report.AppendLine("failure mode where an internal wall could be mistaken for the exterior wall/window.");

            List<float> floorLevels = levels.Count > 0
                ? levels.Select(level => level.y).ToList()
                : new List<float> { bounds.min.y };

            foreach (float floorY in floorLevels)
            {
                report.AppendLine();
                report.AppendLine($"LEVEL y~{floorY:F3} m");
                ScanExteriorSide(scene, bounds, floorY, "+Z", Vector3.back, true, report);
                ScanExteriorSide(scene, bounds, floorY, "-Z", Vector3.forward, true, report);
                ScanExteriorSide(scene, bounds, floorY, "+X", Vector3.left, false, report);
                ScanExteriorSide(scene, bounds, floorY, "-X", Vector3.right, false, report);
            }
        }

        private static void ScanExteriorSide(
            Scene scene, Bounds bounds, float floorY, string label, Vector3 inward,
            bool varyX, StringBuilder report)
        {
            float lateralMin = varyX ? bounds.min.x : bounds.min.z;
            float lateralMax = varyX ? bounds.max.x : bounds.max.z;
            float outside = label == "+Z" ? bounds.max.z + 0.60f :
                            label == "-Z" ? bounds.min.z - 0.60f :
                            label == "+X" ? bounds.max.x + 0.60f : bounds.min.x - 0.60f;

            report.AppendLine("  Side " + label + ":");
            for (float lateral = lateralMin; lateral <= lateralMax + 0.01f; lateral += 0.50f)
            {
                var pattern = new StringBuilder();
                foreach (float height in new[] { 0.55f, 1.15f, 1.75f, 2.35f })
                {
                    Vector3 origin;
                    if (varyX)
                        origin = new Vector3(lateral, floorY + height, outside);
                    else
                        origin = new Vector3(outside, floorY + height, lateral);

                    bool hitScene = TryFirstSceneHit(origin, inward, bounds.size.magnitude + 2f, scene, out RaycastHit hit);
                    if (hitScene)
                        pattern.Append($" h{height:F2}=HIT:{hit.collider.name}@{hit.distance:F2}");
                    else
                        pattern.Append($" h{height:F2}=OPEN");
                }
                report.AppendLine($"    lateral {lateral,7:F2}:{pattern}");
            }
        }

        private static bool TryFirstSceneHit(
            Vector3 origin, Vector3 direction, float distance, Scene scene, out RaycastHit first)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (!BelongsToScene(hit.collider, scene)) continue;
                first = hit;
                return true;
            }
            first = default;
            return false;
        }

        private static void AnalyzePlengFurniture(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("5. PLENG FURNITURE IMPORT / BOUNDS AUDIT");
            report.AppendLine("--------------------------------------");

            foreach (string name in PlengNames)
            {
                string path = PlengFolder + "/" + name + ".fbx";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

                report.AppendLine();
                report.AppendLine(name + ":");
                report.AppendLine("  asset = " + path);

                if (importer == null)
                {
                    report.AppendLine("  ERROR: no ModelImporter found");
                }
                else
                {
                    report.AppendLine($"  importer globalScale={importer.globalScale:F4}; useFileScale={importer.useFileScale}; readable={importer.isReadable}; addCollider={importer.addCollider}");
                    report.AppendLine($"  importer animations={importer.importAnimation}; cameras={importer.importCameras}; lights={importer.importLights}");
                }

                if (prefab == null)
                {
                    report.AppendLine("  ERROR: FBX failed to import as a GameObject");
                    continue;
                }

                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    instance.name = "CEVR_Analysis_" + name;
                    instance.hideFlags = HideFlags.HideAndDontSave;
                    instance.transform.position = Vector3.zero;
                    Physics.SyncTransforms();

                    Vector3 rootScale = instance.transform.localScale;
                    Vector3 rootRotation = instance.transform.localEulerAngles;
                    Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                    Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
                    Bounds bounds = default;
                    bool found = false;

                    foreach (Renderer renderer in renderers)
                    {
                        if (!renderer.enabled || IsCollisionNamed(renderer.transform, instance.transform)) continue;
                        if (!found)
                        {
                            bounds = renderer.bounds;
                            found = true;
                        }
                        else
                        {
                            bounds.Encapsulate(renderer.bounds);
                        }
                    }

                    report.AppendLine("  root local scale=" + Vec(rootScale) + " rotation=" + Vec(rootRotation));
                    report.AppendLine($"  renderers={renderers.Length}; imported colliders={colliders.Length}");
                    if (!found)
                    {
                        report.AppendLine("  ERROR: no enabled non-collision renderer bounds");
                    }
                    else
                    {
                        report.AppendLine("  visible bounds center=" + Vec(bounds.center) +
                                          " size=" + Vec(bounds.size) +
                                          " min=" + Vec(bounds.min) +
                                          " max=" + Vec(bounds.max));
                        report.AppendLine("  bottom-center offset from root=" +
                                          Vec(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) - instance.transform.position));
                        report.AppendLine($"  aspect X:Y:Z = {SafeRatio(bounds.size.x, bounds.size.y):F3}:{1f:F3}:{SafeRatio(bounds.size.z, bounds.size.y):F3}");
                    }

                    foreach (Renderer renderer in renderers.OrderBy(renderer => HierarchyPath(renderer.transform)))
                    {
                        report.AppendLine("    renderer " + HierarchyPath(renderer.transform, instance.transform) +
                                          " enabled=" + renderer.enabled +
                                          " bounds center=" + Vec(renderer.bounds.center) +
                                          " size=" + Vec(renderer.bounds.size));
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        private static bool BelongsToScene(Collider collider, Scene scene)
        {
            return collider != null && collider.gameObject.scene == scene;
        }

        private static bool IsCollisionNamed(Transform candidate, Transform root)
        {
            Transform current = candidate;
            while (current != null)
            {
                if (current.name.IndexOf("collision", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (current == root) break;
                current = current.parent;
            }
            return false;
        }

        private static string HierarchyPath(Transform transform, Transform stopAt = null)
        {
            if (transform == null) return "<null>";
            var names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                if (current == stopAt) break;
                current = current.parent;
            }
            return string.Join("/", names);
        }

        private static string Key(float x, float z, float step)
        {
            int ix = Mathf.RoundToInt(x / step);
            int iz = Mathf.RoundToInt(z / step);
            return ix.ToString(CultureInfo.InvariantCulture) + ":" + iz.ToString(CultureInfo.InvariantCulture);
        }

        private static float Median(IEnumerable<float> values)
        {
            float[] ordered = values.OrderBy(value => value).ToArray();
            if (ordered.Length == 0) return 0f;
            int middle = ordered.Length / 2;
            return ordered.Length % 2 == 0
                ? (ordered[middle - 1] + ordered[middle]) * 0.5f
                : ordered[middle];
        }

        private static float SafeRatio(float numerator, float denominator)
        {
            return numerator / Mathf.Max(0.0001f, denominator);
        }

        private static string Vec(Vector3 value)
        {
            return $"({value.x:F3}, {value.y:F3}, {value.z:F3})";
        }
    }
}
