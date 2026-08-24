#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChulaEarthquakeVR.Editor
{
    public static class GroundMotionCsvImporter
    {
        [MenuItem("Tools/CEVR/Import Ground Motion CSV...")]
        public static void Import()
        {
            string sourcePath = EditorUtility.OpenFilePanel("Ground motion CSV", "", "csv");
            if (string.IsNullOrEmpty(sourcePath)) return;
            try
            {
                Parse(sourcePath, out float sampleRate, out Vector3[] samples, out string units);
                string assetPath = EditorUtility.SaveFilePanelInProject(
                    "Save Quake Profile", Path.GetFileNameWithoutExtension(sourcePath), "asset", "Choose an asset path.");
                if (string.IsNullOrEmpty(assetPath)) return;
                var profile = ScriptableObject.CreateInstance<QuakeProfile>();
                profile.Initialize(Path.GetFileNameWithoutExtension(sourcePath), sampleRate, samples,
                    $"Imported from {Path.GetFileName(sourcePath)}; input units={units}; stored as m/s².");
                AssetDatabase.CreateAsset(profile, assetPath);
                AssetDatabase.SaveAssets();
                Selection.activeObject = profile;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Ground Motion Import Failed", exception.Message, "OK");
            }
        }

        private static void Parse(string path, out float sampleRate, out Vector3[] samples, out string units)
        {
            var times = new List<float>();
            var values = new List<Vector3>();
            units = "m/s2";
            bool headerSeen = false;
            bool dataSeen = false;
            foreach (string raw in File.ReadLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#", StringComparison.Ordinal))
                {
                    int marker = line.IndexOf("units=", StringComparison.OrdinalIgnoreCase);
                    if (marker >= 0)
                    {
                        if (dataSeen) throw new FormatException("Declare units before data rows.");
                        string declared = line.Substring(marker + 6).Trim().ToLowerInvariant();
                        units = declared switch
                        {
                            "g" => "g",
                            "m/s2" => "m/s2",
                            "m/s^2" => "m/s2",
                            _ => throw new FormatException($"Unsupported acceleration units: {declared}")
                        };
                    }
                    continue;
                }
                if (!headerSeen)
                {
                    if (line.Replace(" ", string.Empty).ToLowerInvariant() != "time,ax,ay,az")
                        throw new FormatException("Expected header: time,ax,ay,az");
                    headerSeen = true;
                    continue;
                }
                string[] parts = line.Split(',');
                if (parts.Length != 4) throw new FormatException($"Expected four columns: {line}");
                float time = Number(parts[0]);
                Vector3 acceleration = new Vector3(Number(parts[1]), Number(parts[2]), Number(parts[3]));
                if (!Finite(time) || !Finite(acceleration.x) || !Finite(acceleration.y) || !Finite(acceleration.z))
                    throw new FormatException("NaN and Infinity are not allowed.");
                if (units == "g") acceleration *= Physics.gravity.magnitude;
                times.Add(time);
                values.Add(acceleration);
                dataSeen = true;
            }
            if (times.Count < 2) throw new FormatException("At least two data rows are required.");
            float totalDelta = 0f;
            for (int i = 1; i < times.Count; i++)
            {
                float delta = times[i] - times[i - 1];
                if (delta <= 0f) throw new FormatException("Time values must be strictly increasing.");
                totalDelta += delta;
            }
            float meanDelta = totalDelta / (times.Count - 1);
            for (int i = 1; i < times.Count; i++)
                if (Mathf.Abs((times[i] - times[i - 1]) - meanDelta) / meanDelta > 0.05f)
                    throw new FormatException("Sampling interval varies by more than 5%; resample first.");
            sampleRate = 1f / meanDelta;
            samples = values.ToArray();
        }

        private static float Number(string value) => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
#endif
