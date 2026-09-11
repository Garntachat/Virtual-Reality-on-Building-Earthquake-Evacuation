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
            CharacterController controller = GetComponent<CharacterController>();
            float height = controller == null ? shoulderHeight : controller.height - 0.12f;
            Vector3 focus = transform.position + Vector3.up * height;
            Quaternion look = viewCamera.transform.rotation;
            Vector3 desired = focus - (look * Vector3.forward) * followDistance + transform.right * 0.65f;
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
                    nearest = Mathf.Min(nearest, Mathf.Max(0f, hit.distance - 0.02f));
                }
            }
            desired = focus + direction.normalized * nearest;
            viewCamera.transform.position = desired;
            viewCamera.transform.rotation = look;
        }

        public void Configure(Camera camera) => viewCamera = camera;

        private void BuildAvatar()
        {
            avatar = StudentAvatar.Build(transform, "ThirdPersonAvatar");
            avatar.gameObject.SetActive(false);
        }
    }
}
