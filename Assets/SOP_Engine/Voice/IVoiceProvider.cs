using System;

namespace SOP_Engine.Voice
{
    public interface IVoiceProvider
    {
        bool IsListening { get; }

        event Action<string> TranscriptReceived;

        void StartListening();
        void StopListening();
    }
}
