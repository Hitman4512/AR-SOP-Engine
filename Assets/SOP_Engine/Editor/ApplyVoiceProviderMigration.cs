#if UNITY_EDITOR
using SOP_Engine.Voice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SOP_Engine.Editor
{
    public static class ApplyVoiceProviderMigration
    {
        [MenuItem("SOP Engine/Migrate Voice Provider To UnityKeywordProvider")]
        public static void Execute()
        {
            var scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var voiceGo = GameObject.Find("Voice");
            if (voiceGo == null)
                voiceGo = new GameObject("Voice");

            // Remove old provider if present.
            var google = voiceGo.GetComponent<GoogleSTTProvider>();
            if (google != null)
                Object.DestroyImmediate(google, true);

            // Ensure required components.
            if (voiceGo.GetComponent<IntentResolver>() == null)
                voiceGo.AddComponent<IntentResolver>();

            if (voiceGo.GetComponent<UnityKeywordProvider>() == null)
                voiceGo.AddComponent<UnityKeywordProvider>();

            if (voiceGo.GetComponent<TTSController>() == null)
                voiceGo.AddComponent<TTSController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Voice] Migration complete: SampleScene now uses UnityKeywordProvider.");
        }
    }
}
#endif
