using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    public sealed class TutorialVisualPolish : MonoBehaviour
    {
        private const string RootName = "CEVR_FinalVisualPolish";

        private readonly List<Material> runtimeMaterials = new List<Material>();

        private Material concrete;
        private Material wall;
        private Material floor;
        private Material charcoal;
        private Material wood;
        private Material pink;
        private Material green;
        private Material amber;
        private Material window;
        private Material steel;
        private Material lightPanel;
        private Material teal;
        private Material red;
        private Material screen;
        private Material white;

        public static bool EnsureApplied()
        {
            if (GameObject.Find(RootName) != null) return false;
            var root = new GameObject(RootName);
            TutorialVisualPolish polish = root.AddComponent<TutorialVisualPolish>();
            polish.BuildPalette();
            polish.ApplyScenePalette();
            polish.ConfigureLightingAndCamera();
            polish.BuildWayfindingAndSafetyMarkers();
            polish.PolishHud();
            return true;
        }

        private void BuildPalette()
        {
            concrete = RuntimeMaterial("CEVR Concrete", new Color(0.47f, 0.51f, 0.56f), 0.05f, 0.18f);
            wall = RuntimeMaterial("CEVR Warm Wall", new Color(0.82f, 0.85f, 0.88f), 0f, 0.08f);
            floor = RuntimeMaterial("CEVR Slate Floor", new Color(0.16f, 0.20f, 0.25f), 0.08f, 0.24f);
            charcoal = RuntimeMaterial("CEVR Charcoal", new Color(0.08f, 0.11f, 0.15f), 0.35f, 0.38f);
            wood = RuntimeMaterial("CEVR Table Wood", new Color(0.39f, 0.20f, 0.10f), 0f, 0.28f);
            pink = RuntimeMaterial("CEVR Chula Pink", new Color(0.88f, 0.08f, 0.40f), 0.05f, 0.36f,
                new Color(0.18f, 0.01f, 0.05f));
            green = RuntimeMaterial("CEVR Safety Green", new Color(0.05f, 0.62f, 0.31f), 0.05f, 0.3f,
                new Color(0.01f, 0.13f, 0.04f));
            amber = RuntimeMaterial("CEVR Hazard Amber", new Color(1f, 0.56f, 0.04f), 0.05f, 0.3f,
                new Color(0.18f, 0.06f, 0f));
            window = RuntimeMaterial("CEVR Window Blue", new Color(0.10f, 0.32f, 0.48f), 0.25f, 0.72f);
            steel = RuntimeMaterial("CEVR Brushed Steel", new Color(0.29f, 0.35f, 0.41f), 0.65f, 0.42f);
            lightPanel = RuntimeMaterial("CEVR Light Panel", new Color(0.82f, 0.9f, 1f), 0f, 0.65f,
                new Color(0.85f, 0.95f, 1f));
            teal = RuntimeMaterial("CEVR Engineering Teal", new Color(0.02f, 0.48f, 0.54f), 0.08f, 0.35f);
            red = RuntimeMaterial("CEVR Emergency Red", new Color(0.78f, 0.035f, 0.04f), 0.08f, 0.34f);
            screen = RuntimeMaterial("CEVR Monitor Screen", new Color(0.025f, 0.12f, 0.18f), 0.18f, 0.72f,
                new Color(0.02f, 0.22f, 0.28f));
            white = RuntimeMaterial("CEVR Clean White", new Color(0.92f, 0.95f, 0.96f), 0f, 0.24f);
        }

        private void ApplyScenePalette()
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (Renderer renderer in renderers)
            {
                if (renderer.GetComponent<TextMesh>() != null) continue;
                string objectName = renderer.gameObject.name;
                Material replacement = MaterialFor(objectName);
                if (replacement != null) renderer.sharedMaterial = replacement;
            }
        }

        private Material MaterialFor(string objectName)
        {
            if (objectName == "Floor" || objectName == "OutdoorWalkway") return floor;
            if (objectName.Contains("Wall") || objectName == "Ceiling" || objectName == "Whiteboard") return wall;
            if (objectName.StartsWith("ConcreteColumn") || objectName == "LabBenchLeg") return concrete;
            if (objectName.StartsWith("Window")) return window;
            if (objectName == "SturdyCoverTableTop") return wood;
            if (objectName.Contains("ChairSeat") || objectName.Contains("ChairBack") ||
                objectName.StartsWith("PinkBand") || objectName == "WhiteboardHeader") return pink;
            if (objectName.Contains("ChairLeg") || objectName.Contains("ChairBackPost") ||
                objectName == "LabBenchTop" || objectName.StartsWith("ExitDoorFrame") ||
                objectName == "UnsecuredTallCabinet") return charcoal;
            if (objectName.StartsWith("SturdyTableLeg") || objectName.StartsWith("HangingLamp")) return steel;
            if (objectName.StartsWith("PlacementGoal") || objectName == "AssemblyMarker") return green;
            if (objectName.StartsWith("TaskItem") || objectName.StartsWith("StagedFallingHazard") ||
                objectName.StartsWith("CabinetWarningStripe")) return amber;
            return null;
        }

        private void ConfigureLightingAndCamera()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.33f, 0.39f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.22f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.09f, 0.12f);
            RenderSettings.fog = false;

            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light sceneLight in lights)
            {
                if (sceneLight.type == LightType.Directional)
                {
                    sceneLight.intensity = 0.62f;
                    sceneLight.color = new Color(1f, 0.94f, 0.86f);
                    sceneLight.shadows = LightShadows.Soft;
                }
                else if (sceneLight.name.StartsWith("LabLight"))
                {
                    sceneLight.intensity = 0.82f;
                    sceneLight.range = 6.5f;
                    sceneLight.color = new Color(0.82f, 0.9f, 1f);
                    sceneLight.shadows = LightShadows.None;
                }
            }

            AddFillLight("CEVR_Fill_Cover", new Vector3(3.2f, 2.7f, 1.2f),
                new Color(1f, 0.76f, 0.84f), 0.7f, 5.5f);
            AddFillLight("CEVR_Fill_Exit", new Vector3(5f, 2.5f, -4.2f),
                new Color(0.65f, 1f, 0.78f), 0.65f, 5f);

            Camera playerCamera = FindFirstObjectByType<Camera>();
            if (playerCamera != null)
            {
                playerCamera.allowHDR = true;
                playerCamera.allowMSAA = true;
                playerCamera.fieldOfView = 70f;
                playerCamera.nearClipPlane = 0.04f;
                playerCamera.backgroundColor = new Color(0.07f, 0.1f, 0.14f);
            }
        }

        private void BuildWayfindingAndSafetyMarkers()
        {
            BuildCoverOutline();
            BuildExitPath();
            BuildChairBeacon();
            BuildHazardBoundary();
            BuildVisibleLightPanels();
            BuildEngineeringWorkstations();
            BuildSafetyEquipment();
            BuildComfortAndStorageDecor();
        }

        private void BuildCoverOutline()
        {
            Vector3 center = new Vector3(2.5f, 0.018f, 0.8f);
            DecorCube("CoverCrawlArea", center - Vector3.up * 0.006f,
                new Vector3(2.75f, 0.012f, 1.30f), green);
            DecorCube("CoverOutline_Front", center + new Vector3(0f, 0f, -0.73f),
                new Vector3(3f, 0.025f, 0.055f), green);
            DecorCube("CoverOutline_Back", center + new Vector3(0f, 0f, 0.73f),
                new Vector3(3f, 0.025f, 0.055f), green);
            DecorCube("CoverOutline_Left", center + new Vector3(-1.47f, 0f, 0f),
                new Vector3(0.055f, 0.025f, 1.5f), green);
            DecorCube("CoverOutline_Right", center + new Vector3(1.47f, 0f, 0f),
                new Vector3(0.055f, 0.025f, 1.5f), green);
            DecorCube("CoverTableAccent", new Vector3(2.5f, 0.94f, 0.01f),
                new Vector3(3.23f, 0.035f, 0.04f), green);
        }

        private void BuildExitPath()
        {
            for (int i = 0; i < 4; i++)
            {
                float z = -5.85f - i * 1.25f;
                CreateChevron($"ExitChevron_{i + 1}", new Vector3(5f, 0.025f, z), 0f, green);
            }
        }

        private void BuildChairBeacon()
        {
            Vector3 center = new Vector3(2.5f, 1.45f, 1.8f);
            GameObject left = DecorCube("ChairBeacon_Left", center + new Vector3(-0.1f, 0.12f, 0f),
                new Vector3(0.05f, 0.36f, 0.05f), pink, Quaternion.Euler(0f, 0f, -35f));
            GameObject right = DecorCube("ChairBeacon_Right", center + new Vector3(0.1f, 0.12f, 0f),
                new Vector3(0.05f, 0.36f, 0.05f), pink, Quaternion.Euler(0f, 0f, 35f));
            GameObject chairObject = GameObject.Find("MovableChair_StrongTableApproach");
            MovableFurniture chair = chairObject == null ? null : chairObject.GetComponent<MovableFurniture>();
            if (chair == null) return;
            left.transform.SetParent(chair.transform, true);
            right.transform.SetParent(chair.transform, true);
        }

        private void BuildHazardBoundary()
        {
            Vector3 center = new Vector3(8.6f, 0.02f, 3.6f);
            DecorCube("CabinetHazardFront", center + new Vector3(0f, 0f, -0.72f),
                new Vector3(1.6f, 0.025f, 0.07f), amber);
            DecorCube("CabinetHazardLeft", center + new Vector3(-0.77f, 0f, 0f),
                new Vector3(0.07f, 0.025f, 1.5f), amber);
            DecorCube("CabinetHazardRight", center + new Vector3(0.77f, 0f, 0f),
                new Vector3(0.07f, 0.025f, 1.5f), amber);
        }

        private void BuildVisibleLightPanels()
        {
            for (int i = 0; i < 4; i++)
                DecorCube($"CeilingPanel_{i + 1}", new Vector3(-6f + i * 4f, 3.49f, 0f),
                    new Vector3(2.1f, 0.035f, 0.38f), lightPanel);
        }

        private void BuildEngineeringWorkstations()
        {
            BuildWorkstation("North", new Vector3(-4f, 0f, 1.6f), 0f);
            BuildWorkstation("South", new Vector3(-4f, 0f, -1.6f), 180f);

            DecorCube("CircuitManual_01", new Vector3(-5.45f, 0.94f, 1.72f),
                new Vector3(0.48f, 0.05f, 0.62f), teal, Quaternion.Euler(0f, 12f, 0f));
            DecorCube("CircuitManual_02", new Vector3(-5.45f, 0.99f, 1.72f),
                new Vector3(0.44f, 0.045f, 0.58f), white, Quaternion.Euler(0f, 7f, 0f));
            DecorCube("ToolTray", new Vector3(-2.7f, 0.94f, -1.6f),
                new Vector3(0.72f, 0.06f, 0.48f), teal);
            for (int i = 0; i < 3; i++)
                DecorCube($"ToolTrayInstrument_{i + 1}", new Vector3(-2.92f + i * 0.22f, 0.99f, -1.6f),
                    new Vector3(0.12f, 0.045f, 0.3f), steel);
        }

        private void BuildWorkstation(string suffix, Vector3 benchCenter, float yaw)
        {
            Vector3 monitorCenter = benchCenter + new Vector3(0f, 1.42f, 0f);
            DecorCube($"Monitor_{suffix}_Screen", monitorCenter,
                new Vector3(1.08f, 0.64f, 0.08f), screen, Quaternion.Euler(0f, yaw, 0f));
            DecorCube($"Monitor_{suffix}_Bezel", monitorCenter + new Vector3(0f, 0f, 0.05f),
                new Vector3(1.18f, 0.73f, 0.045f), charcoal, Quaternion.Euler(0f, yaw, 0f));
            DecorCube($"Monitor_{suffix}_Stand", benchCenter + new Vector3(0f, 1.04f, 0f),
                new Vector3(0.12f, 0.28f, 0.12f), steel);
            DecorCube($"Monitor_{suffix}_Base", benchCenter + new Vector3(0f, 0.93f, 0f),
                new Vector3(0.58f, 0.05f, 0.36f), steel);
            DecorCube($"Keyboard_{suffix}", benchCenter + new Vector3(0f, 0.94f, -0.42f),
                new Vector3(0.84f, 0.055f, 0.26f), charcoal, Quaternion.Euler(0f, yaw, 0f));
        }

        private void BuildSafetyEquipment()
        {
            DecorPrimitive("FireExtinguisher", PrimitiveType.Cylinder, new Vector3(7.35f, 0.58f, -5.62f),
                new Vector3(0.22f, 0.55f, 0.22f), red);
            DecorCube("FireExtinguisherHandle", new Vector3(7.35f, 1.13f, -5.62f),
                new Vector3(0.26f, 0.08f, 0.12f), charcoal);
            DecorCube("FirstAidCabinet", new Vector3(7.9f, 1.55f, -5.68f),
                new Vector3(0.66f, 0.76f, 0.12f), white);
            DecorCube("FirstAidCrossVertical", new Vector3(7.9f, 1.55f, -5.60f),
                new Vector3(0.13f, 0.46f, 0.025f), green);
            DecorCube("FirstAidCrossHorizontal", new Vector3(7.9f, 1.55f, -5.59f),
                new Vector3(0.46f, 0.13f, 0.025f), green);
            DecorCube("EmergencyStopStation", new Vector3(9.78f, 1.42f, -3.9f),
                new Vector3(0.16f, 0.48f, 0.48f), amber);
            DecorPrimitive("EmergencyStopButton", PrimitiveType.Cylinder, new Vector3(9.66f, 1.42f, -3.9f),
                new Vector3(0.12f, 0.10f, 0.12f), red, Quaternion.Euler(0f, 0f, 90f));
        }

        private void BuildComfortAndStorageDecor()
        {
            DecorPrimitive("PlantPot", PrimitiveType.Cylinder, new Vector3(-8.65f, 0.30f, 4.75f),
                new Vector3(0.36f, 0.30f, 0.36f), wood);
            DecorPrimitive("PlantLeaf_01", PrimitiveType.Sphere, new Vector3(-8.65f, 0.82f, 4.75f),
                new Vector3(0.34f, 0.58f, 0.26f), green, Quaternion.Euler(0f, 0f, 18f));
            DecorPrimitive("PlantLeaf_02", PrimitiveType.Sphere, new Vector3(-8.9f, 0.74f, 4.75f),
                new Vector3(0.26f, 0.48f, 0.22f), teal, Quaternion.Euler(0f, 0f, -28f));
            DecorPrimitive("PlantLeaf_03", PrimitiveType.Sphere, new Vector3(-8.42f, 0.73f, 4.72f),
                new Vector3(0.24f, 0.44f, 0.20f), green, Quaternion.Euler(0f, 0f, 34f));

            DecorCube("StorageUnitBody", new Vector3(-9.72f, 1.15f, -3.9f),
                new Vector3(0.30f, 2.3f, 2.25f), steel);
            for (int i = 0; i < 3; i++)
                DecorCube($"StorageShelf_{i + 1}", new Vector3(-9.52f, 0.42f + i * 0.7f, -3.9f),
                    new Vector3(0.34f, 0.06f, 2.05f), charcoal);
            DecorCube("WhiteboardMarkerTray", new Vector3(-5.5f, 1.12f, 5.66f),
                new Vector3(2.1f, 0.08f, 0.18f), steel);
            for (int i = 0; i < 4; i++)
                DecorCube($"WhiteboardMarker_{i + 1}", new Vector3(-6.05f + i * 0.35f, 1.2f, 5.56f),
                    new Vector3(0.24f, 0.045f, 0.045f), i == 0 ? pink : teal);
        }

        private void CreateChevron(string name, Vector3 position, float yaw, Material material)
        {
            DecorCube(name + "_L", position + new Vector3(-0.14f, 0f, 0.08f),
                new Vector3(0.055f, 0.025f, 0.48f), material, Quaternion.Euler(0f, yaw + 35f, 0f));
            DecorCube(name + "_R", position + new Vector3(0.14f, 0f, 0.08f),
                new Vector3(0.055f, 0.025f, 0.48f), material, Quaternion.Euler(0f, yaw - 35f, 0f));
        }

        private void PolishHud()
        {
            TutorialHud hud = FindFirstObjectByType<TutorialHud>();
            Canvas canvas = hud == null ? null : hud.GetComponent<Canvas>();
            if (canvas == null) return;
            DesktopDebugRig desktop = FindFirstObjectByType<DesktopDebugRig>();
            if (desktop != null && desktop.isActiveAndEnabled)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                canvasRect.localRotation = Quaternion.identity;
                canvasRect.localScale = Vector3.one;
                canvasRect.anchoredPosition3D = Vector3.zero;
            }
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            Image panel = FindNamed<Image>(canvas.transform, "HUDPanel");
            if (panel != null)
            {
                panel.color = new Color(0.025f, 0.045f, 0.075f, 0.94f);
                panel.raycastTarget = false;
                RectTransform rect = panel.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(28f, -28f);
                rect.sizeDelta = new Vector2(630f, 225f);
            }

            ConfigureText(canvas.transform, "Phase", 30, FontStyle.Bold,
                new Color(1f, 0.25f, 0.55f), new Vector2(24f, -18f), new Vector2(580f, 38f));
            ConfigureText(canvas.transform, "Objective", 23, FontStyle.Normal,
                Color.white, new Vector2(24f, -62f), new Vector2(580f, 72f));
            ConfigureText(canvas.transform, "Timer", 25, FontStyle.Bold,
                new Color(1f, 0.72f, 0.2f), new Vector2(24f, -146f), new Vector2(250f, 38f));
            ConfigureText(canvas.transform, "Tasks", 19, FontStyle.Normal,
                new Color(0.78f, 0.88f, 1f), new Vector2(300f, -146f), new Vector2(280f, 32f));

            Text controls = FindNamed<Text>(canvas.transform, "Controls");
            if (controls == null && panel != null)
            {
                var controlObject = new GameObject("Controls", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                controlObject.transform.SetParent(panel.transform, false);
                controls = controlObject.GetComponent<Text>();
                controls.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            if (controls != null)
            {
                controls.text = "HEALTH";
                controls.fontSize = 16;
                controls.fontStyle = FontStyle.Bold;
                controls.color = new Color(0.62f, 0.7f, 0.8f);
                controls.alignment = TextAnchor.MiddleLeft;
                SetRect(controls.rectTransform, new Vector2(24f, -183f), new Vector2(100f, 24f));
            }

            Slider health = FindNamed<Slider>(canvas.transform, "Health");
            if (health != null)
            {
                health.gameObject.SetActive(true);
                SetRect(health.GetComponent<RectTransform>(), new Vector2(112f, -185f), new Vector2(250f, 14f));
                Image fill = FindNamed<Image>(health.transform, "Fill");
                Image background = FindNamed<Image>(health.transform, "Background");
                if (fill != null) fill.color = new Color(0.08f, 0.82f, 0.42f, 1f);
                if (background != null) background.color = new Color(0.08f, 0.12f, 0.17f, 1f);
            }

            Image quake = FindNamed<Image>(canvas.transform, "QuakeIndicator");
            if (quake != null)
            {
                RectTransform rect = quake.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, 10f);
            }
        }

        private void ConfigureText(
            Transform root, string name, int size, FontStyle style, Color color,
            Vector2 position, Vector2 dimensions)
        {
            Text text = FindNamed<Text>(root, name);
            if (text == null) return;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(text.rectTransform, position, dimensions);
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 dimensions)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
        }

        private static T FindNamed<T>(Transform root, string objectName) where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            foreach (T component in components)
                if (component.gameObject.name == objectName) return component;
            return null;
        }

        private void AddFillLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(transform);
            lightObject.transform.position = position;
            Light sceneLight = lightObject.AddComponent<Light>();
            sceneLight.type = LightType.Point;
            sceneLight.color = color;
            sceneLight.intensity = intensity;
            sceneLight.range = range;
            sceneLight.shadows = LightShadows.None;
        }

        private GameObject DecorCube(
            string name, Vector3 position, Vector3 size, Material material, Quaternion? rotation = null)
        {
            return DecorPrimitive(name, PrimitiveType.Cube, position, size, material, rotation);
        }

        private GameObject DecorPrimitive(
            string name, PrimitiveType primitiveType, Vector3 position, Vector3 size,
            Material material, Quaternion? rotation = null)
        {
            GameObject decoration = GameObject.CreatePrimitive(primitiveType);
            decoration.name = name;
            decoration.transform.SetParent(transform);
            decoration.transform.position = position;
            decoration.transform.rotation = rotation ?? Quaternion.identity;
            decoration.transform.localScale = size;
            Renderer renderer = decoration.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            Collider collider = decoration.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            return decoration;
        }

        private Material RuntimeMaterial(
            string name, Color color, float metallic, float smoothness, Color? emission = null)
        {
            Shader shader = Shader.Find(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit") ??
                            Shader.Find("Sprites/Default") ??
                            Shader.Find("UI/Default");
            if (shader == null) return null;
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            runtimeMaterials.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            foreach (Material material in runtimeMaterials)
            {
                if (material == null) continue;
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
            runtimeMaterials.Clear();
        }
    }
}
