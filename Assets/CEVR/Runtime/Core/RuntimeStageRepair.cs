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
            EnsureMovableChair(repairs);
            if (legacyScene) EnsureCurrentStageInfo(stageInfo, repairs);

            if (repairs.Count == 0) return false;
            Debug.LogWarning("CEVR repaired the generated scene at runtime: " + string.Join(", ", repairs));
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

        private static void EnsureMovableChair(List<string> repairs)
        {
            if (UnityEngine.Object.FindFirstObjectByType<MovableFurniture>() != null) return;

            GroundMotionPlayer motion = UnityEngine.Object.FindFirstObjectByType<GroundMotionPlayer>();
            SessionLogger logger = UnityEngine.Object.FindFirstObjectByType<SessionLogger>();
            GameObject dynamicParent = GameObject.Find("DynamicProps");
            var chair = new GameObject("MovableChair_StrongTableApproach");
            if (dynamicParent != null) chair.transform.SetParent(dynamicParent.transform);
            chair.transform.position = new Vector3(2.5f, 0f, 1.8f);

            Material accent = RuntimeMaterial("CEVR_RuntimeChairAccent", new Color(0.84f, 0.16f, 0.42f));
            Material frame = RuntimeMaterial("CEVR_RuntimeChairFrame", new Color(0.16f, 0.19f, 0.22f));
            ChairPart("ChairSeat_Accent", new Vector3(0f, 0.48f, 0f), new Vector3(0.9f, 0.12f, 0.9f), accent, chair.transform);
            foreach (float x in new[] { -0.35f, 0.35f })
            foreach (float z in new[] { -0.35f, 0.35f })
                ChairPart("ChairLeg", new Vector3(x, 0.23f, z), new Vector3(0.1f, 0.46f, 0.1f), frame, chair.transform);
            ChairPart("ChairBack_Accent", new Vector3(0f, 0.96f, 0.4f), new Vector3(0.72f, 0.34f, 0.1f), accent, chair.transform);
            ChairPart("ChairBackPost_Left", new Vector3(-0.35f, 0.83f, 0.4f), new Vector3(0.08f, 0.72f, 0.08f), frame, chair.transform);
            ChairPart("ChairBackPost_Right", new Vector3(0.35f, 0.83f, 0.4f), new Vector3(0.08f, 0.72f, 0.08f), frame, chair.transform);

            Rigidbody body = chair.AddComponent<Rigidbody>();
            body.mass = 7.5f;
            body.linearDamping = 2.5f;
            body.angularDamping = 4f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationZ;
            chair.AddComponent<InertialRigidbody>().Configure(motion, 0.65f, false);
            chair.AddComponent<MovableFurniture>().Configure("chair-strong-table-01", logger, motion);
            TryAddXrGrabInteractable(chair);
            repairs.Add("created missing pink movable chair");
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
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material RuntimeMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard") ??
                            Shader.Find("Sprites/Default") ??
                            Shader.Find("UI/Default");
            if (shader == null)
                throw new InvalidOperationException("CEVR could not find a built-in shader for the runtime chair.");
            return new Material(shader) { name = name, color = color };
        }

        private static void TryAddXrGrabInteractable(GameObject target)
        {
            Type type = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (type != null && target.GetComponent(type) == null) target.AddComponent(type);
        }
    }
}
