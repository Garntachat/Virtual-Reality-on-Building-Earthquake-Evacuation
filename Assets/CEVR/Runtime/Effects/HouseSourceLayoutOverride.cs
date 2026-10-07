using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Play-mode wrapper for the shared House layout.
    /// The actual furniture/decor construction lives in HouseLayoutSharedBuilder, which is also
    /// called by the Edit-mode preview. That guarantees Scene view and Play use the same builder.
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class HouseSourceLayoutOverride : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                scene.name.IndexOf("house", StringComparison.OrdinalIgnoreCase) < 0)
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
                Debug.LogError("CEVR House layout: CEVR_UniversalGameplay was not found.");
                return;
            }

            // House uses the shared builder exclusively. The generic dresser still handles Tutorial.
            FurnitureSceneDressing generic = gameplayRoot.GetComponent<FurnitureSceneDressing>();
            if (generic != null) generic.enabled = false;

            // The editor preview is built with the same builder. Hide it in Play so the runtime copy
            // is the only active set and receives runtime colliders / window behaviour.
            GameObject preview = GameObject.Find("CEVR_HouseEditorFurniturePreview");
            if (preview != null) preview.SetActive(false);

            GameObject oldRuntime = GameObject.Find("CEVR_HouseRuntimeLayout");
            if (oldRuntime != null) Destroy(oldRuntime);

            GameObject root = new GameObject("CEVR_HouseRuntimeLayout");
            root.transform.SetParent(gameplayRoot, false);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var context = new HouseLayoutSharedBuilder.BuildContext
            {
                scene = scene,
                root = root.transform,
                runtime = true,
                groundMotion = FindFirstObjectByType<GroundMotionPlayer>(),
                logger = FindFirstObjectByType<SessionLogger>()
            };
            HouseLayoutSharedBuilder.Build(context);

            bool valid = HouseLayoutSharedBuilder.ValidateBuiltLayout(scene, root.transform, out string report);
            if (valid)
                Debug.Log("CEVR HOUSE RUNTIME LAYOUT VALIDATION PASS.");
            else
                Debug.LogError("CEVR HOUSE RUNTIME LAYOUT VALIDATION FAILED: " + report);

            if (gameplayRoot.GetComponent<QuakeLightFailures>() == null)
                gameplayRoot.gameObject.AddComponent<QuakeLightFailures>();

            Debug.Log("CEVR HOUSE RUNTIME LAYOUT READY: built by the exact same HouseLayoutSharedBuilder used in Scene view.");
        }
    }
}
