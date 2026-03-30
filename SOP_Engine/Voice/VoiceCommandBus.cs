using System;
using UnityEngine;

namespace SOP_Engine.Voice
{
    /// <summary>
    /// Static event bus for voice-related signals.
    /// Keeps voice providers decoupled from the UI / state machine.
    /// </summary>
    public static class VoiceCommandBus
    {
        /// <summary>
        /// Fired when the system starts/stops actively recording audio.
        /// </summary>
        public static event Action<bool> ListeningStateChanged;

        /// <summary>
        /// Fired for debugging / UI display.
        /// </summary>
        public static event Action<string> TranscriptReceived;

        /// <summary>
        /// Fired when an intent/command is recognized (ex: "next", "repeat").
        /// </summary>
        public static event Action<string> CommandRecognized;

        public static void BroadcastListeningState(bool isListening)
        {
            ListeningStateChanged?.Invoke(isListening);
        }

        public static void BroadcastTranscript(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return;

            TranscriptReceived?.Invoke(transcript);
        }

        public static void BroadcastCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            CommandRecognized?.Invoke(command);
            Debug.Log($"[Voice] Command recognized: {command}");
        }
    }
}
