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

            CharacterController stance = GetComponent<CharacterController>();
            Vector3 head = transform.position + Vector3.up * (stance == null ? 1.63f : stance.height - 0.12f);
            Vector3 target = head + viewCamera.transform.forward * holdDistance;
            if (heldPillow != null)
                target = head + transform.forward * 0.35f + Vector3.up * 0.35f;
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
            float width = Mathf.Min(500f, viewport.width * Screen.width - 24f);
            GUI.Box(new Rect(centerX - width * 0.5f, bottomY - 72f, width, 54f), GUIContent.none);
            GUI.backgroundColor = previousBackground;
            GUI.Label(new Rect(centerX - width * 0.5f + 8f, bottomY - 67f, width - 16f, 44f),
                InteractionPrompt(), promptStyle);
        }

        private bool RaycastTarget(Ray ray, out RaycastHit target)
        {
            target = default;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit hit in Physics.RaycastAll(ray, maximumGrabDistance + Vector3.Distance(ray.origin, transform.position),
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                target = hit;
            }
            // Pick the nearest obstruction first. A wall outside player reach must still
            // block a camera ray instead of exposing a grabbable object behind it.
            return !float.IsPositiveInfinity(nearest) &&
                   Vector3.Distance(target.point, transform.position) <= maximumGrabDistance;
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
                    : "WASD: MOVE   E: PICK UP / RELEASE   Z: CRAWL";
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
                fontSize = 15,
                wordWrap = true,
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
            GameplayAudioDirector.PlayCue(heldFurniture != null
                ? GameplayAudioCue.FurnitureGrab
                : heldPillow != null ? GameplayAudioCue.PillowGrab : GameplayAudioCue.FurnitureGrab,
                heldFurniture != null ? 0.28f : 0.18f);
        }

        private void ReleaseHeldItem(bool restorePhysics)
        {
            if (heldBody == null) return;
            Rigidbody releasedBody = heldBody;
            GameplayAudioDirector.PlayCue(heldFurniture != null
                ? GameplayAudioCue.FurnitureRelease
                : heldPillow != null ? GameplayAudioCue.PillowGrab : GameplayAudioCue.FurnitureRelease,
                heldFurniture != null ? 0.24f : 0.14f, 0.92f);
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
