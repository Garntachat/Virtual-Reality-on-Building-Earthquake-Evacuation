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
    /// Editor-only geometry audit for the authored ProBuilder House scene and every Pleng FBX.
    /// This intentionally DOES NOT place furniture. It produces a deterministic report first so
    /// furniture placement can be based on the actual house mesh, openings, floor clearance and
    /// imported model bounds instead of guessed runtime coordinates.
    /// </summary>
    public static class HouseProBuilderLayoutAnalyzer
    {
        private const string HouseScenePath = "Assets/CEVR/Generated/Scenes/House.unity";
        private const string PlengFolder = "Assets/CEVR/Resources/PlengFurniture";
        private const string ReportPath = "Assets/CEVR/Generated/House_ProBuilder_Layout_Analysis.txt";
        private const float Grid = 0.25f;
        private const float WalkHeight = 1.75f;
        private const float WalkRadius = 0.28f;

        private static readonly string[] PlengNames =
        {
            "Bed", "Bed_Pillow", "DiningChair", "DiningTable", "Fridge",
            "Sofa", "Sofa_Pillows", "Vase", "Wandrobe"
        };

        private sealed class Sample
        {
            public Vector3 floor;
            public float north, south, east, west;
            public float minWall;
        }

        [MenuItem("CEVR/Analyze House ProBuilder + Pleng Furniture", priority = 1)]
        public static void Analyze()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene original = SceneManager.GetActiveScene();
            string originalPath = original.path;

            Scene house = EditorSceneManager.OpenScene(HouseScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();

            var report = new StringBuilder(32768);
            report.AppendLine("CEVR HOUSE / PROBUILDER / PLENG FURNITURE ANALYSIS");
            report.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine("Scene: " + HouseScenePath);
            report.AppendLine(new string('=', 78));

            AnalyzeAuthoredGeometry(house, report, out Bounds houseBounds, out List<Sample> samples);
            AnalyzeFloorPlan(houseBounds, samples, report);
            AnalyzeOpenings(houseBounds, report);
            AnalyzePlengFurniture(report);

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Assets/CEVR/Generated");
            File.WriteAllText(ReportPath, report.ToString());
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Debug.Log("CEVR House analysis complete. Report: " + ReportPath +
                      "\nNo furniture was moved by this analyzer.");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(ReportPath);
            EditorGUIUtility.PingObject(Selection.activeObject);

            // Restore the user's previous scene when it has a saved path; otherwise leave House open
            // so the generated report and its geometry can be inspected together.
            if (!string.IsNullOrEmpty(originalPath) && originalPath != HouseScenePath)
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        }

        private static void AnalyzeAuthoredGeometry(Scene scene, StringBuilder report,
            out Bounds houseBounds, out List<Sample> samples)
        {
            report.AppendLine();
            report.AppendLine("1. AUTHORED HOUSE GEOMETRY");
            report.AppendLine("--------------------------");

            MeshFilter[] filters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .Where(f => f.gameObject.scene == scene && f.sharedMesh != null).ToArray();
            Collider[] colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .Where(c => c.gameObject.scene == scene).ToArray();

            bool haveBounds = false;
            houseBounds = default;
            foreach (MeshFilter filter in filters)
            {
                Renderer r = filter.GetComponent<Renderer>();
                if (r != null && r.enabled)
                {
                    if (!haveBounds) { houseBounds = r.bounds; haveBounds = true; }
                    else houseBounds.Encapsulate(r.bounds);
                }

                Mesh mesh = filter.sharedMesh;
                string componentTypes = string.Join(", ", filter.GetComponents<Component>()
                    .Where(c => c != null).Select(c => c.GetType().FullName)
                    .Where(n => n != null && (n.Contains("ProBuilder") || n.Contains("Collider"))));
                report.AppendLine($"OBJECT {filter.name}");
                report.AppendLine($"  world position = {Vec(filter.transform.position)}");
                report.AppendLine($"  world rotation = {Vec(filter.transform.eulerAngles)}");
                report.AppendLine($"  lossy scale    = {Vec(filter.transform.lossyScale)}");
                report.AppendLine($"  mesh           = {mesh.name}; vertices={mesh.vertexCount}; submeshes={mesh.subMeshCount}");
                report.AppendLine($"  local bounds   = center {Vec(mesh.bounds.center)} size {Vec(mesh.bounds.size)}");
                if (r != null) report.AppendLine($"  world bounds   = center {Vec(r.bounds.center)} size {Vec(r.bounds.size)}");
                if (!string.IsNullOrEmpty(componentTypes)) report.AppendLine("  geometry comps = " + componentTypes);
            }

            report.AppendLine($"\nAuthored mesh count: {filters.Length}");
            report.AppendLine($"Authored collider count: {colliders.Length}");
            if (haveBounds)
            {
                report.AppendLine($"Combined visible bounds min={Vec(houseBounds.min)} max={Vec(houseBounds.max)} size={Vec(houseBounds.size)}");
            }

            samples = SampleWalkableFloor(houseBounds, scene);
            report.AppendLine($"Walkable ground-floor grid samples ({Grid:F2}m spacing): {samples.Count}");
        }

        private static List<Sample> SampleWalkableFloor(Bounds bounds, Scene scene)
        {
            var result = new List<Sample>();
            if (bounds.size == Vector3.zero) return result;

            float castStart = Mathf.Min(2.4f, bounds.max.y + 0.25f);
            int xCount = Mathf.CeilToInt(bounds.size.x / Grid);
            int zCount = Mathf.CeilToInt(bounds.size.z / Grid);

            for (int ix = 0; ix <= xCount; ix++)
            {
                float x = bounds.min.x + ix * Grid;
                for (int iz = 0; iz <= zCount; iz++)
                {
                    float z = bounds.min.z + iz * Grid;
                    Ray ray = new Ray(new Vector3(x, castStart, z), Vector3.down);
                    RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Max(4f, castStart - bounds.min.y + 1f), ~0,
                        QueryTriggerInteraction.Ignore);
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    RaycastHit floorHit = default;
                    bool found = false;
                    foreach (RaycastHit hit in hits)
                    {
                        if (hit.collider == null || hit.collider.gameObject.scene != scene) continue;
                        if (hit.normal.y < 0.65f) continue;
                        // Focus on the ground storey. This excludes upper floor/stair landings.
                        if (hit.point.y < bounds.min.y - 0.15f || hit.point.y > bounds.min.y + 1.15f) continue;
                        floorHit = hit; found = true; break;
                    }
                    if (!found) continue;

                    Vector3 capsuleBottom = floorHit.point + Vector3.up * WalkRadius;
                    Vector3 capsuleTop = floorHit.point + Vector3.up * (WalkHeight - WalkRadius);
                    Collider[] blocking = Physics.OverlapCapsule(capsuleBottom, capsuleTop, WalkRadius, ~0,
                        QueryTriggerInteraction.Ignore);
                    bool blocked = blocking.Any(c => c != null && c.gameObject.scene == scene &&
                        c != floorHit.collider && !IsMostlyHorizontal(c));
                    if (blocked) continue;

                    var s = new Sample { floor = floorHit.point };
                    s.east = WallDistance(s.floor, Vector3.right, scene);
                    s.west = WallDistance(s.floor, Vector3.left, scene);
                    s.north = WallDistance(s.floor, Vector3.forward, scene);
                    s.south = WallDistance(s.floor, Vector3.back, scene);
                    s.minWall = Mathf.Min(Mathf.Min(s.east, s.west), Mathf.Min(s.north, s.south));
                    result.Add(s);
                }
            }
            return result;
        }

        private static bool IsMostlyHorizontal(Collider collider)
        {
            Bounds b = collider.bounds;
            return b.size.y < 0.18f && (b.size.x > 0.5f || b.size.z > 0.5f);
        }

        private static float WallDistance(Vector3 floor, Vector3 direction, Scene scene)
        {
            Vector3 origin = floor + Vector3.up * 0.85f;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, 20f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject.scene != scene) continue;
                if (Mathf.Abs(hit.normal.y) > 0.72f) continue;
                return hit.distance;
            }
            return 20f;
        }

        private static void AnalyzeFloorPlan(Bounds bounds, List<Sample> samples, StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("2. GROUND-FLOOR SPATIAL / CLEARANCE ANALYSIS");
            report.AppendLine("------------------------------------------");
            if (samples.Count == 0)
            {
                report.AppendLine("No walkable floor samples found. Check MeshCollider state in House scene.");
                return;
            }

            report.AppendLine($"Walkable X range: {samples.Min(s => s.floor.x):F2} .. {samples.Max(s => s.floor.x):F2}");
            report.AppendLine($"Walkable Z range: {samples.Min(s => s.floor.z):F2} .. {samples.Max(s => s.floor.z):F2}");
            report.AppendLine($"Ground Y median: {Median(samples.Select(s => s.floor.y)):F3}");

            // Highest-clearance points are useful anchors for large furniture groups without assuming
            // any semantic room names. Keep points spatially separated so the report covers the house.
            var candidates = samples.OrderByDescending(s => s.minWall).ToList();
            var selected = new List<Sample>();
            foreach (Sample sample in candidates)
            {
                if (selected.Any(x => Vector2.Distance(new Vector2(x.floor.x, x.floor.z),
                    new Vector2(sample.floor.x, sample.floor.z)) < 1.5f)) continue;
                selected.Add(sample);
                if (selected.Count >= 16) break;
            }

            report.AppendLine("\nHighest-clearance candidate zones (NOT placements yet):");
            foreach (Sample s in selected.OrderBy(s => s.floor.z).ThenBy(s => s.floor.x))
                report.AppendLine($"  {Vec(s.floor)} clearance E/W/N/S = {s.east:F2}/{s.west:F2}/{s.north:F2}/{s.south:F2}m; nearest wall={s.minWall:F2}m");

            // Coarse occupancy map for visual inspection in the text report.
            report.AppendLine("\nCoarse top-down occupancy (0.50m; #=walkable, .=not walkable; +Z at top):");
            float step = 0.50f;
            int nx = Mathf.CeilToInt(bounds.size.x / step);
            int nz = Mathf.CeilToInt(bounds.size.z / step);
            var set = new HashSet<string>(samples.Select(s => Key(s.floor.x, s.floor.z, step)));
            for (int iz = nz; iz >= 0; iz--)
            {
                float z = bounds.min.z + iz * step;
                var line = new StringBuilder(nx + 1);
                for (int ix = 0; ix <= nx; ix++)
                {
                    float x = bounds.min.x + ix * step;
                    line.Append(set.Contains(Key(x, z, step)) ? '#' : '.');
                }
                report.AppendLine(line.ToString());
            }
        }

        private static void AnalyzeOpenings(Bounds bounds, StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("3. WALL OPENING / WINDOW CANDIDATES");
            report.AppendLine("---------------------------------");
            report.AppendLine("The runtime furniture pass must not invent window coordinates. This section reports wall-facing gaps inferred from authored colliders.");

            float y = bounds.min.y + 1.55f;
            string[] labels = { "+Z", "-Z", "+X", "-X" };
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            Vector3 center = new Vector3(bounds.center.x, y, bounds.center.z);
            for (int d = 0; d < dirs.Length; d++)
            {
                report.AppendLine("  Side " + labels[d] + ": ray scan from house center at y=" + y.ToString("F2"));
                for (float lateral = -4.5f; lateral <= 4.5f; lateral += 0.5f)
                {
                    Vector3 origin = center;
                    if (d < 2) origin.x += lateral; else origin.z += lateral;
                    if (Physics.Raycast(origin, dirs[d], out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                        report.AppendLine($"    lateral {lateral,5:F1}: hit {hit.collider.name} at {Vec(hit.point)} distance {hit.distance:F2}");
                    else
                        report.AppendLine($"    lateral {lateral,5:F1}: OPEN / no collider hit");
                }
            }
        }

        private static void AnalyzePlengFurniture(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("4. PLENG FURNITURE IMPORT / BOUNDS AUDIT");
            report.AppendLine("--------------------------------------");

            foreach (string name in PlengNames)
            {
                string path = PlengFolder + "/" + name + ".fbx";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    report.AppendLine($"{name}: MISSING / failed to import: {path}");
                    continue;
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null) instance = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    instance.hideFlags = HideFlags.HideAndDontSave;
                    instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    instance.transform.localScale = Vector3.one;
                    Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                    Bounds b = default;
                    bool found = false;
                    foreach (Renderer r in renderers)
                    {
                        if (!r.enabled || IsCollisionNamed(r.transform, instance.transform)) continue;
                        if (!found) { b = r.bounds; found = true; }
                        else b.Encapsulate(r.bounds);
                    }

                    report.AppendLine(name + ":");
                    report.AppendLine("  asset = " + path);
                    report.AppendLine($"  root scale={Vec(prefab.transform.localScale)} root rotation={Vec(prefab.transform.localEulerAngles)}");
                    report.AppendLine($"  renderers={renderers.Length}; colliders={instance.GetComponentsInChildren<Collider>(true).Length}");
                    if (found)
                    {
                        report.AppendLine($"  raw visible world bounds center={Vec(b.center)} size={Vec(b.size)} min={Vec(b.min)} max={Vec(b.max)}");
                        report.AppendLine($"  source bottom-center offset from root = {Vec(new Vector3(b.center.x, b.min.y, b.center.z))}");
                    }
                    else report.AppendLine("  ERROR: no visible renderer bounds");

                    foreach (Renderer r in renderers)
                        report.AppendLine($"    renderer {PathOf(r.transform, instance.transform)} bounds center={Vec(r.bounds.center)} size={Vec(r.bounds.size)} enabled={r.enabled}");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        private static bool IsCollisionNamed(Transform t, Transform root)
        {
            while (t != null)
            {
                if (t.name.IndexOf("collision", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (t == root) break;
                t = t.parent;
            }
            return false;
        }

        private static string PathOf(Transform t, Transform root)
        {
            var names = new Stack<string>();
            while (t != null)
            {
                names.Push(t.name);
                if (t == root) break;
                t = t.parent;
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
            float[] a = values.OrderBy(v => v).ToArray();
            if (a.Length == 0) return 0f;
            int mid = a.Length / 2;
            return a.Length % 2 == 0 ? (a[mid - 1] + a[mid]) * 0.5f : a[mid];
        }

        private static string Vec(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
    }
}
