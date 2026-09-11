using UnityEngine;

namespace ChulaEarthquakeVR
{
    // Visual-only motion: the tracked camera and architectural collision remain stable.
    public sealed class VisualQuakeSway : MonoBehaviour
    {
        private GroundMotionPlayer motion;
        private Vector3 rest;
        private Quaternion rotation;
        private void Start()
        {
            motion = FindFirstObjectByType<GroundMotionPlayer>();
            rest = transform.localPosition;
            rotation = transform.localRotation;
        }
        private void LateUpdate()
        {
            float strength = motion != null && motion.IsPlaying ? motion.NormalizedIntensity : 0f;
            float phase = motion == null ? 0f : motion.ElapsedSeconds * 8f;
            Vector3 offset = new Vector3(Mathf.Sin(phase), 0f, Mathf.Cos(phase * 0.83f)) * (0.012f * strength);
            transform.localPosition = rest + (transform.parent == null ? offset : transform.parent.InverseTransformVector(offset));
            transform.localRotation = rotation * Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * strength * 0.35f);
        }
    }
}
