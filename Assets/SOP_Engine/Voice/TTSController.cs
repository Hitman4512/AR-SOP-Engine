using System;
using System.Collections;
using UnityEngine;

namespace SOP_Engine.Voice
{
    /// <summary>
    /// Minimal TTS bridge.
    /// - Android: uses android.speech.tts.TextToSpeech via AndroidJavaObject.
    /// - Editor/Other: logs the text.
    ///
    /// Demo flow note:
    /// For a product-like hands-free loop we expose a "speech completed" event.
    /// This implementation uses a simple time estimate so it works across platforms
    /// without additional Java listener plumbing.
    /// </summary>
    public class TTSController : MonoBehaviour
    {
        public event Action SpeechStarted;
        public event Action SpeechCompleted;

        [SerializeField] private bool speakOnStart = false;
        [TextArea(2, 5)]
        [SerializeField] private string startText = "";

        [Tooltip("Seconds per character used to estimate when TTS has finished.")]
        [SerializeField] private float secondsPerCharacter = 0.045f;

        [Tooltip("Clamp the estimate to avoid extremely short/long waits.")]
        [SerializeField] private Vector2 estimateClampSeconds = new(1.0f, 12.0f);

        public bool IsSpeaking { get; private set; }

        private bool _initialized;
        private Coroutine _estimateRoutine;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _tts;
        private AndroidJavaObject _activity;
#endif

        private void Start()
        {
            Initialize();

            if (speakOnStart && !string.IsNullOrWhiteSpace(startText))
                Speak(startText);
        }

        public void Initialize()
        {
            if (_initialized)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                _activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                // new TextToSpeech(activity, onInitListener)
                _tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", _activity, new OnInitListenerProxy(() =>
                {
                    _initialized = true;
                }));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TTS] Failed to initialize Android TTS: {e.Message}");
                _initialized = true; // fall back to log-only
            }
#else
            _initialized = true;
#endif
        }

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (!_initialized)
                Initialize();

            SpeechStarted?.Invoke();
            IsSpeaking = true;

            if (_estimateRoutine != null)
                StopCoroutine(_estimateRoutine);

            _estimateRoutine = StartCoroutine(EstimateCompletion(text));

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_tts == null)
            {
                Debug.Log($"[TTS] {text}");
                return;
            }

            // speak(CharSequence text, int queueMode, Bundle params, String utteranceId)
            _tts.Call<int>("speak", text, 0, null, System.Guid.NewGuid().ToString());
#else
            Debug.Log($"[TTS] {text}");
#endif
        }

        private IEnumerator EstimateCompletion(string text)
        {
            var seconds = Mathf.Clamp(text.Length * secondsPerCharacter, estimateClampSeconds.x, estimateClampSeconds.y);
            yield return new WaitForSecondsRealtime(seconds);

            IsSpeaking = false;
            _estimateRoutine = null;
            SpeechCompleted?.Invoke();
        }

        public void Stop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _tts?.Call("stop");
#endif

            if (_estimateRoutine != null)
                StopCoroutine(_estimateRoutine);

            _estimateRoutine = null;
            IsSpeaking = false;
        }

        private void OnDestroy()
        {
            Stop();

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                _tts?.Call("shutdown");
            }
            catch { }
            _tts = null;
            _activity = null;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private class OnInitListenerProxy : AndroidJavaProxy
        {
            private readonly System.Action _onInit;

            public OnInitListenerProxy(System.Action onInit)
                : base("android.speech.tts.TextToSpeech$OnInitListener")
            {
                _onInit = onInit;
            }

            public void onInit(int status)
            {
                _onInit?.Invoke();
            }
        }
#endif
    }
}
