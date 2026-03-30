#if UNITY_EDITOR
using SOP_Engine.UI;
using SOP_Engine.Voice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SOP_Engine.Editor
{
    public static class SetupVoiceInScene
    {
        [MenuItem("SOP Engine/Setup Voice In SampleScene")]
        public static void Execute()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;

            // Voice root
            var voiceGo = GameObject.Find("Voice");
            if (voiceGo == null)
            {
                voiceGo = new GameObject("Voice");
                Undo.RegisterCreatedObjectUndo(voiceGo, "Create Voice");
            }

            EnsureComponent<IntentResolver>(voiceGo);
            EnsureComponent<UnityKeywordProvider>(voiceGo);
            EnsureComponent<TTSController>(voiceGo);

            // UI indicator
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var indicator = GameObject.Find("Canvas/VoiceIndicator");
                if (indicator == null)
                {
                    indicator = new GameObject("VoiceIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VoiceIndicatorController));
                    Undo.RegisterCreatedObjectUndo(indicator, "Create VoiceIndicator");
                    indicator.transform.SetParent(canvas.transform, false);

                    var rt = (RectTransform)indicator.transform;
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-24f, -24f);
                    rt.sizeDelta = new Vector2(48f, 48f);

                    var img = indicator.GetComponent<Image>();
                    img.raycastTarget = false;
                    img.color = new Color(1f, 1f, 1f, 0.6f);

                    // Try to use built-in UISprite if available.
                    img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                }

                var controller = indicator.GetComponent<VoiceIndicatorController>();
                if (controller != null)
                {
                    var so = new SerializedObject(controller);
                    so.FindProperty("icon").objectReferenceValue = indicator.GetComponent<Image>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null)
            {
                c = Undo.AddComponent<T>(go);
            }
            return c;
        }
    }
}
#endif
