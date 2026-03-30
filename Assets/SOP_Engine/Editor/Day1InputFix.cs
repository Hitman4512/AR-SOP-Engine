#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SOP_Engine.Editor
{
    public static class Day1InputFix
    {
        [MenuItem("SOP Engine/Day1/Fix UI Click Input (Standalone)")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene();

            // Ensure EventSystem exists
            var es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem));
                es = esGo.GetComponent<EventSystem>();
            }

            // Ensure StandaloneInputModule (works in Editor/mouse without needing InputActions)
            var standalone = es.GetComponent<StandaloneInputModule>();
            if (standalone == null)
            {
                standalone = es.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("Added StandaloneInputModule");
            }
            standalone.enabled = true;

            // Disable InputSystemUIInputModule if present (it needs an InputActionAsset configured)
            var inputSystemUIModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUIModuleType != null)
            {
                var inputSystem = es.GetComponent(inputSystemUIModuleType) as Behaviour;
                if (inputSystem != null)
                {
                    inputSystem.enabled = false;
                    Debug.Log("Disabled InputSystemUIInputModule");
                }
            }

            // Ensure Next button target graphic
            var next = GameObject.Find("Canvas/NextButton");
            if (next != null)
            {
                var button = next.GetComponent<Button>();
                var image = next.GetComponent<Image>();
                if (button != null && image != null)
                {
                    button.targetGraphic = image;
                    EditorUtility.SetDirty(button);
                    Debug.Log("Ensured NextButton.targetGraphic set");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
