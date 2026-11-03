using UnityEngine;

namespace ChulaEarthquakeVR
{
    // ONE physics shaker per object. The same simple force/torque equations
    // apply in Tutorial, House, Condo and manually triggered legacy scenes.
    // Rigidbody Mass and Damping are never changed by this script.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(-100)]
    public sealed class InertialRigidbody : MonoBehaviour
    {
        [SerializeField] private GroundMotionPlayer motionSource;

        [Header("Per-Object Earthquake (Unity Physics)")]
        [Tooltip("0 = no shaking, 1 = normal. Multiplies force and torque for this object only.")]
        [SerializeField, Range(0f, 3f)] private float responseScale = 1f;
        [Tooltip("Linear force in Newtons at normal earthquake intensity. Mass and Linear Damping determine motion.")]
        [SerializeField, Min(0f)] private float forceNewtons = 15f;
        [Tooltip("Turning force in Newton-meters. Mass, geometry and Angular Damping determine rotation.")]
        [SerializeField, Min(0f)] private float torqueNewtonMeters = 5f;
        [Tooltip("Off for chairs and heavy furniture. On allows a small up/down component.")]
        [SerializeField] private bool includeVertical = true;

        // Previous scenes already serialize responseScale / includeVertical; keep them intact.
        // Internal one-time marker: future Play sessions must never replace
        // a user's saved Mass, Damping, Force or Torque settings.
        [SerializeField, HideInInspector] private bool starterPresetProcessed;

        public bool StarterPresetProcessed => starterPresetProcessed;
        private Rigidbody body;
        private ToppleableFurniture topple;

        public float ShakeStrength => responseScale;
        public float ForceNewtons => forceNewtons;
        public float TorqueNewtonMeters => torqueNewtonMeters;

        public void ConnectMotionIfMissing(GroundMotionPlayer source)
        {
            if (motionSource == null) motionSource = source;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            topple = GetComponent<ToppleableFurniture>();
            if (motionSource == null) motionSource = FindFirstObjectByType<GroundMotionPlayer>();
            // DO NOT overwrite Rigidbody mass, linear/angular damping, constraints,
            // collision mode or interpolation. All remain editable in the Inspector.
        }

        private void FixedUpdate()
        {
            if (motionSource == null || !motionSource.IsPlaying) return;

            // The global motion still drives the exact same 20-second quake,
            // warning sounds, hazard release and safety instructions.
            // Normalize against the default 0.16 g training preview so 15 N
            // gives a familiar starting shake; stronger recorded motions scale up.
            float referenceMs2 = 0.16f * Physics.gravity.magnitude;
            Vector3 wave = -motionSource.CurrentFloorAccelerationMs2 / Mathf.Max(0.001f, referenceMs2);
            ApplySimpleWave(wave);
        }

        public void ApplySimpleWave(Vector3 wave, float forceMultiplier = 1f, float torqueMultiplier = 1f)
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (topple == null) topple = GetComponent<ToppleableFurniture>();
            if (body == null || body.isKinematic || responseScale <= 0f) return;
            if (topple != null && topple.AnchoredToStructure) return;
            if (!includeVertical) wave.y = 0f;

            // ForceMode.Force is the important difference from the old system:
            // a heavier Rigidbody accelerates less under the same number of Newtons.
            Vector3 force = wave * (forceNewtons * responseScale * Mathf.Max(0f, forceMultiplier));
            Vector3 torque = new Vector3(wave.z, 0f, -wave.x) *
                             (torqueNewtonMeters * responseScale * Mathf.Max(0f, torqueMultiplier));

            if (topple != null)
                body.AddForceAtPosition(force * topple.UpperForceScale,
                    body.worldCenterOfMass + Vector3.up * topple.ForceHeight, ForceMode.Force);
            else
                body.AddForce(force, ForceMode.Force);
            body.AddTorque(torque, ForceMode.Force);
        }

        // Only called from the saved House proxy-settings bridge. No additional
        // force source or Rigidbody is created.
        public void ConfigureForces(float newtons, float torque)
        {
            forceNewtons = Mathf.Max(0f, newtons);
            torqueNewtonMeters = Mathf.Max(0f, torque);
        }

        public void Configure(GroundMotionPlayer source, float scale = 1f, bool vertical = true)
        {
            motionSource = source;
            responseScale = Mathf.Clamp(scale, 0f, 3f);
            includeVertical = vertical;
        }
    }
}
