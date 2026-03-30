using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ScanCanvasImages
{
    [MenuItem("Tools/Debug/Scan Canvas Images")]
    public static void Execute()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log($"[ScanCanvasImages] Found {canvases.Length} Canvas(es).");

        foreach (var canvas in canvases)
        {
            var images = canvas.GetComponentsInChildren<Image>(true);
            Debug.Log($"[ScanCanvasImages] Canvas '{canvas.name}' has {images.Length} Image(s).");

            foreach (var img in images)
            {
                var rt = img.GetComponent<RectTransform>();
                var color = img.color;
                var size = rt != null ? rt.rect.size : Vector2.zero;

                Debug.Log(
                    $"[ScanCanvasImages] Image: '{GetPath(img.transform)}' size={size} color={color} enabled={img.enabled} raycastTarget={img.raycastTarget} sprite={(img.sprite != null ? img.sprite.name : "None")}");

                // Heuristic: big, mostly-opaque images are likely to be full-screen blockers.
                var isBig = size.x >= 1000f && size.y >= 600f;
                var isOpaque = color.a >= 0.9f;
                var isYellowish = color.r >= 0.7f && color.g >= 0.7f && color.b <= 0.4f;

                if (isBig && (isOpaque || isYellowish))
                {
                    Debug.LogWarning(
                        $"[ScanCanvasImages] Candidate blocker: '{GetPath(img.transform)}' size={size} color={color} enabled={img.enabled} raycastTarget={img.raycastTarget}");
                }
            }
        }
    }

    private static string GetPath(Transform t)
    {
        var names = t.GetComponentsInParent<Transform>(true).Select(x => x.name).Reverse();
        return string.Join("/", names);
    }
}
