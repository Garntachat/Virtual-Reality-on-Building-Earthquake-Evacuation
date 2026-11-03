using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChulaEarthquakeVR.Editor
{
    public static class EditableMainMenu
    {
        private const string ScenePath = "Assets/CEVR/Generated/Scenes/CEVR_MainMenu.unity";

        [MenuItem("CEVR/Main Menu/Open editable UI")]
        public static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Stop Play Mode", "Stop Play Mode before editing the menu.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var controller = Object.FindFirstObjectByType<MainMenuController>();
            if (controller == null)
                controller = new GameObject("Main Menu Controller").AddComponent<MainMenuController>();
            if (controller.editableCanvas != null)
            {
                EnsureCondoButton(controller.editableCanvas.transform, controller);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = controller.editableCanvas.gameObject;
                return;
            }
            var root = new GameObject("Main Menu Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1100, 720);
            scaler.matchWidthOrHeight = 0.5f;
            controller.editableCanvas = canvas;
            var panel = new GameObject("Menu Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            Place(panel, 25, 25, 650, 670);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.10f, 0.98f);
            Label(root.transform, "Title", "EARTHQUAKE TRAINING", 55, 55, 570, 55, 30);
            Label(root.transform, "Description", "Choose a scene. Take cover during shaking, then evacuate.", 55, 120, 570, 65, 20);
            AddButton(root.transform, "Play Tutorial", 185, controller.PlayTutorial);
            AddButton(root.transform, "Play House", 250, controller.PlayHouse);
            AddButton(root.transform, "Play Condo", 315, controller.PlayCondo);
            Label(root.transform, "Appearance Heading", "STUDENT OUTFIT", 55, 390, 570, 35, 20);
            // Use Unity's native dropdown template, preserving its navigation and option hierarchy.
            Selection.activeGameObject = root;
            EditorApplication.ExecuteMenuItem("GameObject/UI/Legacy/Dropdown");
            var dropdown = Selection.activeGameObject.GetComponent<Dropdown>();
            if (dropdown != null)
            {
                dropdown.name = "Student Outfit";
                dropdown.transform.SetParent(root.transform, false);
                Place(dropdown.gameObject, 55, 425, 570, 50);
                dropdown.ClearOptions();
                dropdown.AddOptions(new System.Collections.Generic.List<string>(StudentAppearance.Names));
                dropdown.value = StudentAppearance.Selected;
                UnityEventTools.AddPersistentListener(dropdown.onValueChanged, controller.SelectAppearance);
                controller.appearanceDropdown = dropdown;
            }
            AddButton(root.transform, "Exit", 505, controller.QuitMenu);
            controller.statusLabel = Label(root.transform, "Status", "Choose a scene to begin.", 55, 585, 570, 60, 18);
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem));
            var events = Object.FindFirstObjectByType<EventSystem>();
            // Resolve the input-system module without adding an editor assembly dependency.
            var inputType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputType != null && events.GetComponent(inputType) == null)
            {
                foreach (var oldModule in events.GetComponents<BaseInputModule>()) Object.DestroyImmediate(oldModule);
                events.gameObject.AddComponent(inputType);
            }
            else if (inputType == null && events.GetComponent<BaseInputModule>() == null)
                events.gameObject.AddComponent<StandaloneInputModule>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
        }

        private static void EnsureCondoButton(Transform canvas, MainMenuController controller)
        {
            if (canvas.Find("Play Condo") != null) return;
            // Preserve existing custom labels, colors, and click handlers.
            // Move only the known default menu elements enough to fit a third button.
            Transform title = canvas.Find("Appearance Heading");
            Transform dropdown = canvas.Find("Student Outfit");
            Transform exit = canvas.Find("Exit");
            Transform status = canvas.Find("Status");
            if (title != null) MoveToY(title, 405);
            if (dropdown != null) MoveToY(dropdown, 445);
            if (exit != null) MoveToY(exit, 530);
            if (status != null) MoveToY(status, 600);

            AddButton(canvas, "Play Condo", 340, controller.PlayCondo);
            Transform newButton = canvas.Find("Play Condo");
            Transform house = canvas.Find("Play House");
            if (newButton != null && house != null)
            {
                Image source = house.GetComponent<Image>();
                Image target = newButton.GetComponent<Image>();
                if (source != null && target != null) target.color = source.color;
            }
        }

        private static void MoveToY(Transform item, float y)
        {
            RectTransform rect = item as RectTransform;
            if (rect == null) return;
            Vector2 point = rect.anchoredPosition;
            rect.anchoredPosition = new Vector2(point.x, -y);
        }

        private static void Place(GameObject obj, float x, float y, float width, float height)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
        private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Place(obj, x, y, w, h);
            var text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
        private static void AddButton(Transform parent, string title, float y, UnityEngine.Events.UnityAction action)
        {
            var obj = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            Place(obj, 55, y, 570, 60);
            obj.GetComponent<Image>().color = new Color(0.13f, 0.35f, 0.32f);
            var button = obj.GetComponent<Button>();
            button.targetGraphic = obj.GetComponent<Image>();
            UnityEventTools.AddPersistentListener(button.onClick, action);
            var text = Label(obj.transform, "Label", title, 0, 0, 570, 60, 23);
            text.alignment = TextAnchor.MiddleCenter;
        }
    }
}
