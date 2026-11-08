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

        [Header("Earthquake onset")]
        [Tooltip("For saved House furniture: keep the Rigidbody still in preparation, release it when the shared earthquake starts. Hazards use their own staggered release.")]
        [SerializeField] private bool wakeOnlyWhenEarthquakeStarts;

        // Previous scenes already serialize responseScale / includeVertical; keep them intact.
        // Internal one-time marker: future Play sessions must never replace
        // a user's saved Mass, Damping, Force or Torque settings.
        [SerializeField, HideInInspector] private bool starterPresetProcessed;

        public bool StarterPresetProcessed => starterPresetProcessed;
        private Rigidbody body;
        private ToppleableFurniture topple;
        private GroundMotionPlayer subscribedMotion;
        private bool manualInteractionActive;

        // House-only safety: heavy furniture stays at its saved position;
        // small, loose furniture is allowed to fall and settle under gravity.
        private bool anchoredHouseFurniture;
        private bool looseHouseFurniture;
        private Vector3 houseAnchorPosition;
        private Quaternion houseAnchorRotation;

        // Tutorial-only: large teaching-room furniture stays upright and in
        // place; loose props are released with gravity and never frozen midair.
        private bool anchoredTutorialFurniture;
        private bool looseTutorialFurniture;
        private Vector3 tutorialAnchorPosition;
        private Quaternion tutorialAnchorRotation;
        public bool IsEarthquakeAnchored => anchoredHouseFurniture || anchoredTutorialFurniture;

        public float ShakeStrength => responseScale;
        public float ForceNewtons => forceNewtons;
        public float TorqueNewtonMeters => torqueNewtonMeters;

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
            ConfigureHouseFurniturePhysics();
            ConfigureTutorialFurniturePhysics();
            if (motionSource == null) motionSource = FindFirstObjectByType<GroundMotionPlayer>();
            // Saved quake-activated furniture must not fall or settle during the
            // preparation countdown. FallingHazard bodies are staged separately.
            if (wakeOnlyWhenEarthquakeStarts && body != null &&
                GetComponent<FallingHazard>() == null && !body.isKinematic)
                body.isKinematic = true;
            // DO NOT overwrite Rigidbody mass, linear/angular damping, constraints,
            // collision mode or interpolation. All remain editable in the Inspector.
        }

        private void ConfigureHouseFurniturePhysics()
        {
            if (body == null || gameObject.scene.name != "House") return;

            string itemName = name.ToLowerInvariant();
            bool looseItem = itemName.Contains("chair") ||
                             itemName.Contains("hazard") ||
                             itemName.Contains("falling") ||
                             itemName.Contains("pillow") ||
                             itemName.Contains("cushion") ||
                             itemName.Contains("vase") ||
                             itemName.Contains("plant") ||
                             itemName.Contains("lamp") ||
                             itemName.Contains("bookstack") ||
                             itemName.Contains("bottle") ||
                             itemName.Contains("cup");
            bool largeItem = body.mass >= 18f ||
                             itemName.Contains("fridge") ||
                             itemName.Contains("refrigerator") ||
                             itemName.Contains("wardrobe") ||
                             itemName.Contains("wandrobe") ||
                             itemName.Contains("cabinet") ||
                             itemName.Contains("bookcase") ||
                             itemName.Contains("shelf") ||
                             itemName.Contains("sofa") ||
                             itemName.Contains("bed") ||
                             itemName.Contains("table") ||
                             itemName.Contains("desk") ||
                             itemName.Contains("stove") ||
                             itemName.Contains("sink");

            anchoredHouseFurniture = largeItem && !looseItem;
            looseHouseFurniture = !anchoredHouseFurniture;
            // Gravity is harmless for kinematic furniture, but crucial when
            // loose bodies are released by the earthquake or by player input.
            body.useGravity = true;

            if (anchoredHouseFurniture)
            {
                houseAnchorPosition = body.position;
                houseAnchorRotation = body.rotation;
                body.isKinematic = true;
            }
        }

        private void ConfigureTutorialFurniturePhysics()
        {
            if (body == null ||
                gameObject.scene.name != "CEVR_ChulaEngineering_Tutorial") return;

            // Hanging lights have a ConfigurableJoint and should remain
            // suspended from that joint; player and task logic is unchanged.
            if (GetComponent<ConfigurableJoint>() != null ||
                GetComponent<WearableShoes>() != null) return;

            string itemName = name.ToLowerInvariant();
            bool isSmall = itemName.Contains("chair") ||
                           itemName.Contains("stagedfallinghazard") ||
                           itemName.Contains("falling") ||
                           itemName.Contains("taskitem") ||
                           itemName.Contains("pillow") ||
                           itemName.Contains("cushion") ||
                           itemName.Contains("vase") ||
                           itemName.Contains("bottle") ||
                           itemName.Contains("cup") ||
                           itemName.Contains("bookstack") ||
                           itemName.Contains("small");
            bool isLarge = body.mass >= 18f ||
                           itemName.Contains("cabinet") ||
                           itemName.Contains("bookcase") ||
                           itemName.Contains("wardrobe") ||
                           itemName.Contains("shelf") ||
                           itemName.Contains("desk") ||
                           itemName.Contains("table") ||
                           itemName.Contains("sofa") ||
                           itemName.Contains("bench") ||
                           itemName.Contains("locker") ||
                           itemName.Contains("fridge");

            anchoredTutorialFurniture = isLarge && !isSmall;
            looseTutorialFurniture = !anchoredTutorialFurniture;
            body.useGravity = true;
            if (anchoredTutorialFurniture)
            {
                tutorialAnchorPosition = body.position;
                tutorialAnchorRotation = body.rotation;
                body.isKinematic = true;
            }
        }

        private void UpdateAnchoredTutorialFurniture()
        {
            if (body == null) return;
            if (!body.isKinematic) body.isKinematic = true;
            if (motionSource != null && motionSource.IsPlaying)
            {
                Vector3 wave = motionSource.CurrentFloorAccelerationMs2 /
                    Mathf.Max(0.01f, 0.16f * Physics.gravity.magnitude);
                float strength = Mathf.Clamp(responseScale, 0f, 2f);
                Vector3 offset = Vector3.ClampMagnitude(
                    new Vector3(wave.x, 0f, wave.z) * (0.008f * strength), 0.018f);
                float yaw = Mathf.Clamp(wave.x * (0.3f * strength), -0.65f, 0.65f);
                body.MovePosition(tutorialAnchorPosition + offset);
                body.MoveRotation(tutorialAnchorRotation * Quaternion.Euler(0f, yaw, 0f));
            }
            else
            {
                body.MovePosition(tutorialAnchorPosition);
                body.MoveRotation(tutorialAnchorRotation);
            }
        }

        private void UpdateAnchoredHouseFurniture()
        {
            if (body == null) return;
            // The hazard system may try releasing old tall furniture as a
            // falling hazard. Keep the *heavy* body physically anchored.
            if (!body.isKinematic) body.isKinematic = true;

            if (motionSource != null && motionSource.IsPlaying)
            {
                Vector3 acceleration = motionSource.CurrentFloorAccelerationMs2;
                float normalized = 1f / Mathf.Max(0.01f, 0.16f * Physics.gravity.magnitude);
                float strength = Mathf.Clamp(responseScale, 0f, 2f);
                Vector3 offset = Vector3.ClampMagnitude(
                    new Vector3(acceleration.x, 0f, acceleration.z) * normalized *
                    (0.008f * strength), 0.018f);
                float yaw = Mathf.Clamp(acceleration.x * normalized *
                    (0.30f * strength), -0.65f, 0.65f);
                body.MovePosition(houseAnchorPosition + offset);
                body.MoveRotation(houseAnchorRotation * Quaternion.Euler(0f, yaw, 0f));
            }
            else
            {
                // No offset accumulates; after the quake the exact authored
                // position and orientation are restored.
                body.MovePosition(houseAnchorPosition);
                body.MoveRotation(houseAnchorRotation);
            }
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
            if (!Application.isPlaying ||
                (!wakeOnlyWhenEarthquakeStarts && !looseTutorialFurniture)) return;
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
            if ((!wakeOnlyWhenEarthquakeStarts && !looseTutorialFurniture) ||
                body == null || GetComponent<FallingHazard>() != null) return;
            // Large House furniture never tips or drifts from its authored
            // location. Loose props and chairs use actual gravity.
            if (anchoredHouseFurniture || anchoredTutorialFurniture) return;
            if (looseHouseFurniture || looseTutorialFurniture)
            {
                body.useGravity = true;
                body.constraints &= ~RigidbodyConstraints.FreezePositionY;
                // Keep chairs upright for the existing grab mechanic; all
                // other small props can tip and land naturally.
                if (!name.ToLowerInvariant().Contains("chair"))
                    body.constraints &= ~(RigidbodyConstraints.FreezeRotationX |
                                          RigidbodyConstraints.FreezeRotationZ);
            }
            body.isKinematic = false;
        }

        private void OnQuakeEnded()
        {
            if (!wakeOnlyWhenEarthquakeStarts || body == null ||
                manualInteractionActive || GetComponent<FallingHazard>() != null) return;
            // Never freeze a loose object mid-air. Let it keep falling after
            // shaking stops until it settles on the floor.
            if (looseHouseFurniture || anchoredHouseFurniture ||
                looseTutorialFurniture || anchoredTutorialFurniture) return;
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
            if (looseHouseFurniture || looseTutorialFurniture)
            {
                body.useGravity = true;
                body.constraints &= ~RigidbodyConstraints.FreezePositionY;
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
            if (anchoredTutorialFurniture)
            {
                UpdateAnchoredTutorialFurniture();
                return;
            }
            if (anchoredHouseFurniture)
            {
                UpdateAnchoredHouseFurniture();
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
            SubscribeToQuake();
            responseScale = Mathf.Clamp(scale, 0f, 3f);
            includeVertical = vertical;
        }
    }
}
