using UnityEngine;
using UnityEngine.InputSystem;

namespace ChulaEarthquakeVR
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class LocalSecondPlayerController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(10f)] private float turnSpeed = 95f;
        private CharacterController controller;
        private bool crawling;

        private void Awake() => controller = GetComponent<CharacterController>();

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.nKey.wasPressedThisFrame) crawling = !crawling;
            float height = crawling ? 0.58f : 1.75f;
            if (height > controller.height)
            {
                foreach (Collider overlap in Physics.OverlapCapsule(
                    transform.position + Vector3.up * Mathf.Max(controller.radius, controller.height - controller.radius),
                    transform.position + Vector3.up * (height - controller.radius), controller.radius * 0.9f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    if (overlap != controller && !overlap.transform.IsChildOf(transform)) { height = controller.height; break; }
            }
            controller.height = height;
            controller.center = Vector3.up * height * 0.5f;
            float forward = (Keyboard.current.iKey.isPressed ? 1f : 0f) -
                            (Keyboard.current.kKey.isPressed ? 1f : 0f);
            float side = (Keyboard.current.lKey.isPressed ? 1f : 0f) -
                         (Keyboard.current.jKey.isPressed ? 1f : 0f);
            float turn = (Keyboard.current.oKey.isPressed ? 1f : 0f) -
                         (Keyboard.current.uKey.isPressed ? 1f : 0f);
            transform.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime, Space.World);
            Vector3 direction = (transform.forward * forward + transform.right * side).normalized;
            controller.Move((direction * moveSpeed * (controller.height < 1f ? 0.42f : 1f) + Physics.gravity) * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (playerCamera == null) return;
            Vector3 focus = transform.position + Vector3.up * (controller.height - 0.12f);
            Vector3 desired = focus - transform.forward * 3f + Vector3.up * 0.55f;
            Vector3 offset = desired - focus;
            float distance = offset.magnitude;
            float nearest = distance;
            foreach (RaycastHit hit in Physics.SphereCastAll(focus, 0.16f, offset.normalized, distance,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider != controller && !hit.transform.IsChildOf(transform))
                    nearest = Mathf.Min(nearest, Mathf.Max(0f, hit.distance - 0.02f));
            playerCamera.transform.SetPositionAndRotation(
                focus + offset.normalized * nearest, Quaternion.LookRotation(-offset, Vector3.up));
        }

        public void Configure(Camera camera) => playerCamera = camera;
    }
}
