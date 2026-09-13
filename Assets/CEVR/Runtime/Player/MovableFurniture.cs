using System.Globalization;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MovableFurniture : MonoBehaviour
    {
        [SerializeField] private string furnitureId = "movable-furniture";
        [SerializeField, Min(0.01f)] private float displacementLogThreshold = 0.15f;
        [SerializeField] private SessionLogger logger;
        [SerializeField] private GroundMotionPlayer groundMotion;

        private Vector3 initialPosition;
        private bool displacementLogged;
        private bool interactionActive;

        public string FurnitureId => furnitureId;
        public bool IsInteracting => interactionActive;

        private void Start()
        {
            initialPosition = transform.position;
            if (logger == null) logger = FindFirstObjectByType<SessionLogger>();
            if (groundMotion == null) groundMotion = FindFirstObjectByType<GroundMotionPlayer>();
            if (GetComponent<FurnitureImpactAudio>() == null) gameObject.AddComponent<FurnitureImpactAudio>();
        }

        private void Update()
        {
            if (displacementLogged || logger == null || !logger.IsSessionOpen) return;

            Vector3 displacement = transform.position - initialPosition;
            displacement.y = 0f;
            if (displacement.magnitude < displacementLogThreshold) return;

            displacementLogged = true;
            logger.LogEvent("furniture_displaced",
                $"{{\"furnitureId\":\"{furnitureId}\",\"horizontalMetres\":{Number(displacement.magnitude)}," +
                $"\"duringEarthquake\":{IsQuaking()},\"interactionActive\":{interactionActive.ToString().ToLowerInvariant()}}}");
        }

        public void Configure(string id, SessionLogger sessionLogger, GroundMotionPlayer motion)
        {
            furnitureId = string.IsNullOrWhiteSpace(id) ? "movable-furniture" : id;
            logger = sessionLogger;
            groundMotion = motion;
        }

        public void BeginInteraction(string inputMode)
        {
            interactionActive = true;
            logger?.LogEvent("furniture_interaction_started",
                $"{{\"furnitureId\":\"{furnitureId}\",\"inputMode\":\"{inputMode}\",\"duringEarthquake\":{IsQuaking()}}}");
        }

        public void EndInteraction(string inputMode)
        {
            interactionActive = false;
            logger?.LogEvent("furniture_interaction_ended",
                $"{{\"furnitureId\":\"{furnitureId}\",\"inputMode\":\"{inputMode}\",\"duringEarthquake\":{IsQuaking()}}}");
        }

        private string IsQuaking() =>
            (groundMotion != null && groundMotion.IsPlaying).ToString().ToLowerInvariant();

        private static string Number(float value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
