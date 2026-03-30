using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SOP_Engine.UI;

public static class SetupDemoScene
{
    [MenuItem("Tools/SOP/Setup Demo Scene UI")]
    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("No active scene.");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found in scene.");
            return;
        }

        CreateOrUpdateDrawingOverlay(canvas);
        CreateOrUpdateSummaryReport(canvas);
        CreateOrUpdateBackButton(canvas);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Demo scene UI setup complete.");
    }

    private static void CreateOrUpdateDrawingOverlay(Canvas canvas)
    {
        var existing = canvas.transform.Find("DrawingOverlay");
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject("DrawingOverlay", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
        }

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        if (go.GetComponent<ScreenSpaceDrawTool>() == null)
            go.AddComponent<ScreenSpaceDrawTool>();

        // Put behind StepCard/Buttons but still on top of AR camera.
        go.transform.SetAsFirstSibling();
    }

    private static void CreateOrUpdateSummaryReport(Canvas canvas)
    {
        var existing = canvas.transform.Find("SummaryReport");
        GameObject root;
        if (existing != null)
        {
            root = existing.gameObject;
        }
        else
        {
            root = new GameObject("SummaryReport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
        }

        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = root.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        bg.raycastTarget = true;

        var controller = root.GetComponent<SOPSummaryReportController>();
        if (controller == null)
            controller = root.AddComponent<SOPSummaryReportController>();

        // Content panel
        var content = FindOrCreateChild(root.transform, "Content", typeof(RectTransform));
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.1f, 0.2f);
        contentRt.anchorMax = new Vector2(0.9f, 0.85f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        // Header
        var header = FindOrCreateTMP(content.transform, "HeaderText", 64, TextAlignmentOptions.Center);
        var headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 0.75f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.offsetMin = Vector2.zero;
        headerRt.offsetMax = Vector2.zero;
        header.color = new Color(0.2f, 1f, 0.2f, 1f);
        header.text = "SOP Complete";

        // Checklist
        var checklist = FindOrCreateTMP(content.transform, "ChecklistText", 36, TextAlignmentOptions.TopLeft);
        var checklistRt = checklist.GetComponent<RectTransform>();
        checklistRt.anchorMin = new Vector2(0f, 0.15f);
        checklistRt.anchorMax = new Vector2(1f, 0.75f);
        checklistRt.offsetMin = Vector2.zero;
        checklistRt.offsetMax = Vector2.zero;
        checklist.text = "• 1. ... ✓\n• 2. ... ✓\n• 3. ... ✓";

        // Time
        var time = FindOrCreateTMP(content.transform, "TimeText", 28, TextAlignmentOptions.BottomLeft);
        var timeRt = time.GetComponent<RectTransform>();
        timeRt.anchorMin = new Vector2(0f, 0f);
        timeRt.anchorMax = new Vector2(1f, 0.15f);
        timeRt.offsetMin = Vector2.zero;
        timeRt.offsetMax = Vector2.zero;
        time.text = "Completed (UTC):";

        // Bind serialized fields via SerializedObject so Unity saves references
        var so = new SerializedObject(controller);
        so.FindProperty("headerText").objectReferenceValue = header;
        so.FindProperty("checklistText").objectReferenceValue = checklist;
        so.FindProperty("timeText").objectReferenceValue = time;
        so.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        root.transform.SetAsLastSibling();
    }

    private static void CreateOrUpdateBackButton(Canvas canvas)
    {
        // Tries to match existing naming: Canvas/NextButton
        var nextButtonT = canvas.transform.Find("NextButton");
        if (nextButtonT == null)
        {
            Debug.LogWarning("NextButton not found; skipping BackButton creation.");
            return;
        }

        var existing = canvas.transform.Find("BackButton");
        GameObject backGo;
        if (existing != null)
        {
            backGo = existing.gameObject;
        }
        else
        {
            backGo = new GameObject("BackButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            backGo.transform.SetParent(canvas.transform, false);
        }

        // Position: left of Next button
        var nextRt = nextButtonT.GetComponent<RectTransform>();
        var backRt = backGo.GetComponent<RectTransform>();

        backRt.anchorMin = nextRt.anchorMin;
        backRt.anchorMax = nextRt.anchorMax;
        backRt.pivot = nextRt.pivot;
        backRt.sizeDelta = nextRt.sizeDelta;

        var dx = Mathf.Max(40f, nextRt.sizeDelta.x + 40f);
        backRt.anchoredPosition = nextRt.anchoredPosition + new Vector2(-dx, 0f);

        // Label
        var textT = backGo.transform.Find("Text");
        TextMeshProUGUI tmp;
        if (textT == null)
        {
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(backGo.transform, false);
            tmp = textGo.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            tmp = textT.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = textT.gameObject.AddComponent<TextMeshProUGUI>();
        }

        tmp.text = "Back";
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        var textRt = tmp.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        // Wire into SOPDay1Controller if present
        var controller = Object.FindFirstObjectByType<SOPDay1Controller>();
        if (controller != null)
        {
            var backBtn = backGo.GetComponent<Button>();
            var so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static GameObject FindOrCreateChild(Transform parent, string name, params System.Type[] components)
    {
        var t = parent.Find(name);
        if (t != null)
            return t.gameObject;

        var go = new GameObject(name, components);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI FindOrCreateTMP(Transform parent, string name, float fontSize, TextAlignmentOptions align)
    {
        var t = parent.Find(name);
        TextMeshProUGUI tmp;
        if (t != null)
        {
            tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = t.gameObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            tmp = go.AddComponent<TextMeshProUGUI>();
        }

        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.color = Color.white;

        var rt = tmp.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        return tmp;
    }
}
