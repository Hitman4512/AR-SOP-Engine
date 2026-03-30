#if UNITY_EDITOR
using SOP_Engine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SOP_Engine.Editor
{
    public static class SetupSOPDay1Scene
    {
        [MenuItem("SOP Engine/Day1/Setup SampleScene UI")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("No active scene.");
                return;
            }

            EnsureEventSystem();

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                canvas = CreateCanvas();

            // Root controller
            var controllerGo = GameObject.Find("SOPDay1");
            if (controllerGo == null)
                controllerGo = new GameObject("SOPDay1");

            var controller = controllerGo.GetComponent<SOPDay1Controller>();
            if (controller == null)
                controller = controllerGo.AddComponent<SOPDay1Controller>();

            // Step card panel
            var panelGo = GameObject.Find("Canvas/StepCard");
            if (panelGo == null)
            {
                panelGo = new GameObject("StepCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panelGo.transform.SetParent(canvas.transform, false);

                var rt = (RectTransform)panelGo.transform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(900, 360);
                rt.anchoredPosition = new Vector2(0, 80);

                var img = panelGo.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.6f);
            }

            // Title
            var titleGo = GameObject.Find("Canvas/StepCard/TitleText");
            TMP_Text titleText;
            if (titleGo == null)
            {
                titleGo = CreateTMPText("TitleText", panelGo.transform);
                var rt = (RectTransform)titleGo.transform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(-60, 80);
                rt.anchoredPosition = new Vector2(0, -30);

                titleText = titleGo.GetComponent<TMP_Text>();
                titleText.fontSize = 42;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.TopLeft;
                titleText.text = "Title";
            }
            else
            {
                titleText = titleGo.GetComponent<TMP_Text>();
            }

            // Instruction
            var instructionGo = GameObject.Find("Canvas/StepCard/InstructionText");
            TMP_Text instructionText;
            if (instructionGo == null)
            {
                instructionGo = CreateTMPText("InstructionText", panelGo.transform);
                var rt = (RectTransform)instructionGo.transform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(30, 30);
                rt.offsetMax = new Vector2(-30, -110);

                instructionText = instructionGo.GetComponent<TMP_Text>();
                instructionText.fontSize = 28;
                instructionText.alignment = TextAlignmentOptions.TopLeft;
                instructionText.textWrappingMode = TextWrappingModes.Normal;
                instructionText.text = "Instruction";
            }
            else
            {
                instructionText = instructionGo.GetComponent<TMP_Text>();
            }

            // Next button
            var nextGo = GameObject.Find("Canvas/NextButton");
            Button nextButton;
            if (nextGo == null)
            {
                nextGo = CreateButton(canvas.transform, "NextButton", "Next");
                var rt = (RectTransform)nextGo.transform;
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(240, 70);
                rt.anchoredPosition = new Vector2(0, 60);

                nextButton = nextGo.GetComponent<Button>();
            }
            else
            {
                nextButton = nextGo.GetComponent<Button>();
            }

            // Wire references via SerializedObject (private [SerializeField])
            var so = new SerializedObject(controller);
            so.FindProperty("sopFileName").stringValue = "maintenance-001";
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("instructionText").objectReferenceValue = instructionText;
            so.FindProperty("nextButton").objectReferenceValue = nextButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Ensure button has the controller listener (in case Awake doesn't run in edit mode)
            var click = nextButton.onClick;
            click.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(click, controller.NextStep);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("SOP Day-1 UI setup complete.");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            return canvas;
        }

        private static GameObject CreateTMPText(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.color = Color.white;
            text.raycastTarget = false;
            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.85f);

            var btn = go.GetComponent<Button>();

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)textGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 30;
            tmp.color = Color.black;
            tmp.raycastTarget = false;

            return go;
        }
    }
}
#endif
