using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SOP_Engine.Voice
{
    /// <summary>
    /// Lightweight intent recognizer using a constrained grammar (allowed keyword list per step).
    /// </summary>
    public class IntentResolver : MonoBehaviour
    {
        [Tooltip("If true, logs transcripts and resolution results.")]
        [SerializeField] private bool verboseLogging = true;

        private readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase);

        public void SetGrammar(IEnumerable<string> allowedKeywords)
        {
            _allowed.Clear();

            if (allowedKeywords == null)
                return;

            foreach (var k in allowedKeywords)
            {
                if (string.IsNullOrWhiteSpace(k))
                    continue;

                _allowed.Add(Normalize(k));
            }

            if (verboseLogging)
                Debug.Log($"[Voice] Grammar set: {( _allowed.Count == 0 ? "<empty>" : string.Join(", ", _allowed))}");
        }

        public string Resolve(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return "unknown";

            if (_allowed.Count == 0)
                return "unknown";

            var t = Normalize(transcript);

            // Basic keyword matching (contains). Could be upgraded to tokenization / fuzzy matching.
            foreach (var keyword in _allowed)
            {
                if (t.Contains(keyword))
                    return keyword;
            }

            return "unknown";
        }

        public void HandleTranscript(string transcript)
        {
            if (verboseLogging)
                Debug.Log($"[Voice] Transcript: {transcript}");

            VoiceCommandBus.BroadcastTranscript(transcript);

            var intent = Resolve(transcript);
            if (intent == "unknown")
            {
                if (verboseLogging)
                    Debug.Log("[Voice] Intent: unknown (ignored)");
                return;
            }

            VoiceCommandBus.BroadcastCommand(intent);
        }

        private static string Normalize(string s)
        {
            s = s.Trim().ToLowerInvariant();

            // Remove basic punctuation to make matching robust.
            var chars = s.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray();
            return new string(chars);
        }
    }
}
