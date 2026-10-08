using UnityEngine;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Play-mode interaction for the front door already saved in House.unity.
    /// The frame remains fixed while the panel and handle swing around a runtime hinge.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HouseDoorController : MonoBehaviour
    {
        private const string HingeName = "CEVR_FrontDoorHinge";
        private const string ButtonName = "HouseFrontDoorButton";

        [SerializeField] private float openAngle = -100f;
        [SerializeField] private float degreesPerSecond = 150f;

        private Transform hinge;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private bool targetOpen;
        private bool configured;

        public bool IsOpen => targetOpen;

        public void Configure(Material buttonBase, Material buttonFace)
        {
            if (configured) return;
            configured = true;

            Transform panel = FindChild("FrontDoor_Panel");
            Transform handle = FindChild("FrontDoor_Handle");
            if (panel == null)
            {
                Debug.LogWarning("CEVR House door: FrontDoor_Panel was not found.");
                enabled = false;
                return;
            }

            Transform existingHinge = FindDirectChild(HingeName);
            if (existingHinge == null)
            {
                GameObject hingeObject = new GameObject(HingeName);
                hinge = hingeObject.transform;
                hinge.SetParent(transform, false);
                hinge.localPosition = new Vector3(-0.575f, 0f, 0f);
                hinge.localRotation = Quaternion.identity;
                hinge.localScale = Vector3.one;
            }
            else
            {
                hinge = existingHinge;
            }

            if (panel.parent != hinge) panel.SetParent(hinge, true);
            if (handle != null && handle.parent != hinge) handle.SetParent(hinge, true);

            BoxCollider panelCollider = panel.GetComponent<BoxCollider>();
            if (panelCollider == null) panelCollider = panel.gameObject.AddComponent<BoxCollider>();
            panelCollider.center = Vector3.zero;
            panelCollider.size = Vector3.one;

            closedRotation = Quaternion.identity;
            openRotation = Quaternion.Euler(0f, openAngle, 0f);
            hinge.localRotation = closedRotation;
            targetOpen = false;

            BuildButton(buttonBase, buttonFace);
        }

        private void Update()
        {
            if (hinge == null) return;
            Quaternion targetRotation = targetOpen ? openRotation : closedRotation;
            hinge.localRotation = Quaternion.RotateTowards(
                hinge.localRotation,
                targetRotation,
                degreesPerSecond * Time.deltaTime);
        }

        public void ToggleDoor()
        {
            targetOpen = !targetOpen;
            GameplayAudioDirector.PlayCue(
                targetOpen ? GameplayAudioCue.FurnitureGrab : GameplayAudioCue.FurnitureRelease,
                0.16f,
                targetOpen ? 1.08f : 0.92f);
        }

        private void BuildButton(Material buttonBase, Material buttonFace)
        {
            if (FindDirectChild(ButtonName) != null) return;

            GameObject root = new GameObject(ButtonName);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0.90f, 1.15f, 0.18f);
            root.transform.localRotation = Quaternion.identity;

            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "DoorButton_Backplate";
            back.transform.SetParent(root.transform, false);
            back.transform.localPosition = Vector3.zero;
            back.transform.localScale = new Vector3(0.28f, 0.34f, 0.07f);
            Renderer backRenderer = back.GetComponent<Renderer>();
            if (backRenderer != null && buttonBase != null)
                backRenderer.sharedMaterial = buttonBase;
            Collider backCollider = back.GetComponent<Collider>();
            if (backCollider != null) backCollider.enabled = false;

            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "DoorButton_Press";
            face.transform.SetParent(root.transform, false);
            face.transform.localPosition = new Vector3(0f, 0f, 0.055f);
            face.transform.localScale = new Vector3(0.18f, 0.18f, 0.055f);
            Renderer faceRenderer = face.GetComponent<Renderer>();
            if (faceRenderer != null && buttonFace != null)
                faceRenderer.sharedMaterial = buttonFace;

            HouseDoorButton button = face.AddComponent<HouseDoorButton>();
            button.Configure(this);
        }

        private Transform FindChild(string exactName)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name == exactName) return t;
            return null;
        }

        private Transform FindDirectChild(string exactName)
        {
            foreach (Transform child in transform)
                if (child.name == exactName) return child;
            return null;
        }
    }

    [DisallowMultipleComponent]
    public sealed class HouseDoorButton : MonoBehaviour
    {
        private HouseDoorController door;
        private Vector3 restScale;
        private float releaseAt;

        public string Prompt =>
            door != null && door.IsOpen
                ? "CLOSE FRONT DOOR"
                : "OPEN FRONT DOOR";

        public void Configure(HouseDoorController controller)
        {
            door = controller;
            restScale = transform.localScale;
        }

        public void Press()
        {
            if (door == null) return;
            door.ToggleDoor();
            transform.localScale = restScale * 0.82f;
            releaseAt = Time.time + 0.12f;
        }

        private void Update()
        {
            if (releaseAt <= 0f || Time.time < releaseAt) return;
            transform.localScale = restScale;
            releaseAt = 0f;
        }
    }
}
