using UnityEngine;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Saved Inspector values for House cabinets and overhead hazards that still
    /// use a hidden, collision-safe gameplay Rigidbody. This is not a second
    /// shaker: the existing proxy's InertialRigidbody receives these exact values.
    /// The visible House object keeps its authored transform and detailed mesh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HouseProxyEarthquakeSettings : MonoBehaviour
    {
        [Header("Physics of this House furniture item")]
        [Tooltip("Mass of the playable Rigidbody in kilograms.")]
        [SerializeField, Min(0.01f)] private float mass = 34f;
        [Tooltip("Slows sideways motion. Applied to the gameplay Rigidbody.")]
        [SerializeField, Min(0f)] private float linearDamping = 0f;
        [Tooltip("Slows spinning and tipping. Applied to the gameplay Rigidbody.")]
        [SerializeField, Min(0f)] private float angularDamping = 0.05f;
        [Tooltip("Force applied by the shared earthquake in Newtons.")]
        [SerializeField, Min(0f)] private float forceNewtons = 15f;
        [Tooltip("Rotational force in Newton-metres.")]
        [SerializeField, Min(0f)] private float torqueNewtonMeters = 5f;
        [Tooltip("0 = no vibration, 1 = normal, 2 = double force.")]
        [SerializeField, Range(0f, 3f)] private float shakeStrength = 1f;
        [Tooltip("Overhead loose items can move up/down; cabinets generally shake sideways.")]
        [SerializeField] private bool includeVertical;

        public float Mass => mass;
        public float LinearDamping => linearDamping;
        public float AngularDamping => angularDamping;
        public float ForceNewtons => forceNewtons;
        public float TorqueNewtonMeters => torqueNewtonMeters;
        public float ShakeStrength => shakeStrength;

        public void SetDefaultsForNewComponent(bool overhead)
        {
            // Called exactly once when this script is newly authored in Edit mode.
            mass = overhead ? 2.2f : 34f;
            linearDamping = 0f;
            angularDamping = 0.05f;
            forceNewtons = 15f;
            torqueNewtonMeters = 5f;
            shakeStrength = 1f;
            includeVertical = overhead;
        }

        public void ApplyToExistingProxy(GameObject proxy, GroundMotionPlayer motion)
        {
            if (proxy == null) return;
            Rigidbody body = proxy.GetComponent<Rigidbody>();
            if (body == null)
            {
                Debug.LogError("CEVR House: expected Rigidbody on proxy " + proxy.name, proxy);
                return;
            }

            body.mass = Mathf.Max(0.01f, mass);
            body.linearDamping = Mathf.Max(0f, linearDamping);
            body.angularDamping = Mathf.Max(0f, angularDamping);

            InertialRigidbody physics = proxy.GetComponent<InertialRigidbody>();
            if (physics == null) physics = proxy.AddComponent<InertialRigidbody>();
            physics.Configure(motion, shakeStrength, includeVertical);
            physics.ConfigureForces(forceNewtons, torqueNewtonMeters);
        }
    }
}
