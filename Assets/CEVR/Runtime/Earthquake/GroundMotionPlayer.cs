using System;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DefaultExecutionOrder(-200)]
    public sealed class GroundMotionPlayer : MonoBehaviour
    {
        [Header("Recorded research profile")]
        [SerializeField] private QuakeProfile quakeProfile;
        [SerializeField] private FloorResponseProfile floorResponseProfile;
        [SerializeField] private Vector3 axisScale = Vector3.one;

        [Header("Deterministic tutorial preview — never research data")]
        [SerializeField, Min(1f)] private float previewDurationSeconds = 20f;
        [SerializeField, Range(0.01f, 0.5f)] private float previewPeakG = 0.16f;
        [SerializeField, Min(0.1f)] private float previewFrequencyHz = 2.1f;
        [SerializeField] private int previewSeed = 20260824;

        private FloorResponseState floorState;
        private float elapsed;
        private float intensityReferenceMs2 = 1f;

        public bool IsPlaying { get; private set; }
        public bool IsUsingPreview => quakeProfile == null;
        public string ActiveProfileId => quakeProfile == null
            ? $"tutorial-preview-seed-{previewSeed}"
            : quakeProfile.ProfileId;
        public string ActiveFloorResponseId => floorResponseProfile == null
            ? "none"
            : floorResponseProfile.ProfileId;
        public float ElapsedSeconds => elapsed;
        public float DurationSeconds => quakeProfile == null ? previewDurationSeconds : quakeProfile.DurationSeconds;
        public Vector3 CurrentGroundAccelerationMs2 { get; private set; }
        public Vector3 CurrentFloorAccelerationMs2 { get; private set; }
        public float NormalizedIntensity => Mathf.Clamp01(CurrentFloorAccelerationMs2.magnitude /
            Mathf.Max(0.001f, intensityReferenceMs2));

        public event Action QuakeStarted;
        public event Action QuakeEnded;

        public void ConfigureRecordedProfile(QuakeProfile motion, FloorResponseProfile floorResponse)
        {
            if (IsPlaying) throw new InvalidOperationException("Stop the quake before changing profiles.");
            quakeProfile = motion;
            floorResponseProfile = floorResponse;
        }

        public void ConfigurePreview(float durationSeconds, float peakG, float frequencyHz, int seed)
        {
            if (IsPlaying) throw new InvalidOperationException("Stop the quake before changing preview parameters.");
            quakeProfile = null;
            floorResponseProfile = null;
            previewDurationSeconds = Mathf.Max(1f, durationSeconds);
            previewPeakG = Mathf.Clamp(peakG, 0.01f, 0.5f);
            previewFrequencyHz = Mathf.Max(0.1f, frequencyHz);
            previewSeed = seed;
        }

        public void StartQuake()
        {
            if (quakeProfile != null && !quakeProfile.IsValid(out string error))
                throw new InvalidOperationException($"Cannot play invalid quake profile: {error}");
            elapsed = 0f;
            floorState.Reset();
            float peakG = quakeProfile == null ? previewPeakG : quakeProfile.PeakVectorAccelerationG();
            float axis = Mathf.Max(Mathf.Abs(axisScale.x), Mathf.Abs(axisScale.y), Mathf.Abs(axisScale.z));
            intensityReferenceMs2 = Mathf.Max(0.001f, peakG * axis * Physics.gravity.magnitude);
            CurrentGroundAccelerationMs2 = Vector3.zero;
            CurrentFloorAccelerationMs2 = Vector3.zero;
            IsPlaying = true;
            QuakeStarted?.Invoke();
        }

        public void StopQuake()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            CurrentGroundAccelerationMs2 = Vector3.zero;
            CurrentFloorAccelerationMs2 = Vector3.zero;
            QuakeEnded?.Invoke();
        }

        private void FixedUpdate()
        {
            if (!IsPlaying) return;
            float dt = Time.fixedDeltaTime;
            CurrentGroundAccelerationMs2 = Vector3.Scale(EvaluateGround(elapsed), axisScale);
            CurrentFloorAccelerationMs2 = floorResponseProfile == null
                ? CurrentGroundAccelerationMs2
                : floorResponseProfile.Step(CurrentGroundAccelerationMs2, dt, ref floorState);
            elapsed += dt;
            if (elapsed >= DurationSeconds) StopQuake();
        }

        private Vector3 EvaluateGround(float time)
        {
            if (quakeProfile != null) return quakeProfile.Evaluate(time);
            float duration = Mathf.Max(1f, previewDurationSeconds);
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(time / duration));
            float seedOffset = previewSeed * 0.001f;
            float xNoise = Mathf.PerlinNoise(time * 1.7f, seedOffset) * 2f - 1f;
            float zNoise = Mathf.PerlinNoise(time * 1.3f, seedOffset + 17.3f) * 2f - 1f;
            float yNoise = Mathf.PerlinNoise(time * 2.1f, seedOffset + 31.7f) * 2f - 1f;
            float omegaT = 2f * Mathf.PI * previewFrequencyHz * time;
            float peakMs2 = previewPeakG * Physics.gravity.magnitude;
            return new Vector3(
                Mathf.Sin(omegaT) * (0.65f + 0.35f * xNoise),
                0.20f * Mathf.Sin(omegaT * 1.9f) * yNoise,
                Mathf.Cos(omegaT * 0.83f) * (0.65f + 0.35f * zNoise)) * peakMs2 * envelope;
        }
    }
}
