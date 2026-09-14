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
        [SerializeField] private bool showViewButton = true;
        private Transform avatar;
        private GUIStyle buttonStyle;
        private bool returningToMenu;

        public bool IsThirdPerson { get; private set; }

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            BuildAvatar();
        }

        private void Update()
        {
            if (viewCamera == null) return;
            if (viewCamera.stereoEnabled)
            {
                IsThirdPerson = false;
                if (avatar != null) avatar.gameObject.SetActive(false);
                return;
            }
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) ToggleView();
            if (avatar != null) avatar.gameObject.SetActive(IsThirdPerson);
        }

        private void OnGUI()
        {
            if (!showViewButton || viewCamera == null || viewCamera.stereoEnabled) return;
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = new Color(0.45f, 1f, 0.68f);
            }
            string label = IsThirdPerson ? "FIRST-PERSON VIEW  (T)" : "THIRD-PERSON VIEW  (T)";
            if (GUI.Button(ViewButtonRect(), label, buttonStyle)) ToggleView();
            GUI.enabled = !returningToMenu;
            if (GUI.Button(MenuButtonRect(), "MAIN MENU", buttonStyle))
            {
                if (Application.CanStreamedLevelBeLoaded(MainMenuController.MenuScene))
                {
                    returningToMenu = true;
                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(MainMenuController.MenuScene);
                }
                else Debug.LogWarning("Main menu scene is not enabled in Build Settings.");
            }
            GUI.enabled = true;
            GUI.Label(new Rect(ViewButtonRect().x, ViewButtonRect().yMax + 2f, ViewButtonRect().width, 24f),
                "Press ESC to release the mouse", new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    normal = { textColor = new Color(0.85f, 0.9f, 0.96f) }
                });
        }

        public void ToggleView()
        {
            if (viewCamera == null || viewCamera.stereoEnabled) return;
            IsThirdPerson = !IsThirdPerson;
            if (avatar != null) avatar.gameObject.SetActive(IsThirdPerson);
            if (!IsThirdPerson)
            {
                CharacterController body = GetComponent<CharacterController>();
                viewCamera.transform.localPosition = Vector3.up * (body == null ? 1.63f : Mathf.Max(0.2f, body.height - 0.12f));
            }
        }

        public static bool IsPointerOverViewButton(Vector2 screenPosition)
        {
            Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return ViewButtonRect().Contains(guiPosition) || MenuButtonRect().Contains(guiPosition);
        }

        private static Rect MenuButtonRect() => new Rect(Mathf.Max(12f, Screen.width - 260f), 145f, 242f, 38f);

        private static Rect ViewButtonRect()
        {
            return new Rect(Mathf.Max(12f, Screen.width - 260f), 68f, 242f, 44f);
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
