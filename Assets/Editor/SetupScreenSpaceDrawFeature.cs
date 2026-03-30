#if UNITY_EDITOR
using System.IO;
using SOP_Engine.AR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SetupScreenSpaceDrawFeature
{
    [MenuItem("Tools/SOP/Setup Screen-Space Draw Feature")]
    public static void Execute()
    {
        const string prefabDir = "Assets/SOP_Engine/AR/Prefabs";
        const string matDir = "Assets/SOP_Engine/AR/Materials";
        const string prefabPath = prefabDir + "/DrawingStroke.prefab";
        const string matPath = matDir + "/DrawingStroke_Unlit.mat";

        EnsureFolder(prefabDir);
        EnsureFolder(matDir);

        // 1) Create/Update material
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            mat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(mat, matPath);
        }

        // Try set a bright color if property exists
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", new Color(1f, 0.85f, 0.1f, 1f));
        else if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(1f, 0.85f, 0.1f, 1f));

        EditorUtility.SetDirty(mat);

        // 2) Create/Update stroke prefab
        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject prefabRoot;

        if (existingPrefab == null)
        {
            prefabRoot = new GameObject("DrawingStroke");
        }
        else
        {
            prefabRoot = PrefabUtility.InstantiatePrefab(existingPrefab) as GameObject;
            prefabRoot.name = "DrawingStroke";
        }

        prefabRoot.tag = "DrawingStroke";

        var lr = prefabRoot.GetComponent<LineRenderer>();
        if (lr == null)
            lr = prefabRoot.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = 6;
        lr.numCapVertices = 6;
        lr.textureMode = LineTextureMode.Stretch;

        lr.material = mat;
        lr.startColor = new Color(1f, 0.85f, 0.1f, 1f);
        lr.endColor = new Color(1f, 0.85f, 0.1f, 1f);
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;

        // Save prefab
        var saved = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        if (prefabRoot.scene.IsValid())
            Object.DestroyImmediate(prefabRoot);

        // 3) Scene wiring
        var scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene.");
            return;
        }

        // Draw overlay panel
        var overlayTransform = canvas.transform.Find("DrawInputOverlay");
        GameObject overlayGo;
        if (overlayTransform == null)
        {
            overlayGo = new GameObject("DrawInputOverlay");
            overlayGo.transform.SetParent(canvas.transform, false);
            overlayGo.AddComponent<CanvasRenderer>();
            var img = overlayGo.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;

            var rt = overlayGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Behind other UI so buttons still work.
            overlayGo.transform.SetSiblingIndex(0);

            overlayGo.SetActive(false);
        }
        else
        {
            overlayGo = overlayTransform.gameObject;
        }

        // Disable old overlay (if present)
        var oldOverlay = canvas.transform.Find("DrawingOverlay");
        if (oldOverlay != null)
            oldOverlay.gameObject.SetActive(false);

        // ARDrawController object
        var controllerGo = GameObject.Find("ARDrawController");
        if (controllerGo == null)
            controllerGo = new GameObject("ARDrawController");

        var controller = controllerGo.GetComponent<ARDrawController>();
        if (controller == null)
            controller = controllerGo.AddComponent<ARDrawController>();

        // Try find AR Camera
        var arCamGo = GameObject.Find("XR Origin/Camera Offset/AR Camera");
        var cam = arCamGo != null ? arCamGo.GetComponent<Camera>() : Camera.main;

        // Assign serialized fields
        var so = new SerializedObject(controller);
        so.FindProperty("sopController").objectReferenceValue = Object.FindFirstObjectByType<SOP_Engine.UI.SOPDay1Controller>();
        so.FindProperty("arCamera").objectReferenceValue = cam;
        so.FindProperty("drawInputOverlay").objectReferenceValue = overlayGo.GetComponent<RectTransform>();
        so.FindProperty("drawingStrokePrefab").objectReferenceValue = saved;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Screen-Space Draw Feature setup complete.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            return;

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
