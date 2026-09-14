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
        private Rigidbody body;
        private Collider[] hazardColliders;
        private Vector3 previousPosition;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            hazardColliders = GetComponentsInChildren<Collider>(true);
            previousPosition = body.position;
        }

        private void FixedUpdate()
        {
            Vector3 currentPosition = body.position;
            Vector3 movement = currentPosition - previousPosition;
            float sweptSpeed = movement.magnitude / Mathf.Max(0.0001f, Time.fixedDeltaTime);
            float impactSpeed = Mathf.Max(body.linearVelocity.magnitude, sweptSpeed);
            if (armed && !spent && impactSpeed >= minimumImpactSpeed)
                CheckSweptPlayerContact(previousPosition, currentPosition, impactSpeed);
            previousPosition = currentPosition;
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryDamage(collision.collider, collision.relativeVelocity.magnitude);
        }

        private void CheckSweptPlayerContact(Vector3 from, Vector3 to, float impactSpeed)
        {
            bool foundBounds = false;
            Bounds sweptBounds = default;
            Vector3 previousOffset = from - to;
            foreach (Collider ownCollider in hazardColliders)
            {
                if (ownCollider == null || !ownCollider.enabled || ownCollider.isTrigger) continue;
                Bounds current = ownCollider.bounds;
                Bounds previous = new Bounds(current.center + previousOffset, current.size);
                if (!foundBounds)
                {
                    sweptBounds = current;
                    foundBounds = true;
                }
                else sweptBounds.Encapsulate(current);
                sweptBounds.Encapsulate(previous);
            }
            if (!foundBounds) return;

            sweptBounds.Expand(0.10f);
            Collider[] contacts = Physics.OverlapBox(
                sweptBounds.center, sweptBounds.extents, Quaternion.identity,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider contact in contacts)
            {
                if (IsOwnCollider(contact)) continue;
                if (TryDamage(contact, impactSpeed)) return;
            }
        }

        private bool IsOwnCollider(Collider candidate)
        {
            foreach (Collider ownCollider in hazardColliders)
                if (candidate == ownCollider) return true;
            return false;
        }

        private bool TryDamage(Collider target, float impactSpeed)
        {
            if (!armed || spent || target == null || impactSpeed < minimumImpactSpeed) return false;
            PlayerHealth health = target.GetComponentInParent<PlayerHealth>();
            if (health == null || !health.ApplyDamage(damage, hazardId)) return false;
            if (oneHitOnly) spent = true;
            return true;
        }

        public void Configure(string id, float hitDamage)
        {
            hazardId = string.IsNullOrWhiteSpace(id) ? name : id;
            damage = Mathf.Max(0f, hitDamage);
        }

        public void Arm(bool value)
        {
            armed = value;
            if (body == null) body = GetComponent<Rigidbody>();
            if (value)
            {
                spent = false;
                previousPosition = body.position;
            }
        }
    }
}
