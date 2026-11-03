using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(-100)]
    public sealed class InertialRigidbody : MonoBehaviour
    {
        [SerializeField] private GroundMotionPlayer motionSource;
        [Header("Simple earthquake settings (per item)")]
        [Tooltip("0 = no shaking; 1 = normal; 2 = twice as much. Does not change the global earthquake.")]
        [SerializeField, Range(0f, 3f)] private float responseScale = 1f;
        [Tooltip("Off = horizontal shaking only. Keep off for chairs and heavy furniture.")]
        [SerializeField] private bool includeVertical = true;
        private Rigidbody body;

        public float ShakeStrength => responseScale;

        public void ConnectMotionIfMissing(GroundMotionPlayer source)
        {
            if (motionSource == null) motionSource = source;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (motionSource == null) motionSource = FindFirstObjectByType<GroundMotionPlayer>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void FixedUpdate()
        {
            if (motionSource == null || !motionSource.IsPlaying || body.isKinematic) return;
            Vector3 acceleration = motionSource.CurrentFloorAccelerationMs2;
            if (!includeVertical) acceleration.y = 0f;
            body.AddForce(-acceleration * responseScale, ForceMode.Acceleration);
        }

        public void Configure(GroundMotionPlayer source, float scale = 1f, bool vertical = true)
        {
            motionSource = source;
            responseScale = Mathf.Clamp(scale, 0f, 3f);
            includeVertical = vertical;
        }
    }
}
