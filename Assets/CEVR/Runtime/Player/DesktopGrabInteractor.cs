using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChulaEarthquakeVR
{
    public sealed class DesktopGrabInteractor : MonoBehaviour
    {
        private static readonly HashSet<Rigidbody> ClaimedBodies = new HashSet<Rigidbody>();
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.5f)] private float maximumGrabDistance = 5f;
        [SerializeField, Min(0.5f)] private float holdDistance = 1.4f;
        [SerializeField, Min(1f)] private float followStrength = 18f;
        [SerializeField, Min(1f)] private float maximumFollowSpeed = 8f;
        [SerializeField] private bool secondaryPlayerControls;

        private Rigidbody heldBody;
        private MovableFurniture heldFurniture;
        private ProtectivePillow heldPillow;
        private DesktopDebugRig desktopRig;
        private bool previousUseGravity;
        private float previousLinearDamping;
        private float previousAngularDamping;
        private GUIStyle promptStyle;
        private GUIStyle crosshairStyle;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            desktopRig = GetComponent<DesktopDebugRig>();
        }

        private void OnDisable() => ReleaseHeldItem(true);

        private void Update()
        {
            if (viewCamera == null || Keyboard.current == null || Mouse.current == null) return;

            bool interactPressed = secondaryPlayerControls
                ? Keyboard.current.rightShiftKey.wasPressedThisFrame
                : Keyboard.current.eKey.wasPressedThisFrame ||
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

            if (heldBody != null && !secondaryPlayerControls)
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
                ReleaseHeldItem(false);
                return;
            }

            Vector3 target = viewCamera.transform.position + viewCamera.transform.forward * holdDistance;
            if (heldPillow != null)
                target = viewCamera.transform.position + viewCamera.transform.forward * 0.48f + Vector3.up * 0.42f;
            if (heldFurniture != null) target.y = heldBody.position.y;
            heldBody.linearVelocity = Vector3.ClampMagnitude(
                (target - heldBody.position) * followStrength, maximumFollowSpeed);
            heldBody.angularVelocity *= 0.8f;
        }

        private void OnGUI()
        {
            EnsureGuiStyles();
            bool targetAvailable = HasGrabbableTarget();
            Rect viewport = viewCamera == null ? new Rect(0f, 0f, 1f, 1f) : viewCamera.rect;
            float centerX = (viewport.x + viewport.width * 0.5f) * Screen.width;
            float centerY = (1f - viewport.y - viewport.height * 0.5f) * Screen.height;
            float bottomY = (1f - viewport.y) * Screen.height;
            crosshairStyle.normal.textColor = heldBody != null
                ? new Color(1f, 0.3f, 0.58f)
                : targetAvailable ? new Color(0.2f, 1f, 0.55f) : Color.white;
            GUI.Label(new Rect(centerX - 12f, centerY - 18f, 24f, 36f),
                "+", crosshairStyle);
            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = heldBody != null
                ? new Color(0.88f, 0.08f, 0.4f, 0.95f)
                : targetAvailable ? new Color(0.05f, 0.62f, 0.31f, 0.95f) : new Color(0.04f, 0.08f, 0.13f, 0.94f);
            GUI.Box(new Rect(centerX - 250f, bottomY - 82f, 500f, 48f), GUIContent.none);
            GUI.backgroundColor = previousBackground;
            GUI.Label(new Rect(centerX - 240f, bottomY - 76f, 480f, 36f),
                InteractionPrompt(), promptStyle);
        }

        private bool RaycastTarget(Ray ray, out RaycastHit target)
        {
            target = default;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit hit in Physics.RaycastAll(ray, maximumGrabDistance,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                target = hit;
            }
            return !float.IsPositiveInfinity(nearest);
        }

        private bool HasGrabbableTarget()
        {
            if (viewCamera == null) return false;
            Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!RaycastTarget(ray, out RaycastHit hit)) return false;
            return hit.collider.GetComponentInParent<MovableFurniture>() != null ||
                   hit.collider.GetComponentInParent<TaskItem>() != null ||
                   hit.collider.GetComponentInParent<ProtectivePillow>() != null ||
                   hit.collider.GetComponentInParent<WearableShoes>() != null;
        }

        private string InteractionPrompt()
        {
            string use = secondaryPlayerControls ? "RIGHT SHIFT" : "E / LEFT CLICK";
            if (heldPillow != null) return $"PILLOW HELD OVER HEAD  •  {use}: RELEASE";
            if (heldBody != null) return secondaryPlayerControls
                ? "RIGHT SHIFT: RELEASE"
                : "E / LEFT CLICK: RELEASE  •  MOUSE WHEEL: DISTANCE";
            if (viewCamera == null) return DefaultPrompt();

            Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!RaycastTarget(ray, out RaycastHit hit))
                return DefaultPrompt();
            if (hit.collider.GetComponentInParent<MovableFurniture>() != null)
                return $"{use}: GRAB AND SLIDE CHAIR";
            if (hit.collider.GetComponentInParent<TaskItem>() != null)
                return $"{use}: GRAB TASK ITEM";
            if (hit.collider.GetComponentInParent<ProtectivePillow>() != null)
                return $"{use}: HOLD PILLOW OVER HEAD";
            if (hit.collider.GetComponentInParent<WearableShoes>() != null)
                return $"{use}: WEAR SHOES";
            return MovementPrompt();
        }

        private string MovementPrompt()
        {
            if (desktopRig == null || !desktopRig.IsCrawling)
                return secondaryPlayerControls
                    ? "P2: IJKL MOVE  •  U/O TURN  •  RIGHT SHIFT INTERACT"
                    : "WASD MOVE  •  E INTERACT  •  Z CRAWL  •  T VIEW  •  F2 LOCAL CO-OP";
            return desktopRig.CrawlRequested
                ? "CRAWLING  •  WASD MOVE  •  Z TRY TO STAND"
                : "BLOCKED ABOVE  •  MOVE OUT FROM UNDER THE TABLE TO STAND";
        }

        private string DefaultPrompt() => MovementPrompt();

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
            if (!RaycastTarget(ray, out RaycastHit hit)) return;

            TaskItem item = hit.collider.GetComponentInParent<TaskItem>();
            MovableFurniture furniture = hit.collider.GetComponentInParent<MovableFurniture>();
            ProtectivePillow pillow = hit.collider.GetComponentInParent<ProtectivePillow>();
            WearableShoes shoes = hit.collider.GetComponentInParent<WearableShoes>();
            if (shoes != null)
            {
                shoes.Equip(GetComponent<PlayerHealth>(), transform);
                return;
            }
            if (item == null && furniture == null && pillow == null) return;

            Rigidbody body = item != null
                ? item.GetComponent<Rigidbody>()
                : furniture != null ? furniture.GetComponent<Rigidbody>() : pillow.GetComponent<Rigidbody>();
            ClaimedBodies.RemoveWhere(claimed => claimed == null);
            if (body == null || body.isKinematic || ClaimedBodies.Contains(body)) return;

            heldBody = body;
            ClaimedBodies.Add(body);
            previousUseGravity = body.useGravity;
            previousLinearDamping = body.linearDamping;
            previousAngularDamping = body.angularDamping;
            body.useGravity = false;
            body.linearDamping = 10f;
            body.angularDamping = 10f;
            body.angularVelocity = Vector3.zero;
            heldFurniture = furniture;
            heldPillow = pillow;
            heldPillow?.SetHeldBy(GetComponent<PlayerHealth>(), true);
            heldFurniture?.BeginInteraction("desktop");
        }

        private void ReleaseHeldItem(bool restorePhysics)
        {
            if (heldBody == null) return;
            Rigidbody releasedBody = heldBody;
            EndFurnitureInteraction();
            heldPillow?.SetHeldBy(GetComponent<PlayerHealth>(), false);
            heldPillow = null;
            if (restorePhysics && !heldBody.isKinematic)
            {
                heldBody.useGravity = previousUseGravity;
                heldBody.linearDamping = previousLinearDamping;
                heldBody.angularDamping = previousAngularDamping;
            }
            heldBody = null;
            ClaimedBodies.Remove(releasedBody);
        }

        private void EndFurnitureInteraction()
        {
            if (heldFurniture == null) return;
            heldFurniture.EndInteraction("desktop");
            heldFurniture = null;
        }

        public void Configure(Camera camera, bool useSecondaryControls = false)
        {
            viewCamera = camera;
            secondaryPlayerControls = useSecondaryControls;
        }
    }
}
