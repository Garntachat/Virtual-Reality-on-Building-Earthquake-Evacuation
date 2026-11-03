using UnityEngine;

namespace ChulaEarthquakeVR
{
    // Only used for objects WITHOUT a Rigidbody: decorative visual sway,
    // never collision / mass / physics. Physical furniture uses InertialRigidbody.
    public enum DecorativeQuakeMode { Loose, Rocking, Hanging, Mounted, Heavy }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(250)]
    public sealed class DecorativeQuakeResponse : MonoBehaviour
    {
        [SerializeField] private GroundMotionPlayer motion;
        [SerializeField] private DecorativeQuakeMode responseMode = DecorativeQuakeMode.Loose;
        [Header("Visual only - no Rigidbody")]
        [Tooltip("0 = still, 1 = normal sway, 2 = double. Decorative objects have no Mass or Damping.")]
        [SerializeField, Range(0f, 3f)] private float responseScale = 1f;

        [Header("Visual response (not Rigidbody physics)")]
        [Tooltip("Higher values settle into the motion more gradually. This is visual damping, not physical mass.")]
        [SerializeField, Range(0f, 8f)] private float visualDamping = 0f;

        [SerializeField, HideInInspector] private bool starterPresetProcessed;
        public float VisualDamping => visualDamping;
        public bool StarterPresetProcessed => starterPresetProcessed;

        private Vector3 restPosition;
        private Quaternion restRotation;
        private bool captured;

        public float ShakeStrength => responseScale;

        public void ConnectMotionIfMissing(GroundMotionPlayer source)
        {
            if (motion == null) motion = source;
        }

        public void Configure(GroundMotionPlayer source, DecorativeQuakeMode mode, float scale = 1f)
        {
            motion = source;
            responseMode = mode;
            responseScale = Mathf.Clamp(scale, 0f, 3f);
            CaptureRest();
        }

        private void Awake()
        {
            CaptureRest();
            if (motion == null) motion = FindFirstObjectByType<GroundMotionPlayer>();
        }

        private void CaptureRest()
        {
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            captured = true;
        }

        private void LateUpdate()
        {
            if (!captured) CaptureRest();
            if (motion == null || !motion.IsPlaying || responseScale <= 0f)
            {
                ReturnToRest();
                return;
            }

            // SAME shared wave as physical furniture, but only a tiny transform
            // offset on non-physics art. No random phase or hidden oscillators.
            Vector3 wave = -motion.CurrentFloorAccelerationMs2 /
                Mathf.Max(0.001f, 0.16f * Physics.gravity.magnitude);
            Vector3 localWave = transform.parent == null
                ? wave : transform.parent.InverseTransformDirection(wave);
            float sway = responseScale * ModeStrength();
            Vector3 offset = new Vector3(localWave.x, 0f, localWave.z) * (0.025f * sway);
            Quaternion tilt = Quaternion.Euler(localWave.z * 5f * sway, 0f,
                -localWave.x * 5f * sway);
            float blend = Mathf.Clamp01(Time.deltaTime * 14f / (1f + visualDamping));
            transform.localPosition = Vector3.Lerp(transform.localPosition, restPosition + offset, blend);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, restRotation * tilt, blend);
        }

        private float ModeStrength()
        {
            switch (responseMode)
            {
                case DecorativeQuakeMode.Hanging: return 1.4f;
                case DecorativeQuakeMode.Rocking: return 1.1f;
                case DecorativeQuakeMode.Mounted: return 0.25f;
                case DecorativeQuakeMode.Heavy: return 0.35f;
                default: return 1f;
            }
        }

        private void ReturnToRest()
        {
            if (!captured) return;
            float blend = Mathf.Clamp01(Time.deltaTime * 14f / (1f + visualDamping));
            transform.localPosition = Vector3.Lerp(transform.localPosition, restPosition, blend);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, restRotation, blend);
        }

        private void OnDisable()
        {
            if (!captured) return;
            transform.localPosition = restPosition;
            transform.localRotation = restRotation;
        }
    }
}
