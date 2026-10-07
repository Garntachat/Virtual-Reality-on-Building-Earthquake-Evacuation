using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ChulaEarthquakeVR
{
    /// <summary>A small house companion. Movement yields to both desktop and XR ownership.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public sealed class HousePetController : MonoBehaviour
    {
        private Rigidbody body;
        private XRGrabInteractable grab;
        private GroundMotionPlayer motion;
        private Transform model;
        private Transform[] legs;
        private Transform tail;
        private Vector3 home;
        private Vector3 heading;
        private float decisionAt;
        private float phase;
        private bool desktopHeld;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        public bool IsHeld => desktopHeld || (grab != null && grab.isSelected);

        public void Configure(GroundMotionPlayer source, Transform visual, Transform[] feet, Transform tailJoint)
        {
            body = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
            motion = source;
            model = visual;
            legs = feet;
            tail = tailJoint;
            home = transform.position;
            heading = transform.forward;
            decisionAt = Time.time + 2f;
        }

        public bool TryHold()
        {
            if (IsHeld) return false;
            desktopHeld = true;
            if (grab != null) grab.enabled = false;
            return true;
        }

        public void Release()
        {
            desktopHeld = false;
            if (grab != null) grab.enabled = true;
            decisionAt = Time.time + 2f;
        }

        private bool ClearRay(Vector3 origin, Vector3 direction, float length)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, hits, length,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(transform)) return false;
            return true;
        }

        private bool GroundAhead(Vector3 position)
        {
            int count = Physics.RaycastNonAlloc(position + Vector3.up * 0.15f,
                Vector3.down, hits, 0.28f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(transform) && hits[i].normal.y > 0.8f) return true;
            return false;
        }

        private void FixedUpdate()
        {
            if (body == null || IsHeld || body.isKinematic) return;
            // Never steer in mid-air or pull a carried/released pet back to its spawn.
            if (!GroundAhead(body.position)) return;
            bool shaking = motion != null && motion.IsPlaying;
            if (Time.time >= decisionAt)
            {
                phase += 2.399963f;
                Vector3 towardHome = home - body.position;
                towardHome.y = 0f;
                heading = towardHome.sqrMagnitude > 4f ? towardHome.normalized
                    : new Vector3(Mathf.Sin(phase), 0f, Mathf.Cos(phase));
                decisionAt = Time.time + 3f;
            }
            Vector3 ahead = body.position + heading * 0.45f;
            bool clear = !shaking && GroundAhead(ahead) &&
                ClearRay(body.position + Vector3.up * 0.22f, heading, 0.62f) &&
                ClearRay(body.position + Vector3.up * 0.22f + Vector3.Cross(heading, Vector3.up) * 0.16f, heading, 0.62f) &&
                ClearRay(body.position + Vector3.up * 0.22f - Vector3.Cross(heading, Vector3.up) * 0.16f, heading, 0.62f);
            float speed = clear && Time.time < decisionAt - 0.8f ? 0.38f : 0f;
            Vector3 velocity = body.linearVelocity;
            Vector3 desired = heading * speed;
            velocity.x = Mathf.MoveTowards(velocity.x, desired.x, 1.5f * Time.fixedDeltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, desired.z, 1.5f * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
            if (!clear && !shaking) decisionAt = Mathf.Min(decisionAt, Time.time + 0.2f);
            if (speed > 0f) body.MoveRotation(Quaternion.RotateTowards(body.rotation,
                Quaternion.LookRotation(heading), 100f * Time.fixedDeltaTime));
        }

        private void LateUpdate()
        {
            if (body == null || model == null) return;
            bool shaking = motion != null && motion.IsPlaying && !IsHeld;
            model.localPosition = Vector3.Lerp(model.localPosition,
                shaking ? Vector3.down * 0.08f : Vector3.zero, 8f * Time.deltaTime);
            float gait = !IsHeld && !shaking && GroundAhead(body.position)
                ? Mathf.Clamp01(new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude / 0.38f) : 0f;
            for (int i = 0; legs != null && i < legs.Length; i++)
                legs[i].localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 9f + (i == 0 || i == 3 ? 0 : Mathf.PI)) * 22f * gait, 0f, 0f);
            if (tail != null) tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.8f) * 12f, 0f);
        }
    }
}
