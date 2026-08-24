using UnityEngine;

namespace ChulaEarthquakeVR
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FallingHazard : MonoBehaviour
    {
        [SerializeField] private string hazardId = "falling-object";
        [SerializeField, Min(0f)] private float damage = 25f;
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 1.5f;
        [SerializeField] private bool oneHitOnly = true;
        private bool armed;
        private bool spent;

        private void OnCollisionEnter(Collision collision)
        {
            if (!armed || spent || collision.relativeVelocity.magnitude < minimumImpactSpeed) return;
            PlayerHealth health = collision.collider.GetComponentInParent<PlayerHealth>();
            if (health == null) return;
            if (health.ApplyDamage(damage, hazardId) && oneHitOnly) spent = true;
        }

        public void Configure(string id, float hitDamage)
        {
            hazardId = string.IsNullOrWhiteSpace(id) ? name : id;
            damage = Mathf.Max(0f, hitDamage);
        }

        public void Arm(bool value)
        {
            armed = value;
            if (value) spent = false;
        }
    }
}
