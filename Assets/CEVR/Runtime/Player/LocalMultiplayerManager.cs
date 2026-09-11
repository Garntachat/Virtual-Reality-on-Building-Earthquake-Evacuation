using UnityEngine;
using UnityEngine.InputSystem;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    public sealed class LocalMultiplayerManager : MonoBehaviour
    {
        [SerializeField] private Camera primaryCamera;
        [SerializeField] private Transform primaryPlayer;
        private GameObject secondPlayer;
        private GUIStyle style;

        public bool IsSecondPlayerJoined => secondPlayer != null;

        private void Update()
        {
            if (Keyboard.current == null || primaryCamera == null || primaryCamera.stereoEnabled) return;
            if (!Keyboard.current.f2Key.wasPressedThisFrame) return;
            if (secondPlayer == null) JoinSecondPlayer();
            else LeaveSecondPlayer();
        }

        public void Configure(Camera camera, Transform player)
        {
            primaryCamera = camera;
            primaryPlayer = player;
        }

        private void JoinSecondPlayer()
        {
            if (primaryPlayer == null) return;
            secondPlayer = new GameObject("LocalPlayer2");
            secondPlayer.transform.position = primaryPlayer.position + primaryPlayer.right * 1.2f;
            CharacterController controller = secondPlayer.AddComponent<CharacterController>();
            controller.height = 1.75f;
            controller.radius = 0.24f;
            controller.center = new Vector3(0f, 0.875f, 0f);
            secondPlayer.AddComponent<PlayerHealth>();

            StudentAvatar.Build(secondPlayer.transform, "Player2Avatar");

            GameObject cameraObject = new GameObject("Player2Camera");
            cameraObject.transform.SetParent(secondPlayer.transform, true);
            Camera secondCamera = cameraObject.AddComponent<Camera>();
            secondCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            secondCamera.depth = primaryCamera.depth;
            secondCamera.nearClipPlane = 0.05f;
            primaryCamera.rect = new Rect(0f, 0f, 0.5f, 1f);
            LocalSecondPlayerController movement = secondPlayer.AddComponent<LocalSecondPlayerController>();
            movement.Configure(secondCamera);
            DesktopGrabInteractor grab = secondPlayer.AddComponent<DesktopGrabInteractor>();
            grab.Configure(secondCamera, true);
        }

        private void LeaveSecondPlayer()
        {
            if (secondPlayer != null) Destroy(secondPlayer);
            secondPlayer = null;
            if (primaryCamera != null) primaryCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }

        private void OnDisable() => LeaveSecondPlayer();

        private void OnGUI()
        {
            if (primaryCamera == null || primaryCamera.stereoEnabled) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperRight,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
                style.normal.textColor = new Color(0.78f, 0.9f, 1f);
            }
            string message = secondPlayer == null
                ? "F2: JOIN LOCAL PLAYER 2"
                : "P2: IJKL MOVE | U/O TURN | RIGHT SHIFT USE | F2 LEAVE";
            GUI.Label(new Rect(Screen.width * 0.52f, 12f, Screen.width * 0.46f, 44f), message, style);
        }
    }
}
