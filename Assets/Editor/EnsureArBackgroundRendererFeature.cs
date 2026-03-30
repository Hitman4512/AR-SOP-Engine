using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class EnsureArBackgroundRendererFeature
{
    [MenuItem("Tools/AR/Ensure AR Background Renderer Feature")]
    public static void Execute()
    {
        Ensure("Assets/Settings/Mobile_Renderer.asset");
        Ensure("Assets/Settings/PC_Renderer.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnsureArBackgroundRendererFeature] Done.");
    }

    private static void Ensure(string rendererDataPath)
    {
        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererDataPath);
        if (rendererData == null)
        {
            Debug.LogWarning($"[EnsureArBackgroundRendererFeature] Renderer not found or not a UniversalRendererData: {rendererDataPath}");
            return;
        }

        // AR Foundation URP background feature type (package-defined).
        var featureType = Type.GetType("UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature, Unity.XR.ARFoundation")
                          ?? Type.GetType("UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature, Unity.XR.ARFoundation.Editor")
                          ?? Type.GetType("UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature, Unity.XR.ARFoundation.Runtime");

        if (featureType == null)
        {
            Debug.LogError("[EnsureArBackgroundRendererFeature] Could not find ARBackgroundRendererFeature type. Ensure AR Foundation is installed.");
            return;
        }

        var hasFeature = rendererData.rendererFeatures.Any(f => f != null && featureType.IsInstanceOfType(f));
        if (hasFeature)
        {
            Debug.Log($"[EnsureArBackgroundRendererFeature] '{rendererData.name}' already has ARBackgroundRendererFeature.");
            return;
        }

        var feature = ScriptableObject.CreateInstance(featureType) as ScriptableRendererFeature;
        if (feature == null)
        {
            Debug.LogError($"[EnsureArBackgroundRendererFeature] Failed to create feature instance of type {featureType.FullName}.");
            return;
        }

        feature.name = "AR Background Renderer Feature";
        AssetDatabase.AddObjectToAsset(feature, rendererData);

        rendererData.rendererFeatures.Add(feature);
        rendererData.SetDirty();
        EditorUtility.SetDirty(rendererData);

        Debug.Log($"[EnsureArBackgroundRendererFeature] Added ARBackgroundRendererFeature to {rendererDataPath}");
    }
}
