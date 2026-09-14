using UnityEngine;
using UnityEngine.InputSystem;

namespace ChulaEarthquakeVR
{
    // Capture clicks before DesktopGrabInteractor reads this frame's input.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class DesktopDebugRig : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.4f;
        [SerializeField, Range(1.1f, 3f)] private float runMultiplier = 1.75f;
        [SerializeField, Min(1f)] private float mouseSensitivity = 8f;
        [SerializeField] private float standingHeight = 1.75f;
        [SerializeField] private float crouchingHeight = 1.05f;
        [SerializeField] private float crawlingHeight = 0.58f;
        [SerializeField, Range(0.1f, 1f)] private float crouchMoveMultiplier = 0.72f;
        [SerializeField, Range(0.1f, 1f)] private float crawlMoveMultiplier = 0.68f;
        [SerializeField, Min(0.5f)] private float jumpHeight = 1.05f;
        [SerializeField, Min(1f)] private float gravity = 22f;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;
        private bool crawlToggled;
        private bool running;
        public static int PointerCaptureFrame { get; private set; } = -1;
        public bool IsCrawling => controller != null &&
                                  controller.height <= crawlingHeight + 0.02f;
        public bool IsGrounded => controller != null && controller.isGrounded;
        public bool IsRunning => running;
        public float VerticalVelocity => verticalVelocity;
        public bool CrawlRequested => crawlToggled;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            standingHeight = Mathf.Max(1f, standingHeight);
            crouchingHeight = Mathf.Clamp(crouchingHeight, 0.7f, standingHeight);
            crawlingHeight = Mathf.Clamp(crawlingHeight, controller.radius * 2f + 0.05f, crouchingHeight);
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
            running = false;
        }

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null || viewCamera == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                running = false;
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                running = false;
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (ThirdPersonViewController.IsPointerOverViewButton(Mouse.current.position.ReadValue())) return;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    PointerCaptureFrame = Time.frameCount;
                }
                return;
            }

            Vector2 look = Mouse.current.delta.ReadValue() * mouseSensitivity * 0.01f;
            transform.Rotate(Vector3.up, look.x, Space.World);
            pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            if (Keyboard.current.zKey.wasPressedThisFrame) crawlToggled = !crawlToggled;
            bool crouching = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
            float requestedHeight = crawlToggled
                ? crawlingHeight
                : crouching ? crouchingHeight : standingHeight;
            if (requestedHeight > controller.height && !HasClearance(requestedHeight))
                requestedHeight = controller.height;
            ApplyHeight(requestedHeight);

            Vector2 move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move.y += 1f;
            if (Keyboard.current.sKey.isPressed) move.y -= 1f;
            if (Keyboard.current.dKey.isPressed) move.x += 1f;
            if (Keyboard.current.aKey.isPressed) move.x -= 1f;
            Vector3 direction = (transform.forward * move.y + transform.right * move.x).normalized;

            bool standing = !IsCrawling && controller.height >= standingHeight - 0.02f;
            bool shiftHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            running = shiftHeld && standing && move.sqrMagnitude > 0.01f && controller.isGrounded;

            float stanceSpeed = IsCrawling
                ? crawlMoveMultiplier
                : controller.height < standingHeight - 0.02f ? crouchMoveMultiplier : 1f;
            float movementMultiplier = stanceSpeed * (running ? runMultiplier : 1f);

            // Keep a small downward force while grounded so CharacterController remains snapped
            // to slopes/floors. Space launches the player only while grounded and not crawling.
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded && !IsCrawling)
            {
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            }
            verticalVelocity -= gravity * Time.deltaTime;

            Vector3 velocity = direction * (moveSpeed * movementMultiplier);
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void ApplyHeight(float height)
        {
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            viewCamera.transform.localPosition = new Vector3(0f, Mathf.Max(0.2f, height - 0.12f), 0f);
        }

        private bool HasClearance(float targetHeight)
        {
            if (targetHeight <= controller.height + 0.01f) return true;
            float radius = Mathf.Min(controller.radius * 0.9f, targetHeight * 0.45f);
            Vector3 bottom = transform.position + Vector3.up * Mathf.Max(radius, controller.height - radius);
            Vector3 top = transform.position + Vector3.up * (targetHeight - radius);
            Collider[] overlaps = Physics.OverlapCapsule(
                bottom, top, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider overlap in overlaps)
                if (overlap != controller && !overlap.transform.IsChildOf(transform)) return false;
            return true;
        }

        public void Configure(Camera camera) => viewCamera = camera;
    }
}
