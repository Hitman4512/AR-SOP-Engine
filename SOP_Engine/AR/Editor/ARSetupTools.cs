#if UNITY_EDITOR
using System;
using SOP_Engine.AR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace SOP_Engine.AR.Editor
{
    public static class ARSetupTools
    {
        [MenuItem("SOP Engine/AR/Convert SampleScene to AR")]
        public static void ConvertSceneToAR()
        {
            var scene = EditorSceneManager.GetActiveScene();

            // Remove standard Main Camera
            var mainCam = GameObject.Find("Main Camera");
            if (mainCam != null)
                UnityEngine.Object.DestroyImmediate(mainCam);

            // AR Session
            var arSession = GameObject.Find("AR Session");
            if (arSession == null)
            {
                arSession = new GameObject("AR Session");
                arSession.AddComponent<ARSession>();
            }

            // XR Origin
            var xrOriginGo = GameObject.Find("XR Origin");
            if (xrOriginGo == null)
            {
                xrOriginGo = new GameObject("XR Origin");
                xrOriginGo.AddComponent<XROrigin>();
            }

            // Camera Offset child
            var cameraOffset = xrOriginGo.transform.Find("Camera Offset");
            if (cameraOffset == null)
            {
                var co = new GameObject("Camera Offset");
                co.transform.SetParent(xrOriginGo.transform, false);
                cameraOffset = co.transform;
            }

            // AR Camera
            var arCameraTf = cameraOffset.Find("AR Camera");
            GameObject arCameraGo;
            if (arCameraTf == null)
            {
                arCameraGo = new GameObject("AR Camera");
                arCameraGo.transform.SetParent(cameraOffset, false);
            }
            else
            {
                arCameraGo = arCameraTf.gameObject;
            }

            var cam = arCameraGo.GetComponent<Camera>();
            if (cam == null) cam = arCameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;

            if (arCameraGo.GetComponent<AudioListener>() != null)
                UnityEngine.Object.DestroyImmediate(arCameraGo.GetComponent<AudioListener>());

            if (arCameraGo.GetComponent<ARCameraManager>() == null)
                arCameraGo.AddComponent<ARCameraManager>();
            if (arCameraGo.GetComponent<ARCameraBackground>() == null)
                arCameraGo.AddComponent<ARCameraBackground>();

            // Link camera to XROrigin
            var xrOrigin = xrOriginGo.GetComponent<XROrigin>();
            xrOrigin.Camera = cam;
            xrOrigin.CameraFloorOffsetObject = cameraOffset.gameObject;

            // Managers
            if (xrOriginGo.GetComponent<ARPlaneManager>() == null)
                xrOriginGo.AddComponent<ARPlaneManager>();
            if (xrOriginGo.GetComponent<ARRaycastManager>() == null)
                xrOriginGo.AddComponent<ARRaycastManager>();

            // Annotation manager
            if (xrOriginGo.GetComponent<ARAnnotationManager>() == null)
                xrOriginGo.AddComponent<ARAnnotationManager>();

            // Ensure Canvas still has GraphicRaycaster etc. (no-op)
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Scene converted to AR baseline (AR Session + XR Origin + AR Camera + Plane/Raycast managers)." +
                      "\nNext: create the arrow prefab (SOP Engine/AR/Create Arrow Annotation Prefab) and assign it to ARAnnotationManager.");
        }

        [MenuItem("SOP Engine/AR/Create Arrow Annotation Prefab")]
        public static void CreateArrowPrefab()
        {
            const string prefabFolder = "Assets/SOP_Engine/AR/Prefabs";
            const string meshFolder = "Assets/SOP_Engine/AR/Meshes";

            EnsureFolder("Assets/SOP_Engine/AR", "Prefabs");
            EnsureFolder("Assets/SOP_Engine/AR", "Meshes");

            // Create cone mesh asset (simple) if missing
            var coneMeshPath = meshFolder + "/Cone.mesh";
            var coneMesh = AssetDatabase.LoadAssetAtPath<Mesh>(coneMeshPath);
            if (coneMesh == null)
            {
                coneMesh = CreateConeMesh(0.06f, 0.18f, 18);
                AssetDatabase.CreateAsset(coneMesh, coneMeshPath);
                AssetDatabase.SaveAssets();
            }

            // Build prefab hierarchy
            var root = new GameObject("ArrowAnnotation");

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.04f, 0.04f, 0.18f);
            body.transform.localPosition = new Vector3(0f, 0f, 0.06f);

            var head = new GameObject("Head", typeof(MeshFilter), typeof(MeshRenderer));
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0f, 0.16f);
            head.transform.localRotation = Quaternion.identity;
            head.transform.localScale = Vector3.one;
            head.GetComponent<MeshFilter>().sharedMesh = coneMesh;

            // Simple unlit-ish material via default
            var mr = head.GetComponent<MeshRenderer>();
            mr.sharedMaterial = body.GetComponent<MeshRenderer>().sharedMaterial;

            // Label (3D TextMeshPro)
            var labelGo = new GameObject("Label", typeof(TMPro.TextMeshPro));
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.08f, 0.06f);
            labelGo.transform.localRotation = Quaternion.identity;
            var tmp = labelGo.GetComponent<TMPro.TextMeshPro>();
            tmp.text = "Label";
            tmp.fontSize = 2.5f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;

            // Billboard
            labelGo.AddComponent<BillboardFaceCamera>();

            // Save prefab
            var prefabPath = prefabFolder + "/ArrowAnnotation.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            // Assign prefab to ARAnnotationManager if present
            var mgr = UnityEngine.Object.FindFirstObjectByType<ARAnnotationManager>();
            if (mgr != null)
            {
                var so = new SerializedObject(mgr);
                so.FindProperty("arrowPrefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("Created ArrowAnnotation prefab at: " + prefabPath);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static Mesh CreateConeMesh(float radius, float height, int sides)
        {
            var mesh = new Mesh();
            mesh.name = "Cone";

            // Vertices: tip + base center + base ring
            var verts = new Vector3[2 + sides];
            var tip = Vector3.forward * height;
            verts[0] = tip;
            verts[1] = Vector3.zero;

            for (int i = 0; i < sides; i++)
            {
                var a = (float)i / sides * Mathf.PI * 2f;
                verts[2 + i] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            }

            var tris = new System.Collections.Generic.List<int>();

            // Side triangles (tip, ring i, ring i+1)
            for (int i = 0; i < sides; i++)
            {
                int i0 = 0;
                int i1 = 2 + i;
                int i2 = 2 + ((i + 1) % sides);
                tris.Add(i0);
                tris.Add(i1);
                tris.Add(i2);
            }

            // Base triangles (center, ring i+1, ring i)
            for (int i = 0; i < sides; i++)
            {
                int c = 1;
                int i1 = 2 + ((i + 1) % sides);
                int i2 = 2 + i;
                tris.Add(c);
                tris.Add(i1);
                tris.Add(i2);
            }

            mesh.vertices = verts;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
#endif
