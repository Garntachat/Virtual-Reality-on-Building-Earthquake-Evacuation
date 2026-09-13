using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChulaEarthquakeVR
{
    public static class RuntimeStageRepair
    {
        public static bool EnsurePlayableStage()
        {
            GeneratedStageInfo stageInfo = UnityEngine.Object.FindFirstObjectByType<GeneratedStageInfo>();
            bool legacyScene = stageInfo == null || !stageInfo.IsCurrent;
            var repairs = new List<string>();
            RepairLegacyText(repairs);
            RepairHud(repairs, legacyScene);
            EnsureDesktopGrabInteractor(repairs);
            EnsureMovableChairs(repairs);
            if (TutorialVisualPolish.EnsureApplied())
                repairs.Add("applied final lighting, materials, HUD, and wayfinding polish");
            if (legacyScene) EnsureCurrentStageInfo(stageInfo, repairs);

            if (repairs.Count == 0) return false;
            string message = "CEVR prepared the tutorial scene at runtime: " + string.Join(", ", repairs);
            if (legacyScene) Debug.LogWarning(message);
            else Debug.Log(message);
            return repairs.Count > 0;
        }

        private static void RepairLegacyText(List<string> repairs)
        {
            GameObject disclaimer = GameObject.Find("StageDisclaimer");
            TextMesh disclaimerText = disclaimer == null ? null : disclaimer.GetComponent<TextMesh>();
            if (disclaimer != null && disclaimer.activeSelf &&
                (disclaimerText == null || disclaimerText.characterSize > 0.06f))
            {
                disclaimer.SetActive(false);
                repairs.Add("hidden broken wall disclaimer");
            }

            GameObject exit = GameObject.Find("ExitSign");
            if (exit == null) return;
            TextMesh text = exit.GetComponent<TextMesh>();
            bool brokenRotation = Quaternion.Angle(exit.transform.rotation, Quaternion.Euler(0f, 180f, 0f)) > 1f;
            bool brokenScale = text != null && text.characterSize > 0.06f;
            if (!brokenRotation && !brokenScale) return;

            exit.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            if (text != null)
            {
                text.fontSize = 48;
                text.characterSize = 0.035f;
            }
            repairs.Add("corrected exit sign");
        }

        private static void RepairHud(List<string> repairs, bool legacyScene)
        {
            TutorialHud hud = UnityEngine.Object.FindFirstObjectByType<TutorialHud>();
            Canvas canvas = hud == null ? null : hud.GetComponent<Canvas>();
            if (canvas == null) return;

            DesktopDebugRig desktop = UnityEngine.Object.FindFirstObjectByType<DesktopDebugRig>();
            RectTransform rect = canvas.GetComponent<RectTransform>();
            if (desktop != null && desktop.isActiveAndEnabled)
            {
                bool changed = canvas.renderMode != RenderMode.ScreenSpaceOverlay ||
                               Quaternion.Angle(rect.localRotation, Quaternion.identity) > 0.1f ||
                               Vector3.Distance(rect.localScale, Vector3.one) > 0.01f;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                rect.anchoredPosition3D = Vector3.zero;
                if (changed) repairs.Add("converted HUD to readable desktop overlay");
                return;
            }

            if (!legacyScene) return;
            canvas.renderMode = RenderMode.WorldSpace;
            rect.position = new Vector3(0f, 2.15f, 5.5f);
            rect.rotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.0025f;
            repairs.Add("corrected world-space HUD orientation");
        }

        private static void EnsureDesktopGrabInteractor(List<string> repairs = null)
        {
            DesktopDebugRig desktop = UnityEngine.Object.FindFirstObjectByType<DesktopDebugRig>();
            if (desktop == null) return;
            DesktopGrabInteractor grab = desktop.GetComponent<DesktopGrabInteractor>();
            if (grab != null) return;
            Camera camera = desktop.GetComponentInChildren<Camera>();
            desktop.gameObject.AddComponent<DesktopGrabInteractor>().Configure(camera);
            repairs?.Add("attached desktop grab controls");
        }

        private static void EnsureMovableChairs(List<string> repairs)
        {
            GroundMotionPlayer motion = UnityEngine.Object.FindFirstObjectByType<GroundMotionPlayer>();
            SessionLogger logger = UnityEngine.Object.FindFirstObjectByType<SessionLogger>();
            GameObject dynamicParent = GameObject.Find("DynamicProps");
            int created = 0;
            created += EnsureMovableChair("MovableChair_StrongTableApproach", "chair-strong-table-01",
                new Vector3(2.5f, 0f, 2.25f), 0f, dynamicParent, motion, logger);
            created += EnsureMovableChair("MovableChair_LabBenchNorth", "chair-lab-north-01",
                new Vector3(-3.8f, 0f, 2.6f), 180f, dynamicParent, motion, logger);
            created += EnsureMovableChair("MovableChair_LabBenchSouth", "chair-lab-south-01",
                new Vector3(-3.8f, 0f, -0.6f), 180f, dynamicParent, motion, logger);
            created += EnsureMovableChair("MovableChair_Spare", "chair-spare-01",
                new Vector3(0.5f, 0f, -2.2f), 90f, dynamicParent, motion, logger);
            if (created > 0) repairs.Add($"created {created} missing grabbable chair(s)");
        }

        private static int EnsureMovableChair(
            string objectName, string furnitureId, Vector3 position, float yaw,
            GameObject dynamicParent, GroundMotionPlayer motion, SessionLogger logger)
        {
            if (GameObject.Find(objectName) != null) return 0;

            var chair = new GameObject(objectName);
            if (dynamicParent != null) chair.transform.SetParent(dynamicParent.transform);
            chair.transform.position = position;
            chair.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            ChairPart("ChairSeat_Accent", new Vector3(0f, 0.48f, 0f), new Vector3(0.9f, 0.12f, 0.9f), null, chair.transform);
            foreach (float x in new[] { -0.35f, 0.35f })
            foreach (float z in new[] { -0.35f, 0.35f })
                ChairPart("ChairLeg", new Vector3(x, 0.23f, z), new Vector3(0.1f, 0.46f, 0.1f), null, chair.transform);
            ChairPart("ChairBack_Accent", new Vector3(0f, 0.96f, 0.4f), new Vector3(0.72f, 0.34f, 0.1f), null, chair.transform);
            ChairPart("ChairBackPost_Left", new Vector3(-0.35f, 0.83f, 0.4f), new Vector3(0.08f, 0.72f, 0.08f), null, chair.transform);
            ChairPart("ChairBackPost_Right", new Vector3(0.35f, 0.83f, 0.4f), new Vector3(0.08f, 0.72f, 0.08f), null, chair.transform);

            Rigidbody body = chair.AddComponent<Rigidbody>();
            body.mass = 7.5f;
            body.linearDamping = 2.5f;
            body.angularDamping = 4f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationZ;
            chair.AddComponent<InertialRigidbody>().Configure(motion, 0.65f, false);
            chair.AddComponent<MovableFurniture>().Configure(furnitureId, logger, motion);
            TryAddXrGrabInteractable(chair);
            return 1;
        }

        private static void EnsureCurrentStageInfo(GeneratedStageInfo stageInfo, List<string> repairs)
        {
            if (stageInfo == null)
            {
                GameObject systems = GameObject.Find("GameplaySystems");
                stageInfo = (systems ?? new GameObject("CEVR_RuntimeStageInfo")).AddComponent<GeneratedStageInfo>();
            }
            stageInfo.ConfigureCurrent();
            repairs.Add("updated generated-stage version");
        }

        private static void ChairPart(
            string name, Vector3 localPosition, Vector3 localScale, Material material, Transform parent)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void TryAddXrGrabInteractable(GameObject target)
        {
            Type type = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (type != null && target.GetComponent(type) == null) target.AddComponent(type);
        }
    }
}
