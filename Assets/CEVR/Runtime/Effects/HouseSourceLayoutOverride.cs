using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Play-mode adapter for House.unity.
    ///
    /// The saved House editor furniture is the visual source of truth.
    /// This component never rebuilds or replaces House furniture. It only aligns invisible gameplay
    /// proxies to the transforms already saved in the scene.
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class HouseSourceLayoutOverride : MonoBehaviour
    {
        private Transform fixedSofaPillows;
        private Transform fixedSofa;
        private Vector3 fixedPillowLocalPosition;
        private Quaternion fixedPillowLocalRotation;
        private Vector3 fixedPillowLocalScale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            // New_House is a separate standalone Rigidbody test scene.
            // Only the playable House scene has the saved HouseFurniture sync.
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
                    "CEVR HOUSE SYNC STOPPED: no saved House furniture hierarchy was found in House.unity. " +
                    "Play mode will NOT generate replacement furniture, so the editor scene cannot be overwritten or visually diverge.");
                return;
            }

            GroundMotionPlayer motion = FindFirstObjectByType<GroundMotionPlayer>();
            SessionLogger logger = FindFirstObjectByType<SessionLogger>();

            bool syncSucceeded = HouseSavedSceneRuntimeBinder.Bind(
                scene, savedFurniture, motion, logger, out string report);

            // Decorative pillow lockdown is intentionally separate from the
            // usual furniture physics sync. It runs even if the visual parity
            // check reports a problem, so the saved pillow group never launches.
            LockSofaPillows(savedFurniture);

            if (!syncSucceeded)
            {
                Debug.LogError("CEVR HOUSE SYNC FAILED: " + report);
                return;
            }

            if (gameplayRoot.GetComponent<QuakeLightFailures>() == null)
                gameplayRoot.gameObject.AddComponent<QuakeLightFailures>();

            Debug.Log(
                "CEVR HOUSE EDITOR -> PLAY SYNC PASS: Play is using the furniture already saved in House.unity. " +
                "No replacement visual layout was created. " + report);
        }

        private void LockSofaPillows(Transform savedFurniture)
        {
            if (gameObject.scene.name != MainMenuController.HouseScene || savedFurniture == null)
                return;

            Transform sofa = null;
            Transform pillows = null;
            foreach (Transform item in savedFurniture.GetComponentsInChildren<Transform>(true))
            {
                if (item.name == "HouseLayout_Sofa") sofa = item;
                else if (item.name == "HouseLayout_Sofa_Pillows") pillows = item;
            }
            if (sofa == null || pillows == null) return;

            // Merely setting a CHILD Rigidbody kinematic is not enough: Unity
            // physics/interpolation can continue to move its world pose when
            // the parent sofa Rigidbody moves at earthquake onset. This is
            // decorative art, so remove its separate physics in Play Mode.
            InertialRigidbody shaker = pillows.GetComponent<InertialRigidbody>();
            if (shaker != null)
            {
                shaker.enabled = false;
                Destroy(shaker);
            }
            foreach (DecorativeQuakeResponse response in
                     pillows.GetComponentsInChildren<DecorativeQuakeResponse>(true))
            {
                response.enabled = false;
                Destroy(response);
            }
            foreach (VisualQuakeSway sway in
                     pillows.GetComponentsInChildren<VisualQuakeSway>(true))
            {
                sway.enabled = false;
                Destroy(sway);
            }
            Rigidbody body = pillows.GetComponent<Rigidbody>();
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
                body.useGravity = false;
                body.detectCollisions = false;
                Destroy(body);
            }
            foreach (Collider collider in pillows.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Destroy(collider);
            }

            pillows.SetParent(sofa, true);
            fixedSofa = sofa;
            fixedSofaPillows = pillows;
            fixedPillowLocalPosition = pillows.localPosition;
            fixedPillowLocalRotation = pillows.localRotation;
            fixedPillowLocalScale = pillows.localScale;
            Debug.Log("CEVR House: decorative sofa pillows locked to sofa; independent Rigidbody and quake responses removed in Play Mode.");
        }

        private void LateUpdate()
        {
            if (fixedSofaPillows == null || fixedSofa == null) return;
            if (fixedSofaPillows.parent != fixedSofa)
                fixedSofaPillows.SetParent(fixedSofa, false);
            // Enforce the same local pose after each rendered frame, even if a
            // future scene effect tries to animate the decorative pillow root.
            fixedSofaPillows.localPosition = fixedPillowLocalPosition;
            fixedSofaPillows.localRotation = fixedPillowLocalRotation;
            fixedSofaPillows.localScale = fixedPillowLocalScale;
        }
    }
}
