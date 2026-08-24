using System;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    [CreateAssetMenu(fileName = "QuakeProfile", menuName = "CEVR/Quake Profile")]
    public sealed class QuakeProfile : ScriptableObject
    {
        [SerializeField] private string profileId = "recorded-motion";
        [SerializeField, Min(1f)] private float sampleRateHz = 100f;
        [SerializeField] private Vector3[] accelerationMs2 = Array.Empty<Vector3>();
        [SerializeField, TextArea] private string sourceNote = "Acceleration in m/s²; X/Z horizontal and Y vertical.";

        public string ProfileId => profileId;
        public float SampleRateHz => sampleRateHz;
        public int SampleCount => accelerationMs2?.Length ?? 0;
        public float DurationSeconds => SampleCount < 2 ? 0f : (SampleCount - 1) / sampleRateHz;
        public string SourceNote => sourceNote;

        public Vector3 Evaluate(float timeSeconds)
        {
            if (SampleCount == 0 || sampleRateHz <= 0f) return Vector3.zero;
            float position = Mathf.Clamp(timeSeconds, 0f, DurationSeconds) * sampleRateHz;
            int lower = Mathf.Clamp(Mathf.FloorToInt(position), 0, SampleCount - 1);
            int upper = Mathf.Min(lower + 1, SampleCount - 1);
            return Vector3.LerpUnclamped(accelerationMs2[lower], accelerationMs2[upper], position - lower);
        }

        public float PeakComponentAccelerationG()
        {
            float peak = 0f;
            for (int i = 0; i < SampleCount; i++)
            {
                Vector3 sample = accelerationMs2[i];
                peak = Mathf.Max(peak, Mathf.Abs(sample.x), Mathf.Abs(sample.y), Mathf.Abs(sample.z));
            }
            return peak / Physics.gravity.magnitude;
        }

        public float PeakVectorAccelerationG()
        {
            float peak = 0f;
            for (int i = 0; i < SampleCount; i++) peak = Mathf.Max(peak, accelerationMs2[i].magnitude);
            return peak / Physics.gravity.magnitude;
        }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(profileId)) return Fail("Profile ID is required.", out error);
            if (sampleRateHz <= 0f) return Fail("Sample rate must be greater than zero.", out error);
            if (SampleCount < 2) return Fail("At least two acceleration samples are required.", out error);
            for (int i = 0; i < SampleCount; i++)
            {
                Vector3 s = accelerationMs2[i];
                if (!Finite(s.x) || !Finite(s.y) || !Finite(s.z))
                    return Fail($"Acceleration sample {i} contains NaN or Infinity.", out error);
            }
            error = string.Empty;
            return true;
        }

        public void Initialize(string id, float rateHz, Vector3[] samples, string note = "")
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Profile ID is required.", nameof(id));
            if (rateHz <= 0f) throw new ArgumentOutOfRangeException(nameof(rateHz));
            if (samples == null || samples.Length < 2)
                throw new ArgumentException("At least two samples are required.", nameof(samples));
            for (int i = 0; i < samples.Length; i++)
                if (!Finite(samples[i].x) || !Finite(samples[i].y) || !Finite(samples[i].z))
                    throw new ArgumentException($"Acceleration sample {i} contains NaN or Infinity.", nameof(samples));
            profileId = id.Trim();
            sampleRateHz = rateHz;
            accelerationMs2 = (Vector3[])samples.Clone();
            sourceNote = note ?? string.Empty;
        }

        private static bool Fail(string message, out string error) { error = message; return false; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
