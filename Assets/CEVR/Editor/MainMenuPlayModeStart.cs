using UnityEditor;
using UnityEditor.SceneManagement;

namespace ChulaEarthquakeVR.Editor
{
    /// <summary>
    /// Makes the CEVR main menu the entry point even when a developer presses Play
    /// while House or Tutorial is currently open in the editor. Player builds already
    /// use EditorBuildSettings scene 0; this closes the editor-only gap.
    /// </summary>
    [InitializeOnLoad]
    internal static class MainMenuPlayModeStart
    {
        private const string MainMenuPath = "Assets/CEVR/Generated/Scenes/CEVR_MainMenu.unity";

        static MainMenuPlayModeStart()
        {
            Apply();
            EditorApplication.projectChanged -= Apply;
            EditorApplication.projectChanged += Apply;
        }

        private static void Apply()
        {
            SceneAsset menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
            if (menu == null)
            {
                UnityEngine.Debug.LogWarning("CEVR main menu scene is missing; Play Mode start scene was not changed.");
                return;
            }

            if (EditorSceneManager.playModeStartScene != menu)
                EditorSceneManager.playModeStartScene = menu;
        }
    }
}
