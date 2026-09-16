using UnityEngine;

namespace ChulaEarthquakeVR
{
    public enum DecorativeQuakeMode
    {
        Loose,
        Rocking,
        Hanging,
        Mounted,
        Heavy
    }

    /// <summary>
    /// Deterministic visual response for scene dressing that is not a physics object.
    /// It never moves the camera, player rig, floor, or safety-zone collision.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(250)]
    public sealed class DecorativeQuakeResponse : MonoBehaviour
    {
        [SerializeField] private GroundMotionPlayer motion;
        [SerializeField] private DecorativeQuakeMode responseMode = DecorativeQuakeMode.Loose;
        [SerializeField, Range(0.5f, 2f)] private float responseScale = 1f;

        private Vector3 restLocalPosition;
        private Quaternion restLocalRotation;
        private Vector3 smoothedOffset;
        private Vector3 smoothedEuler;
        private float deterministicPhase;
        private bool captured;

        private void Awake()
        {
            CaptureRestPose();
            if (motion == null) motion = FindFirstObjectByType<GroundMotionPlayer>();
        }

        public void Configure(
            GroundMotionPlayer source, DecorativeQuakeMode mode, float scale = 1f)
        {
            motion = source;
            responseMode = mode;
            responseScale = Mathf.Clamp(scale, 0.5f, 2f);
            CaptureRestPose();
        }

        private void CaptureRestPose()
        {
            restLocalPosition = transform.localPosition;
            restLocalRotation = transform.localRotation;
            deterministicPhase = StablePhase(HierarchyPath(transform));
            captured = true;
        }

        private void LateUpdate()
        {
            if (!captured) CaptureRestPose();
            bool active = motion != null && motion.IsPlaying;
            float intensity = active ? motion.PresentationIntensity : 0f;
            Vector3 acceleration = active ? motion.CurrentFloorAccelerationMs2 : Vector3.zero;
            Vector3 localAcceleration = transform.parent == null
                ? acceleration
                : transform.parent.InverseTransformDirection(acceleration);
            Vector3 normalized = localAcceleration / Mathf.Max(0.001f, Physics.gravity.magnitude);

            float time = motion == null
                ? 0f
                : motion.ElapsedSeconds + Mathf.Max(0f, Time.time - Time.fixedTime);
            float resonance = Mathf.Sin(time * ResonanceFrequency() * Mathf.PI * 2f + deterministicPhase)
                              * intensity;

            Vector3 targetOffset = TargetOffset(normalized, resonance) * responseScale;
            Vector3 targetEuler = TargetEuler(normalized, resonance) * responseScale;
            float blend = 1f - Mathf.Exp(-(active ? 18f : 9f) * Time.deltaTime);
            smoothedOffset = Vector3.Lerp(smoothedOffset, targetOffset, blend);
            smoothedEuler = Vector3.Lerp(smoothedEuler, targetEuler, blend);

            transform.localPosition = restLocalPosition + smoothedOffset;
            transform.localRotation = restLocalRotation * Quaternion.Euler(smoothedEuler);
        }

        private Vector3 TargetOffset(Vector3 accelerationG, float resonance)
        {
            switch (responseMode)
            {
                case DecorativeQuakeMode.Loose:
                    return new Vector3(-accelerationG.x * 0.075f, Mathf.Abs(accelerationG.y) * 0.018f,
                        -accelerationG.z * 0.075f) + new Vector3(resonance, 0f, -resonance) * 0.004f;
                case DecorativeQuakeMode.Hanging:
                    return new Vector3(-accelerationG.x * 0.025f, 0f, -accelerationG.z * 0.025f);
                case DecorativeQuakeMode.Mounted:
                    return new Vector3(-accelerationG.x, 0f, -accelerationG.z) * 0.012f;
                case DecorativeQuakeMode.Heavy:
                    return new Vector3(-accelerationG.x, 0f, -accelerationG.z) * 0.018f;
                default:
                    return new Vector3(-accelerationG.x, 0f, -accelerationG.z) * 0.028f;
            }
        }

        private Vector3 TargetEuler(Vector3 accelerationG, float resonance)
        {
            switch (responseMode)
            {
                case DecorativeQuakeMode.Hanging:
                    return new Vector3(accelerationG.z * 55f + resonance * 2.5f, 0f,
                        -accelerationG.x * 55f + resonance * 1.8f);
                case DecorativeQuakeMode.Rocking:
                    return new Vector3(accelerationG.z * 30f + resonance * 0.8f, 0f,
                        -accelerationG.x * 30f);
                case DecorativeQuakeMode.Mounted:
                    return new Vector3(accelerationG.z * 3f, 0f, -accelerationG.x * 3f);
                case DecorativeQuakeMode.Heavy:
                    return new Vector3(accelerationG.z * 7f, 0f, -accelerationG.x * 7f);
                default:
                    return new Vector3(accelerationG.z * 11f, resonance * 0.5f,
                        -accelerationG.x * 11f);
            }
        }

        private float ResonanceFrequency()
        {
            switch (responseMode)
            {
                case DecorativeQuakeMode.Hanging: return 0.9f;
                case DecorativeQuakeMode.Rocking: return 1.4f;
                case DecorativeQuakeMode.Heavy: return 1.1f;
                case DecorativeQuakeMode.Mounted: return 2.8f;
                default: return 3.2f;
            }
        }

        private void OnDisable()
        {
            if (!captured) return;
            transform.localPosition = restLocalPosition;
            transform.localRotation = restLocalRotation;
            smoothedOffset = Vector3.zero;
            smoothedEuler = Vector3.zero;
        }

        private static float StablePhase(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < value.Length; i++)
                    hash = (hash ^ value[i]) * 16777619u;
                return (hash % 10000) / 10000f * Mathf.PI * 2f;
            }
        }

        private static string HierarchyPath(Transform current)
        {
            string result = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                result = current.name + "/" + result;
            }
            return result;
        }
    }
}
