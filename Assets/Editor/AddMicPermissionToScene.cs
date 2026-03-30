using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AddMicPermissionToScene
{
    [MenuItem("Tools/SOP Engine/Add MicPermission To SampleScene")]
    public static void Execute()
    {
        var type = System.Type.GetType("SOP_Engine.Voice.MicPermission, Assembly-CSharp");
        if (type == null)
        {
            Debug.LogError("MicPermission type not found (did scripts compile?).");
            return;
        }

        // Reuse existing object if present
        var existing = GameObject.Find("MicPermission");
        if (existing == null)
            existing = new GameObject("MicPermission");

        if (existing.GetComponent(type) == null)
            existing.AddComponent(type);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("AddMicPermissionToScene: MicPermission object ensured in active scene.");
    }
}
