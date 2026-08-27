using UnityEngine;
using UnityEngine.InputSystem;

namespace ChulaEarthquakeVR
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DesktopDebugRig : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.4f;
        [SerializeField, Min(1f)] private float mouseSensitivity = 8f;
        [SerializeField] private float standingHeight = 1.75f;
        [SerializeField] private float crouchingHeight = 1.05f;

        private CharacterController controller;
        private float pitch;
        public static int PointerCaptureFrame { get; private set; } = -1;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null || viewCamera == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    PointerCaptureFrame = Time.frameCount;
                }
                return;
            }

            Vector2 move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move.y += 1f;
            if (Keyboard.current.sKey.isPressed) move.y -= 1f;
            if (Keyboard.current.dKey.isPressed) move.x += 1f;
            if (Keyboard.current.aKey.isPressed) move.x -= 1f;
            Vector3 direction = (transform.forward * move.y + transform.right * move.x).normalized;
            controller.Move((direction * moveSpeed + Physics.gravity) * Time.deltaTime);

            Vector2 look = Mouse.current.delta.ReadValue() * mouseSensitivity * 0.01f;
            transform.Rotate(Vector3.up, look.x, Space.World);
            pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            bool crouching = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
            float height = crouching ? crouchingHeight : standingHeight;
            if (!crouching && controller.height < standingHeight && !HasStandingClearance())
                height = crouchingHeight;
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            viewCamera.transform.localPosition = new Vector3(0f, height - 0.12f, 0f);

        }

        private bool HasStandingClearance()
        {
            float radius = Mathf.Min(controller.radius, standingHeight * 0.45f);
            Vector3 bottom = transform.position + Vector3.up * radius;
            Vector3 top = transform.position + Vector3.up * (standingHeight - radius);
            Collider[] overlaps = Physics.OverlapCapsule(
                bottom, top, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider overlap in overlaps)
                if (overlap != controller && !overlap.transform.IsChildOf(transform)) return false;
            return true;
        }

        public void Configure(Camera camera) => viewCamera = camera;
    }
}
