using UnityEngine;
using UnityEngine.InputSystem;

namespace ChulaEarthquakeVR
{
    public sealed class DesktopGrabInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.5f)] private float maximumGrabDistance = 5f;
        [SerializeField, Min(0.5f)] private float holdDistance = 1.4f;
        [SerializeField, Min(1f)] private float followStrength = 18f;
        [SerializeField, Min(1f)] private float maximumFollowSpeed = 8f;

        private Rigidbody heldBody;
        private MovableFurniture heldFurniture;
        private bool previousUseGravity;
        private float previousLinearDamping;
        private float previousAngularDamping;
        private GUIStyle promptStyle;
        private GUIStyle crosshairStyle;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void OnDisable() => ReleaseHeldItem(true);

        private void Update()
        {
            if (viewCamera == null || Keyboard.current == null || Mouse.current == null) return;

            bool interactPressed = Keyboard.current.eKey.wasPressedThisFrame ||
                                   (Cursor.lockState == CursorLockMode.Locked &&
                                    DesktopDebugRig.PointerCaptureFrame != Time.frameCount &&
                                    Mouse.current.leftButton.wasPressedThisFrame);
            if (interactPressed)
            {
                if (heldBody == null) TryGrab();
                else ReleaseHeldItem(true);
            }

            if (heldBody != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ReleaseHeldItem(true);

            if (heldBody != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    holdDistance = Mathf.Clamp(holdDistance + scroll * 0.0015f, 0.8f, 2.5f);
            }
        }

        private void FixedUpdate()
        {
            if (heldBody == null || viewCamera == null) return;

            // PlacementGoal makes a correctly placed item kinematic. Stop controlling it
            // without restoring its previous physics state so it remains snapped in place.
            if (heldBody.isKinematic)
            {
                EndFurnitureInteraction();
                heldBody = null;
                return;
            }

            Vector3 target = viewCamera.transform.position + viewCamera.transform.forward * holdDistance;
            if (heldFurniture != null) target.y = heldBody.position.y;
            heldBody.linearVelocity = Vector3.ClampMagnitude(
                (target - heldBody.position) * followStrength, maximumFollowSpeed);
            heldBody.angularVelocity *= 0.8f;
        }

        private void OnGUI()
        {
            EnsureGuiStyles();
            GUI.Label(new Rect(Screen.width * 0.5f - 12f, Screen.height * 0.5f - 18f, 24f, 36f),
                "+", crosshairStyle);
            GUI.Box(new Rect(Screen.width * 0.5f - 265f, Screen.height - 82f, 530f, 48f), GUIContent.none);
            GUI.Label(new Rect(Screen.width * 0.5f - 255f, Screen.height - 76f, 510f, 36f),
                InteractionPrompt(), promptStyle);
        }

        private string InteractionPrompt()
        {
            if (heldBody != null) return "E / LEFT CLICK: RELEASE  •  MOUSE WHEEL: DISTANCE";
            if (viewCamera == null) return DefaultPrompt();

            Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, maximumGrabDistance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return DefaultPrompt();
            if (hit.collider.GetComponentInParent<MovableFurniture>() != null)
                return "E / LEFT CLICK: GRAB AND SLIDE CHAIR";
            if (hit.collider.GetComponentInParent<TaskItem>() != null)
                return "E / LEFT CLICK: GRAB TASK ITEM";
            return DefaultPrompt();
        }

        private static string DefaultPrompt() =>
            "WASD MOVE  •  MOUSE LOOK  •  TURN 180° TO FIND THE PINK CHAIR  •  C CROUCH";

        private void EnsureGuiStyles()
        {
            if (promptStyle != null) return;
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            promptStyle.normal.textColor = Color.white;
            crosshairStyle = new GUIStyle(promptStyle) { fontSize = 24 };
        }

        private void TryGrab()
        {
            Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, maximumGrabDistance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;

            TaskItem item = hit.collider.GetComponentInParent<TaskItem>();
            MovableFurniture furniture = hit.collider.GetComponentInParent<MovableFurniture>();
            if (item == null && furniture == null) return;

            Rigidbody body = item != null ? item.GetComponent<Rigidbody>() : furniture.GetComponent<Rigidbody>();
            if (body == null || body.isKinematic) return;

            heldBody = body;
            previousUseGravity = body.useGravity;
            previousLinearDamping = body.linearDamping;
            previousAngularDamping = body.angularDamping;
            body.useGravity = false;
            body.linearDamping = 10f;
            body.angularDamping = 10f;
            body.angularVelocity = Vector3.zero;
            heldFurniture = furniture;
            heldFurniture?.BeginInteraction("desktop");
        }

        private void ReleaseHeldItem(bool restorePhysics)
        {
            if (heldBody == null) return;
            EndFurnitureInteraction();
            if (restorePhysics && !heldBody.isKinematic)
            {
                heldBody.useGravity = previousUseGravity;
                heldBody.linearDamping = previousLinearDamping;
                heldBody.angularDamping = previousAngularDamping;
            }
            heldBody = null;
        }

        private void EndFurnitureInteraction()
        {
            if (heldFurniture == null) return;
            heldFurniture.EndInteraction("desktop");
            heldFurniture = null;
        }

        public void Configure(Camera camera) => viewCamera = camera;
    }
}
