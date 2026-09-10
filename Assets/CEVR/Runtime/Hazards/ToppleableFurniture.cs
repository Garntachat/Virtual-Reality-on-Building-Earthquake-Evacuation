using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ToppleableFurniture : MonoBehaviour
    {
        [SerializeField] private GroundMotionPlayer motion;
        [SerializeField, Min(0f)] private float upperForceScale = 1.35f;
        [SerializeField, Min(0.1f)] private float forceHeight = 1.4f;
        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (motion == null) motion = FindFirstObjectByType<GroundMotionPlayer>();
        }

        private void FixedUpdate()
        {
            if (motion == null || !motion.IsPlaying || body.isKinematic) return;
            Vector3 acceleration = motion.CurrentFloorAccelerationMs2;
            acceleration.y = 0f;
            Vector3 applicationPoint = body.worldCenterOfMass + Vector3.up * forceHeight;
            body.AddForceAtPosition(-acceleration * upperForceScale, applicationPoint, ForceMode.Acceleration);
        }

        public void Configure(GroundMotionPlayer source, float scale, float height)
        {
            motion = source;
            upperForceScale = Mathf.Max(0f, scale);
            forceHeight = Mathf.Max(0.1f, height);
        }
    }
}
