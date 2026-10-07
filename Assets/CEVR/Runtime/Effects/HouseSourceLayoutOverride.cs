using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Runtime binder for the House.
    ///
    /// Preferred path: use the normal serialized HouseFurniture hierarchy saved in House.unity.
    /// Fallback path: if an older scene has not been baked yet, build the same shared layout at
    /// runtime so gameplay remains usable.
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

            // Tutorial still uses FurnitureSceneDressing. House does not.
            FurnitureSceneDressing generic = gameplayRoot.GetComponent<FurnitureSceneDressing>();
            if (generic != null) generic.enabled = false;

            GroundMotionPlayer motion = FindFirstObjectByType<GroundMotionPlayer>();
            SessionLogger logger = FindFirstObjectByType<SessionLogger>();

            GameObject baked = GameObject.Find("HouseFurniture");
            if (baked != null)
            {
                baked.SetActive(true);
                HouseLayoutSharedBuilder.PrepareBakedRuntime(scene, baked.transform, motion, logger);

                bool valid = HouseLayoutSharedBuilder.ValidateBuiltLayout(scene, baked.transform, out string report);
                string signature = HouseLayoutSharedBuilder.ComputeVisualSignature(baked.transform);
                if (valid)
                    Debug.Log("CEVR HOUSE BAKED LAYOUT VALIDATION PASS. visual-signature=" + signature);
                else
                    Debug.LogError("CEVR HOUSE BAKED LAYOUT VALIDATION FAILED: " + report + "; visual-signature=" + signature);

                if (gameplayRoot.GetComponent<QuakeLightFailures>() == null)
                    gameplayRoot.gameObject.AddComponent<QuakeLightFailures>();

                Debug.Log("CEVR HOUSE USING SERIALIZED HouseFurniture FROM House.unity. No runtime visual rebuild.");
                return;
            }

            Debug.LogWarning(
                "CEVR HouseFurniture is not baked into House.unity yet. Falling back to runtime generation. " +
                "Use CEVR > House > Bake Play Layout Into House Scene.");

            GameObject root = new GameObject("CEVR_HouseRuntimeLayout");
            root.transform.SetParent(gameplayRoot, false);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var context = new HouseLayoutSharedBuilder.BuildContext
            {
                scene = scene,
                root = root.transform,
                runtime = true,
                groundMotion = motion,
                logger = logger
            };
            HouseLayoutSharedBuilder.Build(context);

            bool fallbackValid = HouseLayoutSharedBuilder.ValidateBuiltLayout(scene, root.transform, out string fallbackReport);
            string fallbackSignature = HouseLayoutSharedBuilder.ComputeVisualSignature(root.transform);
            if (fallbackValid)
                Debug.Log("CEVR HOUSE FALLBACK LAYOUT VALIDATION PASS. visual-signature=" + fallbackSignature);
            else
                Debug.LogError("CEVR HOUSE FALLBACK LAYOUT VALIDATION FAILED: " + fallbackReport +
                               "; visual-signature=" + fallbackSignature);

            if (gameplayRoot.GetComponent<QuakeLightFailures>() == null)
                gameplayRoot.gameObject.AddComponent<QuakeLightFailures>();
        }
    }
}
