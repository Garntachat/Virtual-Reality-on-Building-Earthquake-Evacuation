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
        private AudioSource menuMusic;
        private AudioClip generatedSoundtrack;

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
            CreateMenuSoundtrack();
            RefreshPreview();
        }

        private void CreateMenuSoundtrack()
        {
            // Keep the repository self-contained: the menu music is generated at runtime instead
            // of relying on a missing binary audio asset. It is intentionally calm so gameplay
            // warning/earthquake sounds remain distinct after a scene is selected.
            const int sampleRate = 44100;
            const float duration = 16f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            // Four-bar ambient progression. Each chord crossfades into the next so the final loop
            // does not click. Frequencies are deliberately low/mid and mixed quietly.
            float[][] chords =
            {
                new[] { 146.83f, 220.00f, 293.66f }, // Dm
                new[] { 130.81f, 196.00f, 261.63f }, // C
                new[] { 116.54f, 174.61f, 233.08f }, // Bb
                new[] { 130.81f, 196.00f, 293.66f }  // C(add9)
            };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float barPosition = t / 4f;
                int chordIndex = Mathf.FloorToInt(barPosition) % chords.Length;
                int nextIndex = (chordIndex + 1) % chords.Length;
                float withinBar = barPosition - Mathf.Floor(barPosition);
                float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, withinBar));

                float current = ChordSample(chords[chordIndex], t);
                float next = ChordSample(chords[nextIndex], t);
                float pad = Mathf.Lerp(current, next, blend);

                float pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.25f * t - Mathf.PI * 0.5f);
                float air = Mathf.Sin(2f * Mathf.PI * 587.33f * t) * (0.01f + 0.012f * pulse);
                float bass = Mathf.Sin(2f * Mathf.PI * 73.42f * t) * 0.035f;
                float loopEnvelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(t / 0.15f, (duration - t) / 0.15f));
                samples[i] = Mathf.Clamp((pad * 0.10f + bass + air) * loopEnvelope, -0.22f, 0.22f);
            }

            generatedSoundtrack = AudioClip.Create("CEVR Main Menu Ambient", sampleCount, 1, sampleRate, false);
            generatedSoundtrack.SetData(samples, 0);

            var musicObject = new GameObject("Main Menu Soundtrack", typeof(AudioSource));
            musicObject.transform.SetParent(transform, false);
            menuMusic = musicObject.GetComponent<AudioSource>();
            menuMusic.clip = generatedSoundtrack;
            menuMusic.loop = true;
            menuMusic.playOnAwake = false;
            menuMusic.spatialBlend = 0f;
            menuMusic.volume = 0.42f;
            menuMusic.priority = 180;
            menuMusic.Play();
        }

        private static float ChordSample(float[] chord, float t)
        {
            float sample = 0f;
            for (int i = 0; i < chord.Length; i++)
            {
                float frequency = chord[i];
                sample += Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.52f;
                sample += Mathf.Sin(2f * Mathf.PI * frequency * 0.5f * t) * 0.18f;
            }
            return sample / chord.Length;
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
            StartCoroutine(FadeMusicAndLoad(scene));
        }

        private IEnumerator FadeMusicAndLoad(string scene)
        {
            const float fadeSeconds = 0.45f;
            float startVolume = menuMusic == null ? 0f : menuMusic.volume;
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (menuMusic != null)
                    menuMusic.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            yield return LoadSelectedScene(scene);
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
                if (menuMusic != null)
                {
                    menuMusic.volume = 0.42f;
                    if (!menuMusic.isPlaying) menuMusic.Play();
                }
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

        private void OnDestroy()
        {
            if (generatedSoundtrack != null) Destroy(generatedSoundtrack);
        }
    }
}
