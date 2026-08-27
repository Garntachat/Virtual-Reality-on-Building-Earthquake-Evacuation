using UnityEngine;
using UnityEngine.InputSystem;

namespace ChulaEarthquakeVR
{
    public sealed class DesktopGrabInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.5f)] private float maximumGrabDistance = 4f;
        [SerializeField, Min(0.5f)] private float holdDistance = 1.4f;
        [SerializeField, Min(1f)] private float followStrength = 18f;

        private Rigidbody heldBody;
        private MovableFurniture heldFurniture;
        private bool previousUseGravity;
        private float previousLinearDamping;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void OnDisable() => ReleaseHeldItem(true);

        private void Update()
        {
            if (viewCamera == null || Keyboard.current == null || Mouse.current == null) return;

            bool interactPressed = Keyboard.current.eKey.wasPressedThisFrame ||
                                   Mouse.current.leftButton.wasPressedThisFrame;
            if (interactPressed)
            {
                if (heldBody == null) TryGrab();
                else ReleaseHeldItem(true);
            }

            if (heldBody != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ReleaseHeldItem(true);
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
            heldBody.linearVelocity = (target - heldBody.position) * followStrength;
            heldBody.angularVelocity *= 0.8f;
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
            body.useGravity = false;
            body.linearDamping = 10f;
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
