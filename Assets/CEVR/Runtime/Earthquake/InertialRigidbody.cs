using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(-100)]
    public sealed class InertialRigidbody : MonoBehaviour
    {
        [SerializeField] private GroundMotionPlayer motionSource;
        [SerializeField, Min(0f)] private float responseScale = 1f;
        [SerializeField] private bool includeVertical = true;
        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
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
            responseScale = Mathf.Max(0f, scale);
            includeVertical = vertical;
        }
    }
}
