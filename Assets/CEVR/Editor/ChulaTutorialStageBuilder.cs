using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChulaEarthquakeVR.Editor
{
    public static class ChulaTutorialStageBuilder
    {
        private const string GeneratedRoot = "Assets/CEVR/Generated";
        private const string ScenePath = GeneratedRoot + "/Scenes/CEVR_ChulaEngineering_Tutorial.unity";
        private const string ConfigPath = GeneratedRoot + "/Data/CEVR_TutorialScenario.asset";
        private static readonly List<Light> LabLights = new List<Light>();

        [MenuItem("Tools/CEVR/1. Build Chula Engineering Tutorial Stage")]
        public static void BuildStage()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build CEVR Tutorial Stage",
                    "This creates or replaces the generated tutorial scene. Source scripts and hand-authored assets are not touched.",
                    "Build", "Cancel")) return;

            EnsureFolders();
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.externalVersionControl = "Visible Meta Files";
            LabLights.Clear();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var environment = new GameObject("Environment_ChulaEngineeringInspired");
            var gameplay = new GameObject("GameplaySystems");
            var dynamicProps = new GameObject("DynamicProps");
            var tutorialObjects = new GameObject("TutorialObjects");
            var zones = new GameObject("SafetyZones");

            Material concrete = MaterialAsset("Concrete", new Color(0.62f, 0.64f, 0.66f));
            Material wall = MaterialAsset("WallWhite", new Color(0.89f, 0.90f, 0.91f));
            Material pink = MaterialAsset("ChulaPinkAccent", new Color(0.84f, 0.16f, 0.42f));
            Material dark = MaterialAsset("BenchDark", new Color(0.16f, 0.19f, 0.22f));
            Material wood = MaterialAsset("BenchWood", new Color(0.48f, 0.30f, 0.18f));
            Material green = MaterialAsset("SafetyGreen", new Color(0.12f, 0.58f, 0.25f));
            Material yellow = MaterialAsset("HazardYellow", new Color(0.95f, 0.68f, 0.08f));
            Material glass = MaterialAsset("WindowBlue", new Color(0.32f, 0.55f, 0.72f));

            BuildEnvironment(environment.transform, concrete, wall, pink, dark, glass, green);
            GroundMotionPlayer motion = gameplay.AddComponent<GroundMotionPlayer>();
            SessionLogger logger = gameplay.AddComponent<SessionLogger>();
            TutorialTaskSequence taskSequence = gameplay.AddComponent<TutorialTaskSequence>();
            HazardDirector hazardDirector = gameplay.AddComponent<HazardDirector>();
            TutorialHud hud = BuildHud(gameplay.transform);
            StageEffectsController effects = gameplay.AddComponent<StageEffectsController>();
            AudioSource rumble = gameplay.AddComponent<AudioSource>();
            gameplay.AddComponent<ProceduralRumbleGenerator>();

            PlayerHealth health = BuildDesktopPlayer(out Camera playerCamera);
            PoseTelemetrySampler sampler = gameplay.AddComponent<PoseTelemetrySampler>();
            sampler.Configure(logger, playerCamera.transform, null, null);

            CoverZone coverZone = BuildStrongTableAndCoverZone(
                environment.transform, dynamicProps.transform, zones.transform,
                new Vector3(2.5f, 0f, 0.8f), wood, dark, motion, logger);
            ExitAssemblyZone assembly = BuildAssemblyZone(zones.transform, green);

            List<TutorialTask> tasks = BuildTutorialTasks(
                tutorialObjects.transform, dynamicProps.transform, motion, dark, green, yellow);
            taskSequence.Configure(tasks);

            List<Rigidbody> hazards = BuildHazards(dynamicProps.transform, motion, yellow, dark);
            hazards.Add(BuildTopplingCabinet(dynamicProps.transform, motion, dark, yellow));
            BuildHangingLights(dynamicProps.transform, motion, concrete);
            hazardDirector.Configure(hazards);

            TutorialScenarioConfig config = CreateOrLoadConfig();
            config.ConfigureForBuilder(StudyMode.Training);
            EditorUtility.SetDirty(config);

            GameFlowController flow = gameplay.AddComponent<GameFlowController>();
            flow.Configure(config, motion, taskSequence, hazardDirector, health, coverZone, assembly, hud, logger);
            EmergencyStopInput emergencyStop = gameplay.AddComponent<EmergencyStopInput>();
            emergencyStop.Configure(flow);
            effects.Configure(motion, LabLights.ToArray(), rumble);

            Selection.activeGameObject = gameplay;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CEVR tutorial stage generated at {ScenePath}. Press Play for desktop testing.");
            EditorUtility.DisplayDialog(
                "Stage built",
                "Open the generated scene and press Play. Desktop controls: WASD, mouse look, E or left click to grab/drop, C/Ctrl to crouch, F12 or Backspace for emergency stop.",
                "OK");
        }

        [MenuItem("Tools/CEVR/2. Validate Open Tutorial Scene")]
        public static void ValidateScene()
        {
            int errors = 0;
            errors += CountError(UnityEngine.Object.FindObjectsByType<GameFlowController>(FindObjectsSortMode.None).Length == 1,
                "Exactly one GameFlowController is required.");
            errors += CountError(UnityEngine.Object.FindObjectsByType<GroundMotionPlayer>(FindObjectsSortMode.None).Length == 1,
                "Exactly one GroundMotionPlayer is required.");
            errors += CountError(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length >= 1,
                "At least one camera is required.");
            errors += CountError(UnityEngine.Object.FindObjectsByType<CoverZone>(FindObjectsSortMode.None).Length >= 1,
                "At least one CoverZone is required.");
            errors += CountError(UnityEngine.Object.FindObjectsByType<ExitAssemblyZone>(FindObjectsSortMode.None).Length == 1,
                "Exactly one ExitAssemblyZone is required.");
            errors += CountError(UnityEngine.Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).Length == 1,
                "Exactly one active PlayerHealth is required.");
            errors += CountError(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1,
                "Exactly one active AudioListener is required.");
            errors += CountError(UnityEngine.Object.FindObjectsByType<TutorialTask>(FindObjectsSortMode.None).Length == 2,
                "The tutorial stage requires exactly two normal-activity tasks.");
            errors += CountError(UnityEngine.Object.FindObjectsByType<FallingHazard>(FindObjectsSortMode.None).Length == 5,
                "The generated stage requires four overhead hazards and one cabinet hazard.");
            errors += CountError(UnityEngine.Object.FindObjectsByType<MovableFurniture>(FindObjectsSortMode.None).Length >= 1,
                "At least one movable furniture obstacle is required.");
            foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                errors += CountError(camera.GetComponentInParent<InertialRigidbody>() == null,
                    $"Camera '{camera.name}' must never be parented under an inertial quake object.");
            string message = errors == 0
                ? "Scene structure passed. Continue with Console, Test Runner, OpenXR Project Validation, and headset build gates."
                : $"Scene has {errors} structural error(s). Check the Console.";
            EditorUtility.DisplayDialog("CEVR Scene Validation", message, "OK");
        }

        private static int CountError(bool condition, string message)
        {
            if (condition) return 0;
            Debug.LogError(message);
            return 1;
        }

        private static void BuildEnvironment(
            Transform parent, Material concrete, Material wall, Material pink,
            Material dark, Material glass, Material green)
        {
            Cube("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(20f, 0.2f, 12f), concrete, parent, true);
            Cube("Ceiling", new Vector3(0f, 3.6f, 0f), new Vector3(20f, 0.15f, 12f), wall, parent, true);
            Cube("BackWall", new Vector3(0f, 1.8f, 6f), new Vector3(20f, 3.6f, 0.2f), wall, parent, true);
            Cube("LeftWall", new Vector3(-10f, 1.8f, 0f), new Vector3(0.2f, 3.6f, 12f), wall, parent, true);
            Cube("RightWall", new Vector3(10f, 1.8f, 0f), new Vector3(0.2f, 3.6f, 12f), wall, parent, true);
            Cube("FrontWallLeft", new Vector3(-3f, 1.8f, -6f), new Vector3(14f, 3.6f, 0.2f), wall, parent, true);
            Cube("FrontWallRight", new Vector3(8f, 1.8f, -6f), new Vector3(4f, 3.6f, 0.2f), wall, parent, true);
            Cube("PinkBandBack", new Vector3(0f, 2.8f, 5.85f), new Vector3(19.8f, 0.28f, 0.08f), pink, parent, true);

            for (int i = 0; i < 4; i++)
            {
                float x = -7.5f + i * 5f;
                Cube($"ConcreteColumn_{i + 1}", new Vector3(x, 1.8f, 5.6f), new Vector3(0.45f, 3.6f, 0.45f), concrete, parent, true);
            }
            for (int i = 0; i < 3; i++)
            {
                float x = -5f + i * 5f;
                Cube($"Window_{i + 1}", new Vector3(x, 2f, 5.72f), new Vector3(3.7f, 1.5f, 0.08f), glass, parent, true);
            }
            Cube("ExitDoorFrameLeft", new Vector3(4f, 1.25f, -5.85f), new Vector3(0.18f, 2.5f, 0.25f), dark, parent, true);
            Cube("ExitDoorFrameRight", new Vector3(6f, 1.25f, -5.85f), new Vector3(0.18f, 2.5f, 0.25f), dark, parent, true);
            Cube("ExitDoorFrameTop", new Vector3(5f, 2.5f, -5.85f), new Vector3(2.2f, 0.18f, 0.25f), green, parent, true);

            Cube("OutdoorWalkway", new Vector3(5f, -0.1f, -9f), new Vector3(8f, 0.2f, 6f), concrete, parent, true);
            Cube("AssemblyMarker", new Vector3(5f, 0.015f, -10f), new Vector3(4f, 0.03f, 3f), green, parent, true);

            BuildLabBench(parent, new Vector3(-4f, 0f, 1.6f), dark, concrete);
            BuildLabBench(parent, new Vector3(-4f, 0f, -1.6f), dark, concrete);
            BuildWhiteboard(parent, wall, pink);
            BuildLighting(parent);
            CreateTextSign("StageDisclaimer", "CEVR ENGINEERING TUTORIAL LAB\nFICTIONAL TRAINING ENVIRONMENT",
                new Vector3(0f, 2.35f, 5.65f), Quaternion.Euler(0f, 180f, 0f), pink, parent, 0.14f, TextAnchor.MiddleCenter);
            CreateTextSign("ExitSign", "EXIT  /  ASSEMBLY POINT", new Vector3(5f, 2.85f, -5.72f),
                Quaternion.identity, green, parent, 0.11f, TextAnchor.MiddleCenter);
        }

        private static void BuildLabBench(Transform parent, Vector3 origin, Material top, Material legs)
        {
            Cube("LabBenchTop", origin + new Vector3(0f, 0.82f, 0f), new Vector3(4f, 0.14f, 1.2f), top, parent, true);
            foreach (float x in new[] { -1.7f, 1.7f })
            foreach (float z in new[] { -0.45f, 0.45f })
                Cube("LabBenchLeg", origin + new Vector3(x, 0.4f, z), new Vector3(0.16f, 0.8f, 0.16f), legs, parent, true);
        }

        private static CoverZone BuildStrongTableAndCoverZone(
            Transform environment, Transform dynamicParent, Transform zoneParent, Vector3 origin,
            Material wood, Material legs, GroundMotionPlayer motion, SessionLogger logger)
        {
            Cube("SturdyCoverTableTop", origin + new Vector3(0f, 0.86f, 0f), new Vector3(3.2f, 0.18f, 1.6f), wood, environment, true);
            foreach (float x in new[] { -1.35f, 1.35f })
            foreach (float z in new[] { -0.65f, 0.65f })
                Cube("SturdyTableLeg", origin + new Vector3(x, 0.42f, z), new Vector3(0.2f, 0.84f, 0.2f), legs, environment, true);
            var zoneObject = new GameObject("CoverZone_StrongTable");
            zoneObject.transform.SetParent(zoneParent);
            zoneObject.transform.position = origin + new Vector3(0f, 0.43f, 0f);
            BoxCollider trigger = zoneObject.AddComponent<BoxCollider>();
            trigger.size = new Vector3(2.6f, 0.8f, 1.25f);
            trigger.isTrigger = true;
            CoverZone cover = zoneObject.AddComponent<CoverZone>();
            cover.Configure("cover-sturdy-table-01");
            BuildMovableChair(dynamicParent, origin + new Vector3(0f, 0f, 1.35f), wood, legs, motion, logger);
            return cover;
        }

        private static void BuildMovableChair(
            Transform parent, Vector3 position, Material seatMaterial, Material frameMaterial,
            GroundMotionPlayer motion, SessionLogger logger)
        {
            var chair = new GameObject("MovableChair_StrongTableApproach");
            chair.transform.SetParent(parent);
            chair.transform.position = position;

            ChairPart("ChairSeat", new Vector3(0f, 0.48f, 0f), new Vector3(0.9f, 0.12f, 0.9f), seatMaterial, chair.transform);
            foreach (float x in new[] { -0.35f, 0.35f })
            foreach (float z in new[] { -0.35f, 0.35f })
                ChairPart("ChairLeg", new Vector3(x, 0.23f, z), new Vector3(0.1f, 0.46f, 0.1f), frameMaterial, chair.transform);
            ChairPart("ChairBack", new Vector3(0f, 1.0f, 0.4f), new Vector3(0.85f, 1.0f, 0.1f), seatMaterial, chair.transform);

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

        private static ExitAssemblyZone BuildAssemblyZone(Transform parent, Material green)
        {
            var zoneObject = new GameObject("ExitAssemblyZone");
            zoneObject.transform.SetParent(parent);
            zoneObject.transform.position = new Vector3(5f, 0.75f, -10f);
            BoxCollider trigger = zoneObject.AddComponent<BoxCollider>();
            trigger.size = new Vector3(4f, 1.5f, 3f);
            trigger.isTrigger = true;
            ExitAssemblyZone zone = zoneObject.AddComponent<ExitAssemblyZone>();
            zone.Configure("outdoor-assembly-point");
            return zone;
        }

        private static List<TutorialTask> BuildTutorialTasks(
            Transform taskParent, Transform dynamicParent, GroundMotionPlayer motion,
            Material bench, Material goalMaterial, Material itemMaterial)
        {
            var tasks = new List<TutorialTask>();
            tasks.Add(BuildPlacementTask(
                "task-circuit-module", "Place the circuit module in the green test tray.", "circuit-module",
                new Vector3(-5.1f, 1.15f, 1.6f), new Vector3(-3.2f, 1.0f, 1.6f),
                taskParent, dynamicParent, motion, itemMaterial, goalMaterial, PrimitiveType.Cube));
            tasks.Add(BuildPlacementTask(
                "task-safety-canister", "Place the safety canister in the green storage slot.", "safety-canister",
                new Vector3(-5.0f, 1.15f, -1.6f), new Vector3(-3.1f, 1.0f, -1.6f),
                taskParent, dynamicParent, motion, itemMaterial, goalMaterial, PrimitiveType.Cylinder));
            return tasks;
        }

        private static TutorialTask BuildPlacementTask(
            string taskId, string description, string itemId, Vector3 itemPosition, Vector3 goalPosition,
            Transform taskParent, Transform dynamicParent, GroundMotionPlayer motion,
            Material itemMaterial, Material goalMaterial, PrimitiveType itemPrimitive)
        {
            var taskObject = new GameObject(taskId);
            taskObject.transform.SetParent(taskParent);
            TutorialTask task = taskObject.AddComponent<TutorialTask>();
            task.Configure(taskId, description);

            GameObject item = GameObject.CreatePrimitive(itemPrimitive);
            item.name = "TaskItem_" + itemId;
            item.transform.SetParent(dynamicParent);
            item.transform.position = itemPosition;
            item.transform.localScale = itemPrimitive == PrimitiveType.Cylinder
                ? new Vector3(0.25f, 0.35f, 0.25f)
                : new Vector3(0.45f, 0.25f, 0.35f);
            item.GetComponent<Renderer>().sharedMaterial = itemMaterial;
            TaskItem id = item.AddComponent<TaskItem>();
            id.Configure(itemId);
            Rigidbody body = item.AddComponent<Rigidbody>();
            body.mass = 1.2f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            item.AddComponent<InertialRigidbody>().Configure(motion);
            TryAddXrGrabInteractable(item);

            GameObject goal = Cube("PlacementGoal_" + itemId, goalPosition, new Vector3(0.8f, 0.12f, 0.65f),
                goalMaterial, taskParent, false);
            BoxCollider goalCollider = goal.GetComponent<BoxCollider>();
            goalCollider.isTrigger = true;
            PlacementGoal placement = goal.AddComponent<PlacementGoal>();
            placement.Configure(itemId, task, goal.transform);
            return task;
        }

        private static List<Rigidbody> BuildHazards(
            Transform parent, GroundMotionPlayer motion, Material hazardMaterial, Material dark)
        {
            var hazards = new List<Rigidbody>();
            Vector3[] positions =
            {
                new Vector3(-1.5f, 3.15f, 0.2f),
                new Vector3(0.4f, 3.2f, -1.4f),
                new Vector3(6.8f, 3.05f, 1.6f),
                new Vector3(4.8f, 3.1f, -2.0f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject hazardObject = Cube($"StagedFallingHazard_{i + 1}", positions[i],
                    new Vector3(0.65f, 0.25f, 0.45f), i % 2 == 0 ? hazardMaterial : dark, parent, false);
                Rigidbody body = hazardObject.AddComponent<Rigidbody>();
                body.mass = 2.5f;
                body.isKinematic = true;
                body.useGravity = false;
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
                hazardObject.AddComponent<InertialRigidbody>().Configure(motion);
                FallingHazard falling = hazardObject.AddComponent<FallingHazard>();
                falling.Configure($"overhead-object-{i + 1}", 28f);
                hazards.Add(body);
            }
            return hazards;
        }

        private static Rigidbody BuildTopplingCabinet(
            Transform parent, GroundMotionPlayer motion, Material cabinetMaterial, Material hazardMaterial)
        {
            GameObject cabinet = Cube("UnsecuredTallCabinet", new Vector3(8.6f, 1.25f, 3.6f),
                new Vector3(1.1f, 2.5f, 0.75f), cabinetMaterial, parent, false);
            Rigidbody body = cabinet.AddComponent<Rigidbody>();
            body.mass = 38f;
            body.centerOfMass = new Vector3(0f, 0.15f, 0f);
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            cabinet.AddComponent<InertialRigidbody>().Configure(motion, 1f, false);
            FallingHazard hazard = cabinet.AddComponent<FallingHazard>();
            hazard.Configure("unsecured-cabinet", 45f);
            for (int i = 0; i < 3; i++)
                Cube($"CabinetWarningStripe_{i}", new Vector3(8.6f, 0.6f + i * 0.65f, 3.2f),
                    new Vector3(0.85f, 0.08f, 0.03f), hazardMaterial, cabinet.transform, false);
            return body;
        }

        private static void BuildHangingLights(Transform parent, GroundMotionPlayer motion, Material material)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector3 anchorPosition = new Vector3(-2f + i * 5f, 3.35f, 0f);
                GameObject anchor = new GameObject($"LampAnchor_{i + 1}");
                anchor.transform.SetParent(parent);
                anchor.transform.position = anchorPosition;
                Rigidbody anchorBody = anchor.AddComponent<Rigidbody>();
                anchorBody.isKinematic = true;

                GameObject lamp = Cube($"HangingLamp_{i + 1}", anchorPosition + Vector3.down * 0.65f,
                    new Vector3(1.3f, 0.12f, 0.3f), material, parent, false);
                Rigidbody lampBody = lamp.AddComponent<Rigidbody>();
                lampBody.mass = 3f;
                lampBody.linearDamping = 0.15f;
                lamp.AddComponent<InertialRigidbody>().Configure(motion);
                ConfigurableJoint joint = lamp.AddComponent<ConfigurableJoint>();
                joint.connectedBody = anchorBody;
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Limited;
                joint.angularZMotion = ConfigurableJointMotion.Limited;
                SoftJointLimit limit = new SoftJointLimit { limit = 18f };
                joint.lowAngularXLimit = new SoftJointLimit { limit = -18f };
                joint.highAngularXLimit = limit;
                joint.angularYLimit = limit;
                joint.angularZLimit = limit;
            }
        }

        private static PlayerHealth BuildDesktopPlayer(out Camera camera)
        {
            var player = new GameObject("DesktopDebugPlayer");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0f, 3.2f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.75f;
            controller.radius = 0.24f;
            controller.center = new Vector3(0f, 0.875f, 0f);
            PlayerHealth health = player.AddComponent<PlayerHealth>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.63f, 0f);
            camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();
            player.AddComponent<DesktopDebugRig>().Configure(camera);
            player.AddComponent<DesktopGrabInteractor>().Configure(camera);
            return health;
        }

        private static TutorialHud BuildHud(Transform parent)
        {
            var canvasObject = new GameObject("TutorialHUD");
            canvasObject.transform.SetParent(parent);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.position = new Vector3(0f, 1.85f, 5.5f);
            canvasRect.rotation = Quaternion.Euler(0f, 180f, 0f);
            canvasRect.localScale = Vector3.one * 0.0022f;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            Image panel = UiImage("HUDPanel", canvas.transform, new Color(0.04f, 0.06f, 0.08f, 0.82f),
                new Vector2(20f, -20f), new Vector2(760f, -245f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            Text phase = UiText("Phase", panel.transform, 28, FontStyle.Bold, new Vector2(20f, -15f), new Vector2(700f, -55f));
            Text objective = UiText("Objective", panel.transform, 23, FontStyle.Normal, new Vector2(20f, -58f), new Vector2(700f, -125f));
            Text timer = UiText("Timer", panel.transform, 24, FontStyle.Bold, new Vector2(20f, -135f), new Vector2(330f, -175f));
            Text tasks = UiText("Tasks", panel.transform, 20, FontStyle.Normal, new Vector2(350f, -135f), new Vector2(700f, -215f));

            Slider health = UiSlider("Health", panel.transform, new Vector2(20f, -190f), new Vector2(320f, -215f));
            Image indicator = UiImage("QuakeIndicator", canvas.transform, new Color(0.95f, 0.18f, 0.10f, 0.7f),
                Vector2.zero, new Vector2(1920f, 12f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            indicator.enabled = false;
            TutorialHud hud = canvasObject.AddComponent<TutorialHud>();
            hud.Configure(phase, objective, timer, tasks, health, indicator);
            return hud;
        }

        private static Image UiImage(string name, Transform parent, Color color, Vector2 min, Vector2 max, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = min;
            rect.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text UiText(string name, Transform parent, int size, FontStyle style, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = min;
            rect.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Slider UiSlider(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = min;
            rect.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
            Image background = UiImage("Background", root.transform, new Color(0.2f, 0.2f, 0.2f, 1f),
                Vector2.zero, rect.sizeDelta, Vector2.zero, Vector2.zero);
            Image fill = UiImage("Fill", root.transform, new Color(0.15f, 0.85f, 0.30f, 1f),
                Vector2.zero, rect.sizeDelta, Vector2.zero, Vector2.zero);
            Slider slider = root.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            slider.interactable = false;
            background.raycastTarget = false;
            fill.raycastTarget = false;
            return slider;
        }

        private static void BuildWhiteboard(Transform parent, Material wall, Material pink)
        {
            Cube("Whiteboard", new Vector3(-7.5f, 1.9f, -5.82f), new Vector3(3.8f, 1.7f, 0.08f), wall, parent, true);
            Cube("WhiteboardHeader", new Vector3(-7.5f, 2.83f, -5.75f), new Vector3(4.1f, 0.16f, 0.12f), pink, parent, true);
        }

        private static void BuildLighting(Transform parent)
        {
            var sun = new GameObject("Directional Light");
            sun.transform.SetParent(parent);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light directional = sun.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 0.75f;
            directional.shadows = LightShadows.Soft;
            for (int i = 0; i < 4; i++)
            {
                var lightObject = new GameObject($"LabLight_{i + 1}");
                lightObject.transform.SetParent(parent);
                lightObject.transform.position = new Vector3(-6f + i * 4f, 3.2f, 0f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 7f;
                light.intensity = 1.5f;
                light.color = new Color(0.91f, 0.95f, 1f);
                LabLights.Add(light);
            }
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 size, Material material, Transform parent, bool isStatic)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = size;
            cube.isStatic = isStatic;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static void CreateTextSign(
            string name, string value, Vector3 position, Quaternion rotation, Material material,
            Transform parent, float characterSize, TextAnchor anchor)
        {
            var sign = new GameObject(name);
            sign.transform.SetParent(parent);
            sign.transform.position = position;
            sign.transform.rotation = rotation;
            TextMesh text = sign.AddComponent<TextMesh>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 60;
            text.characterSize = characterSize;
            text.anchor = anchor;
            text.alignment = TextAlignment.Center;
            text.color = material.color;
        }

        private static Material MaterialAsset(string name, Color color)
        {
            string path = GeneratedRoot + "/Materials/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static TutorialScenarioConfig CreateOrLoadConfig()
        {
            TutorialScenarioConfig config = AssetDatabase.LoadAssetAtPath<TutorialScenarioConfig>(ConfigPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<TutorialScenarioConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static void TryAddXrGrabInteractable(GameObject target)
        {
            Type type = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (type == null)
            {
                Debug.LogWarning("XRGrabInteractable type not found. Import/resolve XR Interaction Toolkit, then rebuild the stage.");
                return;
            }
            if (target.GetComponent(type) == null) target.AddComponent(type);
        }

        private static void EnsureFolders()
        {
            foreach (string path in new[]
                     {
                         GeneratedRoot,
                         GeneratedRoot + "/Scenes",
                         GeneratedRoot + "/Data",
                         GeneratedRoot + "/Materials"
                     })
            {
                if (AssetDatabase.IsValidFolder(path)) continue;
                string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                string child = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent)) AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(scene => scene.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
