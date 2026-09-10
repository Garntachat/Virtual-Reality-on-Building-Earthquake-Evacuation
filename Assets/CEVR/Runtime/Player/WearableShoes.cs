using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    public sealed class WearableShoes : MonoBehaviour
    {
        [SerializeField] private string footwearId = "protective-shoes-01";
        [SerializeField] private SessionLogger logger;

        public bool IsEquipped { get; private set; }

        private void Start()
        {
            if (logger == null) logger = FindFirstObjectByType<SessionLogger>();
        }

        public void Configure(string id, SessionLogger sessionLogger)
        {
            footwearId = string.IsNullOrWhiteSpace(id) ? name : id;
            logger = sessionLogger;
        }

        public void Equip(PlayerHealth wearer, Transform wearerRoot)
        {
            if (IsEquipped || wearer == null || wearerRoot == null) return;
            IsEquipped = true;
            wearer.EquipProtectiveFootwear();
            transform.SetParent(wearerRoot, false);
            transform.localPosition = new Vector3(0f, 0.07f, 0.08f);
            transform.localRotation = Quaternion.identity;
            foreach (Collider itemCollider in GetComponentsInChildren<Collider>()) itemCollider.enabled = false;
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
            logger?.LogEvent("footwear_equipped", $"{{\"footwearId\":\"{footwearId}\"}}");
        }
    }
}
