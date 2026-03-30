using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using UnityEngine.Windows.Speech;
#endif

namespace SOP_Engine.Voice
{
    /// <summary>
    /// Offline voice provider using Unity's built-in keyword spotting.
    /// 
    /// Notes:
    /// - This uses <see cref="UnityEngine.Windows.Speech.KeywordRecognizer"/>, which is supported on Windows.
    /// - Keywords must be provided (and refreshed) whenever the SOP step changes.
    /// </summary>
    public class UnityKeywordProvider : MonoBehaviour, IVoiceProvider
    {
        [Header("Keywords")]
        [Tooltip("Optional default keywords. Usually driven per-step by SOPDay1Controller/state machine.")]
        [SerializeField] private List<string> defaultKeywords = new() { "next", "repeat" };

        [Header("Flow")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private IntentResolver intentResolver;

        public bool IsListening { get; private set; }
        public event Action<string> TranscriptReceived;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private KeywordRecognizer _recognizer;
#endif

        private readonly HashSet<string> _keywords = new(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            if (intentResolver == null)
                intentResolver = GetComponent<IntentResolver>();

            SetKeywords(defaultKeywords);
        }

        private void OnEnable()
        {
            if (autoStart)
                StartListening();
        }

        private void OnDisable()
        {
            StopListening();
        }

        public void StartListening()
        {
            if (IsListening)
                return;

            IsListening = true;
            VoiceCommandBus.BroadcastListeningState(true);

            RestartRecognizerIfNeeded();
        }

        public void StopListening()
        {
            if (!IsListening)
                return;

            IsListening = false;
            VoiceCommandBus.BroadcastListeningState(false);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (_recognizer != null)
            {
                try
                {
                    _recognizer.OnPhraseRecognized -= OnPhraseRecognized;

                    if (_recognizer.IsRunning)
                        _recognizer.Stop();

                    _recognizer.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Voice] UnityKeywordProvider: failed stopping recognizer: {ex.Message}");
                }
                finally
                {
                    _recognizer = null;
                }
            }
#endif
        }

        /// <summary>
        /// Refresh which keywords the recognizer is listening for. Call this on every SOP step change.
        /// </summary>
        public void SetKeywords(IEnumerable<string> keywords)
        {
            _keywords.Clear();

            if (keywords != null)
            {
                foreach (var k in keywords)
                {
                    if (string.IsNullOrWhiteSpace(k))
                        continue;

                    _keywords.Add(k.Trim());
                }
            }

            if (IsListening)
                RestartRecognizerIfNeeded();
        }

        private void RestartRecognizerIfNeeded()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // KeywordRecognizer requires a non-empty keyword array.
            var keys = _keywords.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (keys.Length == 0)
            {
                Debug.LogWarning("[Voice] UnityKeywordProvider: keyword list is empty; nothing to listen for.");
                return;
            }

            // Recreate recognizer any time the keyword set changes.
            StopRecognizerOnly();

            _recognizer = new KeywordRecognizer(keys, ConfidenceLevel.Medium);
            _recognizer.OnPhraseRecognized += OnPhraseRecognized;
            _recognizer.Start();
#else
            Debug.LogWarning("[Voice] UnityKeywordProvider: KeywordRecognizer is not supported on this platform/build target.");
#endif
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private void StopRecognizerOnly()
        {
            if (_recognizer == null)
                return;

            try
            {
                _recognizer.OnPhraseRecognized -= OnPhraseRecognized;

                if (_recognizer.IsRunning)
                    _recognizer.Stop();

                _recognizer.Dispose();
            }
            catch
            {
                // ignore
            }
            finally
            {
                _recognizer = null;
            }
        }

        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            var keyword = args.text;
            if (string.IsNullOrWhiteSpace(keyword))
                return;

            TranscriptReceived?.Invoke(keyword);
            intentResolver?.HandleTranscript(keyword);
        }
#endif
    }
}
