using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace SOP_Engine.Voice
{
    /// <summary>
    /// Requests runtime microphone permission on Android.
    /// (Android 6+ ignores manifest-only permissions until requested at runtime.)
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class MicPermission : MonoBehaviour
    {
        private void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                Permission.RequestUserPermission(Permission.Microphone);
#endif
        }
    }
}
