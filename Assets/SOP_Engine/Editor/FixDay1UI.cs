#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SOP_Engine.Editor
{
    public static class FixDay1UI
    {
        [MenuItem("SOP Engine/Day1/Fix Next Button Wiring")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene();

            // Fix button target graphic
            var next = GameObject.Find("Canvas/NextButton");
            if (next != null)
            {
                var button = next.GetComponent<Button>();
                var image = next.GetComponent<Image>();
                if (button != null && image != null)
                {
                    button.targetGraphic = image;
                    EditorUtility.SetDirty(button);
                    Debug.Log("Ensured NextButton.targetGraphic -> Image");
                }
            }

            // Fix EventSystem input module if project uses the new Input System
            var es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (es != null)
            {
                // If InputSystemUIInputModule exists, prefer it over StandaloneInputModule.
                var inputSystemUIModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemUIModuleType != null)
                {
                    var standalone = es.GetComponent<StandaloneInputModule>();
                    var inputSystem = es.GetComponent(inputSystemUIModuleType);

                    if (inputSystem == null)
                    {
                        es.gameObject.AddComponent(inputSystemUIModuleType);
                        Debug.Log("Added InputSystemUIInputModule to EventSystem");
                    }

                    if (standalone != null)
                    {
                        UnityEngine.Object.DestroyImmediate(standalone);
                        Debug.Log("Removed StandaloneInputModule from EventSystem");
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
