using UnityEngine;
using UnityEngine.InputSystem;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonViewController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(1f)] private float followDistance = 3.1f;
        [SerializeField, Min(0.2f)] private float shoulderHeight = 1.15f;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.16f;
        private Transform avatar;

        public bool IsThirdPerson { get; private set; }

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            BuildAvatar();
        }

        private void Update()
        {
            if (viewCamera == null || Keyboard.current == null) return;
            if (viewCamera.stereoEnabled)
            {
                IsThirdPerson = false;
                if (avatar != null) avatar.gameObject.SetActive(false);
                return;
            }
            if (Keyboard.current.tKey.wasPressedThisFrame) IsThirdPerson = !IsThirdPerson;
            if (avatar != null) avatar.gameObject.SetActive(IsThirdPerson);
        }

        private void LateUpdate()
        {
            if (!IsThirdPerson || viewCamera == null || viewCamera.stereoEnabled) return;
            Vector3 focus = transform.position + Vector3.up * shoulderHeight;
            Vector3 desired = focus - transform.forward * followDistance + Vector3.up * 0.45f;
            Vector3 direction = desired - focus;
            float distance = direction.magnitude;
            float nearest = distance;
            if (distance > 0.01f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(
                    focus, collisionRadius, direction.normalized, distance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                    nearest = Mathf.Min(nearest, Mathf.Max(0.25f, hit.distance - collisionRadius));
                }
            }
            desired = focus + direction.normalized * nearest;
            viewCamera.transform.position = desired;
            viewCamera.transform.rotation = Quaternion.LookRotation(focus - desired, Vector3.up);
        }

        public void Configure(Camera camera) => viewCamera = camera;

        private void BuildAvatar()
        {
            if (transform.Find("ThirdPersonAvatar") != null) return;
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "ThirdPersonAvatar";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            body.transform.localScale = new Vector3(0.42f, 0.84f, 0.42f);
            Collider bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null) bodyCollider.enabled = false;
            Renderer renderer = body.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.88f, 0.08f, 0.4f);
            avatar = body.transform;
            body.SetActive(false);
        }
    }
}
