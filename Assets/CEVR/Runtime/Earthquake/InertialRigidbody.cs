using UnityEngine;

namespace ChulaEarthquakeVR
{
    // House-only saved response. Standard retains the existing Tutorial/Condo
    // behavior. These modes share GroundMotionPlayer and the editable Inspector.
    public enum HouseFurnitureMotionMode
    {
        Standard = 0,
        AnchoredSway = 1,
        GravityLoose = 2
    }

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

        [Header("Earthquake onset")]
        [Tooltip("For saved House furniture: keep the Rigidbody still in preparation, release it when the shared earthquake starts. Hazards use their own staggered release.")]
        [SerializeField] private bool wakeOnlyWhenEarthquakeStarts;

        [Header("Saved House furniture behaviour")]
        [Tooltip("Anchored Sway: heavy furniture rocks gently at its saved position. Gravity Loose: smaller objects fall and settle under gravity. Standard: chairs and other scenes.")]
        [SerializeField] private HouseFurnitureMotionMode houseMotionMode = HouseFurnitureMotionMode.Standard;
        [Tooltip("Maximum tilt in degrees for anchored large furniture. No displacement from the saved position.")]
        [SerializeField, Range(0f, 3f)] private float anchoredSwayDegrees = 1.35f;

        // Previous scenes already serialize responseScale / includeVertical; keep them intact.
        // Internal one-time marker: future Play sessions must never replace
        // a user's saved Mass, Damping, Force or Torque settings.
        [SerializeField, HideInInspector] private bool starterPresetProcessed;

        public bool StarterPresetProcessed => starterPresetProcessed;
        private Rigidbody body;
        private ToppleableFurniture topple;
        private GroundMotionPlayer subscribedMotion;
        private bool manualInteractionActive;
        private bool earthquakeHasStarted;
        private Vector3 anchoredPosition;
        private Quaternion anchoredRotation;

        public float ShakeStrength => responseScale;
        public float ForceNewtons => forceNewtons;
        public float TorqueNewtonMeters => torqueNewtonMeters;
        public HouseFurnitureMotionMode HouseMotionMode => houseMotionMode;

        public void ConnectMotionIfMissing(GroundMotionPlayer source)
        {
            if (motionSource == null) motionSource = source;
            SubscribeToQuake();
        }

        // Used by the one-time House editor bake. Does not touch Mass or Damping.
        public void ConfigureQuakeActivation(bool enabled)
        {
            wakeOnlyWhenEarthquakeStarts = enabled;
            SubscribeToQuake();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            topple = GetComponent<ToppleableFurniture>();
            if (motionSource == null) motionSource = FindFirstObjectByType<GroundMotionPlayer>();
            anchoredPosition = body == null ? transform.position : body.position;
            anchoredRotation = body == null ? transform.rotation : body.rotation;
            if (houseMotionMode == HouseFurnitureMotionMode.AnchoredSway && body != null)
            {
                // Kinematic, grounded cabinets and tables can rock but can NEVER
                // translate, topple, or freeze midair. Keep their exact saved pivot.
                body.isKinematic = true;
                body.useGravity = false;
            }
            // Saved quake-activated furniture must not fall or settle during the
            // preparation countdown. FallingHazard bodies are staged separately.
            if (wakeOnlyWhenEarthquakeStarts && body != null &&
                GetComponent<FallingHazard>() == null && !body.isKinematic)
                body.isKinematic = true;
            // DO NOT overwrite Rigidbody mass, linear/angular damping, constraints,
            // collision mode or interpolation. All remain editable in the Inspector.
        }

        private void Start()
        {
            if (motionSource == null) motionSource = FindFirstObjectByType<GroundMotionPlayer>();
            SubscribeToQuake();
        }

        private void OnEnable()
        {
            // OnDisable removes event handlers. A furniture component switched
            // back on during Play must reconnect to the SAME shared quake source.
            SubscribeToQuake();
        }

        private void SubscribeToQuake()
        {
            if (!Application.isPlaying || !wakeOnlyWhenEarthquakeStarts) return;
            if (motionSource == null) motionSource = FindFirstObjectByType<GroundMotionPlayer>();
            if (motionSource == subscribedMotion) return;
            if (subscribedMotion != null)
            {
                subscribedMotion.QuakeStarted -= OnQuakeStarted;
                subscribedMotion.QuakeEnded -= OnQuakeEnded;
            }
            subscribedMotion = motionSource;
            if (subscribedMotion == null) return;
            subscribedMotion.QuakeStarted += OnQuakeStarted;
            subscribedMotion.QuakeEnded += OnQuakeEnded;
            if (subscribedMotion.IsPlaying) OnQuakeStarted();
        }

