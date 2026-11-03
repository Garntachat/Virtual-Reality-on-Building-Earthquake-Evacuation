using UnityEngine;

namespace ChulaEarthquakeVR
{
    // Old saved scenes use this component on light fixtures and ceiling visuals.
    // Visual only; reads the SAME ground-motion wave as the simple Rigidbody system.
    // Never drives colliders, player, or camera.
    public sealed class VisualQuakeSway : MonoBehaviour
    {
        [SerializeField, Range(0f, 3f)] private float visualStrength = 1f;
        [Tooltip("Visual-only damping; higher values follow the shared earthquake more slowly.")]
        [SerializeField, Range(0f, 8f)] private float visualDamping = 1.2f;
        private GroundMotionPlayer motion;
        private Vector3 rest;
        private Quaternion rotation;
        private bool captured;

        private void Start()
        {
            motion = FindFirstObjectByType<GroundMotionPlayer>();
            rest = transform.localPosition;
            rotation = transform.localRotation;
            captured = true;
        }

        private void LateUpdate()
        {
            if (motion == null) return;
            Vector3 acceleration = motion.IsPlaying ? motion.CurrentFloorAccelerationMs2 : Vector3.zero;
            Vector3 wave = -acceleration /
                Mathf.Max(0.001f, 0.16f * Physics.gravity.magnitude);
            Vector3 local = transform.parent == null
                ? wave : transform.parent.InverseTransformDirection(wave);
            Vector3 target = rest + new Vector3(local.x, 0f, local.z) * (0.025f * visualStrength);
            Quaternion targetRotation = rotation *
                Quaternion.Euler(local.z * visualStrength * 2f, 0f, -local.x * visualStrength * 2f);
            float blend = Mathf.Clamp01(Time.deltaTime * 14f / (1f + visualDamping));
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, blend);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, blend);
        }

        private void OnDisable()
        {
            if (!captured) return;
            transform.localPosition = rest;
            transform.localRotation = rotation;
        }
    }
}
