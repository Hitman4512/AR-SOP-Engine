using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public static class FixARPlaneAndSceneRefs
{
    private const string PlanePrefabPath = "Assets/SOP_Engine/AR/Prefabs/SimpleARPlane.prefab";
    private const string PlaneMaterialPath = "Assets/SOP_Engine/AR/Materials/SimpleARPlane_Mat.mat";

    [MenuItem("Tools/SOP Engine/Fix AR Plane Visualizer + Refs")]
    public static void Execute()
    {
        EnsureFolders();

        var planePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
        if (planePrefab == null)
            planePrefab = CreatePlanePrefab();

        AssignSceneReferences(planePrefab);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("FixARPlaneAndSceneRefs: done.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/SOP_Engine/AR/Prefabs");
        EnsureFolder("Assets/SOP_Engine/AR/Materials");
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

    private static GameObject CreatePlanePrefab()
    {
        // Material
        var mat = AssetDatabase.LoadAssetAtPath<Material>(PlaneMaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = new Color(0.1f, 0.8f, 1f, 0.25f);
            AssetDatabase.CreateAsset(mat, PlaneMaterialPath);
        }

        // Prefab GO
        var go = new GameObject("SimpleARPlane");
        try
        {
            // Required by ARPlaneManager
            go.AddComponent<ARPlane>();

            // Generates plane mesh
            var vis = go.AddComponent<ARPlaneMeshVisualizer>();
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null) mf = go.AddComponent<MeshFilter>();

            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            // Save prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlanePrefabPath);
            Debug.Log($"Created plane prefab at {PlanePrefabPath}");
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static void AssignSceneReferences(GameObject planePrefab)
    {
        var xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin == null)
        {
            Debug.LogWarning("FixARPlaneAndSceneRefs: couldn't find 'XR Origin' in scene.");
            return;
        }

        var planeManager = xrOrigin.GetComponent<ARPlaneManager>();
        if (planeManager != null && planeManager.planePrefab == null)
        {
            planeManager.planePrefab = planePrefab;
            Debug.Log("Assigned ARPlaneManager.planePrefab.");
        }

        // Also make sure ARAnnotationManager has the ARRaycastManager reference serialized (optional; Awake() already finds it).
        var raycastManager = xrOrigin.GetComponent<ARRaycastManager>();
        var annotationManager = xrOrigin.GetComponent("SOP_Engine.AR.ARAnnotationManager");
        if (annotationManager != null && raycastManager != null)
        {
            var so = new SerializedObject(annotationManager);
            var prop = so.FindProperty("raycastManager");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = raycastManager;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("Assigned ARAnnotationManager.raycastManager.");
            }
        }
    }
}
