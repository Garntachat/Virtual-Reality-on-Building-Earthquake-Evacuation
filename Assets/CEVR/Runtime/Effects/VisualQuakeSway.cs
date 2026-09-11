using UnityEngine;

namespace ChulaEarthquakeVR
{
    // Visual-only motion: the tracked camera and architectural collision remain stable.
    public sealed class VisualQuakeSway : MonoBehaviour
    {
        private GroundMotionPlayer motion;
        private Vector3 rest;
        private Quaternion rotation;
        private Vector3 smoothedOffset;
        private float smoothedRoll;
        private void Start()
        {
            motion = FindFirstObjectByType<GroundMotionPlayer>();
            rest = transform.localPosition;
            rotation = transform.localRotation;
        }
        private void LateUpdate()
        {
            float strength = motion != null && motion.IsPlaying ? motion.PresentationIntensity : 0f;
            float phase = motion == null ? 0f : (motion.ElapsedSeconds + Mathf.Max(0f, Time.time - Time.fixedTime)) * 8f;
            Vector3 offset = new Vector3(Mathf.Sin(phase), 0f, Mathf.Cos(phase * 0.83f)) * (0.03f * strength);
            float blend = 1f - Mathf.Exp(-20f * Time.deltaTime);
            smoothedOffset = Vector3.Lerp(smoothedOffset, offset, blend);
            smoothedRoll = Mathf.Lerp(smoothedRoll, Mathf.Sin(phase) * strength * 0.45f, blend);
            transform.localPosition = rest + (transform.parent == null ? smoothedOffset : transform.parent.InverseTransformVector(smoothedOffset));
            transform.localRotation = rotation * Quaternion.Euler(0f, 0f, smoothedRoll);
        }
    }
}
