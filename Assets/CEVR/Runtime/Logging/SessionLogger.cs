using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    public sealed class SessionLogger : MonoBehaviour
    {
        [Serializable]
        private sealed class TelemetryEvent
        {
            public string timestampUtc;
            public float sessionTimeSeconds;
            public string participantCode;
            public string scenarioId;
            public string buildVersion;
            public string eventType;
            public string payloadJson;
        }

        private StreamWriter writer;
        private float sessionStartRealtime;

        public bool IsSessionOpen => writer != null;
        public string CurrentLogPath { get; private set; } = string.Empty;
        public string ParticipantCode { get; private set; } = "UNSET";
        public string ScenarioId { get; private set; } = "UNSET";

        public void BeginSession(string participantCode, string scenarioId, StudyMode mode)
        {
            EndSession();
            ParticipantCode = Sanitize(participantCode, "TUTORIAL");
            ScenarioId = Sanitize(scenarioId, "SCENARIO_UNSET");
            string directory = Path.Combine(Application.persistentDataPath, "CEVRLogs");
            Directory.CreateDirectory(directory);
            CurrentLogPath = UniquePath(directory, ParticipantCode, ScenarioId);
            var stream = new FileStream(CurrentLogPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            sessionStartRealtime = Time.realtimeSinceStartup;
            LogEvent("session_started", $"{{\"mode\":\"{mode}\",\"platform\":\"{Application.platform}\"}}");
        }

        public void LogEvent(string eventType, string payloadJson = "{}")
        {
            if (writer == null) return;
            var item = new TelemetryEvent
            {
                timestampUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                sessionTimeSeconds = Time.realtimeSinceStartup - sessionStartRealtime,
                participantCode = ParticipantCode,
                scenarioId = ScenarioId,
                buildVersion = Application.version,
                eventType = string.IsNullOrWhiteSpace(eventType) ? "unspecified" : eventType,
                payloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson
            };
            writer.WriteLine(JsonUtility.ToJson(item));
        }

        public void EndSession()
        {
            if (writer == null) return;
            LogEvent("session_ended");
            writer.Dispose();
            writer = null;
        }

        private void OnApplicationQuit() => EndSession();
        private void OnDestroy() => EndSession();

        private static string UniquePath(string directory, string participant, string scenario)
        {
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            for (int i = 0; i < 1000; i++)
            {
                string suffix = i == 0 ? string.Empty : $"_{i:D3}";
                string path = Path.Combine(directory, $"{participant}_{scenario}_{stamp}{suffix}.jsonl");
                if (!File.Exists(path)) return path;
            }
            throw new IOException("Could not allocate a unique CEVR log filename.");
        }

        private static string Sanitize(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var result = new StringBuilder();
            foreach (char c in value.Trim())
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') result.Append(c);
            return result.Length == 0 ? fallback : result.ToString();
        }
    }
}
