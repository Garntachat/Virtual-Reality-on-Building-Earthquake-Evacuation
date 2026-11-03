using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Play-mode adapter for the currently selected playable House scene (New_House.unity).
    ///
    /// The saved House editor furniture is the visual source of truth.
    /// This component never rebuilds or replaces House furniture. It only aligns invisible gameplay
    /// proxies to the transforms already saved in the scene.
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class HouseSourceLayoutOverride : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            // The Menu's playable House scene is now New_House, which already
            // contains the user's editable physical furniture.
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name != MainMenuController.HouseScene)
                return;

            if (FindFirstObjectByType<HouseSourceLayoutOverride>() != null)
                return;

            GameObject host = new GameObject("CEVR_HouseSourceLayoutOverride");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<HouseSourceLayoutOverride>();
        }

        private void Start()
        {
            Scene scene = gameObject.scene;
            Transform gameplayRoot = GameObject.Find("CEVR_UniversalGameplay")?.transform;
            if (gameplayRoot == null)
            {
                Debug.LogError("CEVR House sync: CEVR_UniversalGameplay was not found.");
                return;
            }

            // House must not use the generic runtime furniture dresser. Tutorial still can.
            FurnitureSceneDressing generic = gameplayRoot.GetComponent<FurnitureSceneDressing>();
            if (generic != null) generic.enabled = false;

            Transform savedFurniture = HouseSavedSceneRuntimeBinder.FindSavedFurnitureRoot(scene);
            if (savedFurniture == null)
            {
                Debug.LogError(
                    "CEVR HOUSE SYNC STOPPED: no saved furniture hierarchy was found in " + scene.name + ".unity. " +
                    "Play mode will NOT generate replacement furniture, so the editor scene cannot be overwritten or visually diverge.");
                return;
            }

            GroundMotionPlayer motion = FindFirstObjectByType<GroundMotionPlayer>();
            SessionLogger logger = FindFirstObjectByType<SessionLogger>();

            if (!HouseSavedSceneRuntimeBinder.Bind(
                    scene,
                    savedFurniture,
                    motion,
                    logger,
                    out string report))
            {
                Debug.LogError("CEVR HOUSE SYNC FAILED: " + report);
                return;
            }

            if (gameplayRoot.GetComponent<QuakeLightFailures>() == null)
                gameplayRoot.gameObject.AddComponent<QuakeLightFailures>();

            Debug.Log(
                "CEVR HOUSE EDITOR -> PLAY SYNC PASS: Play is using the furniture saved in " + scene.name + ".unity. " +
                "No replacement visual layout was created. " + report);
        }
    }
}
