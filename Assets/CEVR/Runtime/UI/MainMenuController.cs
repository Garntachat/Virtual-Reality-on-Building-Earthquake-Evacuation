using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    // Desktop/lab-operator menu. Does not claim headset-controller UI support.
    public sealed class MainMenuController : MonoBehaviour
    {
        public const string MenuScene = "CEVR_MainMenu";
        public const string TutorialScene = "CEVR_ChulaEngineering_Tutorial";
        public const string HouseScene = "House";
        private bool loading;
        private string message = "Choose a scene to begin.";
        private Transform previewRoot;
        private GUIStyle heading, body, button;

        private void Awake()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var background = new GameObject("Menu Background", typeof(Camera));
            background.transform.SetParent(transform, false);
            Camera backgroundCamera = background.GetComponent<Camera>();
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundCamera.backgroundColor = new Color(0.025f, 0.045f, 0.075f);
            backgroundCamera.cullingMask = 0;
            backgroundCamera.depth = -10f;
            var cameraObject = new GameObject("Menu Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.075f);
            camera.transform.position = new Vector3(0f, 1f, -3.5f);
            camera.fieldOfView = 36f;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            backgroundCamera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.rect = new Rect(0.64f, 0.14f, 0.34f, 0.72f);
            var lightObject = new GameObject("Menu Key Light", typeof(Light));
            lightObject.transform.SetParent(transform, false);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (previewRoot != null)
            {
                previewRoot.gameObject.SetActive(false);
                Destroy(previewRoot.gameObject);
            }
            previewRoot = new GameObject("Student Appearance Preview").transform;
            previewRoot.SetParent(transform, false);
            StudentAvatar.Build(previewRoot, "StudentPreview");
        }

        private void OnGUI()
        {
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
                body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
                button = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            }
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1100f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1100f * scale) / 2f,
                (Screen.height - 720f * scale) / 2f, 0f), Quaternion.identity, Vector3.one * scale);
            GUI.Box(new Rect(25, 25, 650, 670), GUIContent.none);
            GUI.Label(new Rect(55, 55, 600, 50), "CEVR | EARTHQUAKE TRAINING", heading);
            GUI.Label(new Rect(55, 112, 570, 62),
                "Explore. Take cover during shaking. Evacuate when it stops.\nChoose your student outfit and training scene.", body);
            GUI.enabled = !loading;
            if (GUI.Button(new Rect(55, 195, 570, 62), "PLAY TUTORIAL", button)) BeginLoad(TutorialScene);
            if (GUI.Button(new Rect(55, 272, 570, 62), "PLAY HOUSE", button)) BeginLoad(HouseScene);
            GUI.Label(new Rect(55, 365, 570, 30), "STUDENT APPEARANCE", body);
            int selected = GUI.SelectionGrid(new Rect(55, 403, 570, 100), StudentAppearance.Selected,
                StudentAppearance.Names, 1, button);
            if (selected != StudentAppearance.Selected)
            {
                StudentAppearance.Select(selected);
                RefreshPreview();
            }
            if (GUI.Button(new Rect(55, 525, 570, 48), "EXIT", button)) ExitGame();
            GUI.enabled = true;
            GUI.Label(new Rect(55, 590, 570, 72), message, body);
            GUI.Label(new Rect(725, 630, 340, 64), "Your selected outfit carries into both scenes.\nDesktop controls: WASD / E / Z / T", body);
            GUI.matrix = previous;
        }

        private void BeginLoad(string scene)
        {
            if (loading) return;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                message = "Scene unavailable. Enable " + scene + " in File > Build Profiles > Scene List.";
                return;
            }
            loading = true;
            message = "Loading " + (scene == HouseScene ? "House" : "Tutorial") + "...";
            StartCoroutine(LoadSelectedScene(scene));
        }

        private IEnumerator LoadSelectedScene(string scene)
        {
            yield return null; // Render loading feedback before starting import/activation work.
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single); }
            catch (System.Exception exception)
            {
                message = "Could not load scene: " + exception.Message;
            }
            if (operation == null)
            {
                loading = false;
                if (message.StartsWith("Loading")) message = "Scene loading failed. Check the Unity Console.";
                yield break;
            }
            while (!operation.isDone) yield return null;
        }

        private static void ExitGame()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
