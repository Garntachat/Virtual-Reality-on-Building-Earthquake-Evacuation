using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ProtectivePillow : MonoBehaviour
    {
        [SerializeField] private string pillowId = "protective-pillow-01";
        [SerializeField] private SessionLogger logger;
        private PlayerHealth protectedPlayer;
        private bool isProtecting;
        private PlayerHealth nearbyPlayer;
        private Camera playerCamera;
        private bool explicitlyHeld;

        public bool IsProtecting => isProtecting;

        private void Start()
        {
            if (logger == null) logger = FindFirstObjectByType<SessionLogger>();
            playerCamera = FindFirstObjectByType<Camera>();
            nearbyPlayer = playerCamera == null ? null : playerCamera.GetComponentInParent<PlayerHealth>();
        }

        private void Update()
        {
            if (explicitlyHeld || playerCamera == null || nearbyPlayer == null) return;
            bool overHead = transform.position.y >= playerCamera.transform.position.y + 0.05f &&
                            Vector3.Distance(transform.position, playerCamera.transform.position) <= 0.85f;
            ApplyProtection(nearbyPlayer, overHead, false);
        }

        public void Configure(string id, SessionLogger sessionLogger)
        {
            pillowId = string.IsNullOrWhiteSpace(id) ? name : id;
            logger = sessionLogger;
        }

        public void SetHeldBy(PlayerHealth player, bool held)
        {
            explicitlyHeld = held;
            ApplyProtection(player, held, true);
        }

        private void ApplyProtection(PlayerHealth player, bool enabled, bool logChange)
        {
            if (enabled == isProtecting && (!enabled || protectedPlayer == player)) return;
            if (protectedPlayer != null) protectedPlayer.SetProtection("pillow:" + pillowId, false);
            protectedPlayer = enabled ? player : null;
            isProtecting = enabled && player != null;
            if (isProtecting) protectedPlayer.SetProtection("pillow:" + pillowId, true);
            if (!logChange) return;
            logger?.LogEvent(enabled ? "pillow_cover_started" : "pillow_cover_ended",
                $"{{\"pillowId\":\"{pillowId}\"}}");
        }

        private void OnDisable()
        {
            if (protectedPlayer != null) protectedPlayer.SetProtection("pillow:" + pillowId, false);
            protectedPlayer = null;
            isProtecting = false;
            explicitlyHeld = false;
        }
    }
}