        private void OnQuakeStarted()
        {
            if (!wakeOnlyWhenEarthquakeStarts || body == null ||
                GetComponent<FallingHazard>() != null) return;
            earthquakeHasStarted = true;
            if (houseMotionMode == HouseFurnitureMotionMode.AnchoredSway) return;
            // Only physical loose objects/chairs are released. Gravity remains
            // enabled AFTER the wave ends, so nothing freezes while airborne.
            if (houseMotionMode == HouseFurnitureMotionMode.GravityLoose ||
                GetComponent<MovableFurniture>() != null)
                body.useGravity = true;
            body.isKinematic = false;
        }

        private void OnQuakeEnded()
        {
            if (!wakeOnlyWhenEarthquakeStarts || body == null ||
                manualInteractionActive || GetComponent<FallingHazard>() != null) return;
            if (houseMotionMode == HouseFurnitureMotionMode.AnchoredSway)
            {
                body.MovePosition(anchoredPosition);
                body.MoveRotation(anchoredRotation);
                return;
            }
            if (houseMotionMode == HouseFurnitureMotionMode.GravityLoose ||
                GetComponent<MovableFurniture>() != null)
            {
                // Physical objects must finish falling and settle on the real
                // floor/table support, even after sound and shaking have ended.
                body.useGravity = true;
                body.isKinematic = false;
                return;
            }
            // Backwards compatible for other scenes' ordinary non-loose props.
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
        }

        /// <summary>
        /// Let the participant deliberately slide a pre-quake chair while it is
        /// otherwise parked. Only the player interaction can unfreeze it.
        /// All settings and the last manually selected position are preserved.
        /// </summary>
        public bool BeginManualInteraction()
        {
            if (!wakeOnlyWhenEarthquakeStarts ||
                GetComponent<MovableFurniture>() == null) return false;
            if (body == null) body = GetComponent<Rigidbody>();
            if (body == null || GetComponent<FallingHazard>() != null) return false;
            manualInteractionActive = true;
            body.isKinematic = false;
            return true;
        }

        public void EndManualInteraction()
        {
            if (!manualInteractionActive) return;
            manualInteractionActive = false;
            if (body == null) body = GetComponent<Rigidbody>();
            if (body == null || (motionSource != null && motionSource.IsPlaying)) return;
            if (earthquakeHasStarted)
            {
                // Post-quake chair release still falls/settles under gravity.
                // Only PRE-quake chairs are parked when the user lets go.
                body.useGravity = true;
                body.isKinematic = false;
                return;
            }
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        private void OnDisable()
        {
            if (subscribedMotion != null)
            {
                subscribedMotion.QuakeStarted -= OnQuakeStarted;
                subscribedMotion.QuakeEnded -= OnQuakeEnded;
                subscribedMotion = null;
            }
        }

        private void FixedUpdate()
        {
            if (houseMotionMode == HouseFurnitureMotionMode.AnchoredSway)
            {
                UpdateAnchoredSway();
                return;
            }
            if (motionSource == null || !motionSource.IsPlaying) return;

            // The global motion still drives the exact same 20-second quake,
            // warning sounds, hazard release and safety instructions.
            // Normalize against the default 0.16 g training preview so 15 N
            // gives a familiar starting shake; stronger recorded motions scale up.
            float referenceMs2 = 0.16f * Physics.gravity.magnitude;
            Vector3 wave = -motionSource.CurrentFloorAccelerationMs2 / Mathf.Max(0.001f, referenceMs2);
            ApplySimpleWave(wave);
        }

        private void UpdateAnchoredSway()
        {
            if (body == null || !body.isKinematic) return;
            // Only rotate in place. Rigidbody.MovePosition keeps the saved
            // pivot fixed, including during collisions and after the quake.
            body.MovePosition(anchoredPosition);
            if (motionSource == null || !motionSource.IsPlaying ||
                responseScale <= 0f || anchoredSwayDegrees <= 0f)
            {
                body.MoveRotation(anchoredRotation);
                return;
            }

            float reference = Mathf.Max(0.001f, 0.16f * Physics.gravity.magnitude);
            Vector3 wave = -motionSource.CurrentFloorAccelerationMs2 / reference;
            float pitch = Mathf.Clamp(wave.z * responseScale, -1f, 1f) * anchoredSwayDegrees;
            float roll = Mathf.Clamp(-wave.x * responseScale, -1f, 1f) * anchoredSwayDegrees;
            Quaternion target = anchoredRotation * Quaternion.Euler(pitch, 0f, roll);
            body.MoveRotation(Quaternion.Slerp(body.rotation, target,
                Mathf.Clamp01(Time.fixedDeltaTime * 12f)));
        }

        public void ApplySimpleWave(Vector3 wave, float forceMultiplier = 1f, float torqueMultiplier = 1f)
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (topple == null) topple = GetComponent<ToppleableFurniture>();
            if (body == null || body.isKinematic || responseScale <= 0f ||
                houseMotionMode == HouseFurnitureMotionMode.AnchoredSway) return;
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
            SubscribeToQuake();
            responseScale = Mathf.Clamp(scale, 0f, 3f);
            includeVertical = vertical;
        }
    }
}
