using UnityEngine;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Play-mode movement for the cat already saved in House.unity.
    /// It never creates or replaces the cat visual, and it never writes back to the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HouseSavedCatController : MonoBehaviour
    {
        private static readonly Vector2[] PatrolOffsets =
        {
            new Vector2(0.00f, 0.00f),
            new Vector2(0.85f, 0.45f),
            new Vector2(0.30f, 1.10f),
            new Vector2(-0.75f, 0.75f),
            new Vector2(-0.95f, -0.10f),
            new Vector2(-0.25f, -0.95f),
            new Vector2(0.75f, -0.70f)
        };

        [SerializeField, Min(0.05f)] private float walkSpeed = 0.42f;
        [SerializeField, Min(10f)] private float turnSpeed = 150f;
        [SerializeField, Min(0.5f)] private float patrolRadius = 1.0f;

        private Rigidbody body;
        private Vector3 home;
        private Vector3 target;
        private int targetIndex;
        private float waitUntil;
        private Transform tailBase;
        private Transform tailTip;
        private Transform[] legs;
        private Quaternion tailBaseRest;
        private Quaternion tailTipRest;
        private Quaternion[] legRest;
        private bool configured;

        public void Configure()
        {
            if (configured) return;
            configured = true;

            home = transform.position;
            target = home;

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.center = new Vector3(0f, 0.30f, 0f);
            capsule.radius = 0.17f;
            capsule.height = 0.62f;

            body = GetComponent<Rigidbody>();
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            tailBase = FindChild("Cat_TailBase");
            tailTip = FindChild("Cat_TailTip");
            tailBaseRest = tailBase == null ? Quaternion.identity : tailBase.localRotation;
            tailTipRest = tailTip == null ? Quaternion.identity : tailTip.localRotation;

            legs = new[]
            {
                FindChild("Cat_LegFrontLeft"),
                FindChild("Cat_LegFrontRight"),
                FindChild("Cat_LegBackLeft"),
                FindChild("Cat_LegBackRight")
            };
            legRest = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++)
                legRest[i] = legs[i] == null ? Quaternion.identity : legs[i].localRotation;

            PickNextTarget();
        }

        private void Awake() => Configure();

        private void FixedUpdate()
        {
            if (!configured) Configure();
            if (body == null) return;

            Vector3 position = body.position;
            Vector3 flat = target - position;
            flat.y = 0f;

            if (flat.sqrMagnitude < 0.08f)
            {
                if (Time.time >= waitUntil) PickNextTarget();
                Animate(false);
                return;
            }

            Vector3 direction = flat.normalized;
            if (!HasGroundAhead(position, direction) || ObstacleAhead(position, direction))
            {
                PickNextTarget();
                Animate(false);
                return;
            }

            Quaternion wanted = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion rotation = Quaternion.RotateTowards(
                body.rotation, wanted, turnSpeed * Time.fixedDeltaTime);
            Vector3 step = direction * walkSpeed * Time.fixedDeltaTime;
            Vector3 next = position + step;
            next.y = home.y;

            body.MoveRotation(rotation);
            body.MovePosition(next);
            Animate(true);
        }

        private void PickNextTarget()
        {
            targetIndex = (targetIndex + 1) % PatrolOffsets.Length;
            Vector2 offset = PatrolOffsets[targetIndex] * patrolRadius;
            target = new Vector3(home.x + offset.x, home.y, home.z + offset.y);

            target.x = Mathf.Clamp(target.x, -4.0f, 6.0f);
            target.z = Mathf.Clamp(target.z, -6.4f, 6.4f);
            waitUntil = Time.time + 0.65f;
        }

        private bool HasGroundAhead(Vector3 position, Vector3 direction)
        {
            Vector3 origin = position + direction * 0.34f + Vector3.up * 0.65f;
            RaycastHit[] hits = Physics.RaycastAll(
                origin, Vector3.down, 1.30f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (hit.normal.y > 0.65f) return true;
            }
            return false;
        }

        private bool ObstacleAhead(Vector3 position, Vector3 direction)
        {
            Vector3 origin = position + Vector3.up * 0.28f;
            RaycastHit[] hits = Physics.SphereCastAll(
                origin, 0.15f, direction, 0.42f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (hit.normal.y > 0.75f) continue;
                return true;
            }
            return false;
        }

        private void Animate(bool walking)
        {
            float gait = walking ? Mathf.Sin(Time.time * 10f) * 20f : 0f;
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                float phase = (i == 0 || i == 3) ? gait : -gait;
                legs[i].localRotation = legRest[i] * Quaternion.Euler(phase, 0f, 0f);
            }

            float tail = Mathf.Sin(Time.time * 2.4f) * 14f;
            if (tailBase != null)
                tailBase.localRotation = tailBaseRest * Quaternion.Euler(0f, tail, 0f);
            if (tailTip != null)
                tailTip.localRotation = tailTipRest * Quaternion.Euler(0f, -tail * 0.7f, 0f);
        }

        private Transform FindChild(string exactName)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name == exactName) return t;
            return null;
        }
    }
}
