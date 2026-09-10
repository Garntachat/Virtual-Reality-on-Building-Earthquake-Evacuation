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

        private void Awake() => controller = GetComponent<CharacterController>();

        private void Update()
        {
            if (Keyboard.current == null) return;
            float forward = (Keyboard.current.iKey.isPressed ? 1f : 0f) -
                            (Keyboard.current.kKey.isPressed ? 1f : 0f);
            float side = (Keyboard.current.lKey.isPressed ? 1f : 0f) -
                         (Keyboard.current.jKey.isPressed ? 1f : 0f);
            float turn = (Keyboard.current.oKey.isPressed ? 1f : 0f) -
                         (Keyboard.current.uKey.isPressed ? 1f : 0f);
            transform.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime, Space.World);
            Vector3 direction = (transform.forward * forward + transform.right * side).normalized;
            controller.Move((direction * moveSpeed + Physics.gravity) * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (playerCamera == null) return;
            Vector3 focus = transform.position + Vector3.up * 1.05f;
            Vector3 desired = focus - transform.forward * 3f + Vector3.up * 0.55f;
            playerCamera.transform.SetPositionAndRotation(
                desired, Quaternion.LookRotation(focus - desired, Vector3.up));
        }

        public void Configure(Camera camera) => playerCamera = camera;
    }
}
