using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SOP_Engine.Voice
{
    /// <summary>
    /// Records audio from the device microphone and sends it to Google Speech-to-Text (REST) to get a transcript.
    /// Requires a Google Cloud Speech-to-Text API key.
    /// </summary>
    public class GoogleSTTProvider : MonoBehaviour, IVoiceProvider
    {
        [Header("Google Speech-to-Text")]
        [SerializeField] private string googleApiKey = "";
        [SerializeField] private string languageCode = "en-US";
        [SerializeField] private int sampleRateHz = 16000;

        [Header("Recording")]
        [SerializeField] private string preferredMicDevice = "";
        [SerializeField, Range(1f, 10f)] private float maxRecordSeconds = 5f;
        [SerializeField, Range(0.001f, 0.1f)] private float startTalkingRmsThreshold = 0.01f;
        [SerializeField, Range(0.05f, 1.0f)] private float silenceSecondsToStop = 0.35f;

        [Header("Flow")]
        [SerializeField] private bool autoLoop = true;
        [SerializeField] private IntentResolver intentResolver;

        public bool IsListening { get; private set; }
        public event Action<string> TranscriptReceived;

        private AudioClip _clip;
        private string _device;
        private Coroutine _loopRoutine;

        private void Awake()
        {
            if (intentResolver == null)
                intentResolver = GetComponent<IntentResolver>();
        }

        private void OnEnable()
        {
            if (autoLoop)
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

            if (string.IsNullOrWhiteSpace(googleApiKey))
            {
                Debug.LogWarning("[Voice] GoogleSTTProvider: API key is empty. STT disabled.");
                return;
            }

            IsListening = true;
            VoiceCommandBus.BroadcastListeningState(true);

            _loopRoutine = StartCoroutine(ListenLoop());
        }

        public void StopListening()
        {
            if (!IsListening)
                return;

            IsListening = false;
            VoiceCommandBus.BroadcastListeningState(false);

            if (_loopRoutine != null)
            {
                StopCoroutine(_loopRoutine);
                _loopRoutine = null;
            }

            if (!string.IsNullOrEmpty(_device) && Microphone.IsRecording(_device))
                Microphone.End(_device);

            _clip = null;
        }

        private IEnumerator ListenLoop()
        {
            while (IsListening)
            {
                yield return RecordOneUtterance();

                // Small backoff to prevent hammering the mic / network.
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator RecordOneUtterance()
        {
            _device = ResolveMicDevice();
            if (string.IsNullOrEmpty(_device))
            {
                Debug.LogWarning("[Voice] No microphone device found.");
                yield return new WaitForSeconds(1f);
                yield break;
            }

            _clip = Microphone.Start(_device, false, Mathf.CeilToInt(maxRecordSeconds), sampleRateHz);

            // Wait for mic to start.
            var startTime = Time.realtimeSinceStartup;
            while (Microphone.GetPosition(_device) <= 0)
            {
                if (Time.realtimeSinceStartup - startTime > 1f)
                    break;
                yield return null;
            }

            // Wait until user starts speaking (RMS threshold) or until max duration.
            float timeSpeaking = 0f;
            float timeSilent = 0f;
            bool started = false;

            var sampleBuffer = new float[1024];

            while (true)
            {
                var pos = Microphone.GetPosition(_device);
                if (pos <= 0)
                {
                    yield return null;
                    continue;
                }

                var readPos = Mathf.Max(0, pos - sampleBuffer.Length);
                _clip.GetData(sampleBuffer, readPos);

                var rms = ComputeRms(sampleBuffer);

                if (!started)
                {
                    if (rms >= startTalkingRmsThreshold)
                        started = true;
                }
                else
                {
                    timeSpeaking += Time.unscaledDeltaTime;
                    if (rms < startTalkingRmsThreshold)
                        timeSilent += Time.unscaledDeltaTime;
                    else
                        timeSilent = 0f;

                    if (timeSilent >= silenceSecondsToStop)
                        break;
                }

                // Safety: stop at max duration.
                if (Time.realtimeSinceStartup - startTime >= maxRecordSeconds)
                    break;

                yield return null;
            }

            Microphone.End(_device);

            if (!started)
                yield break;

            var wavBytes = WavUtility.FromAudioClipToWav(_clip, sampleRateHz);
            if (wavBytes == null || wavBytes.Length == 0)
                yield break;

            var base64 = Convert.ToBase64String(wavBytes);

            yield return SendRecognizeRequest(base64);
        }

        private IEnumerator SendRecognizeRequest(string base64Wav)
        {
            var url = $"https://speech.googleapis.com/v1/speech:recognize?key={googleApiKey}";

            var payload = new SpeechRequest
            {
                config = new SpeechConfig
                {
                    encoding = "LINEAR16",
                    sampleRateHertz = sampleRateHz,
                    languageCode = languageCode,
                    enableAutomaticPunctuation = true
                },
                audio = new SpeechAudio { content = base64Wav }
            };

            var json = JsonUtility.ToJson(payload);
            using var req = new UnityWebRequest(url, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Voice] STT request failed: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
                yield break;
            }

            var responseJson = req.downloadHandler.text;
            var transcript = SpeechResponseParser.TryGetFirstTranscript(responseJson);
            if (string.IsNullOrWhiteSpace(transcript))
                yield break;

            TranscriptReceived?.Invoke(transcript);
            intentResolver?.HandleTranscript(transcript);
        }

        private string ResolveMicDevice()
        {
            if (!string.IsNullOrWhiteSpace(preferredMicDevice))
                return preferredMicDevice;

            if (Microphone.devices == null || Microphone.devices.Length == 0)
                return null;

            return Microphone.devices[0];
        }

        private static float ComputeRms(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return 0f;

            double sum = 0;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];

            return (float)Math.Sqrt(sum / samples.Length);
        }

        [Serializable]
        private class SpeechRequest
        {
            public SpeechConfig config;
            public SpeechAudio audio;
        }

        [Serializable]
        private class SpeechConfig
        {
            public string encoding;
            public int sampleRateHertz;
            public string languageCode;
            public bool enableAutomaticPunctuation;
        }

        [Serializable]
        private class SpeechAudio
        {
            public string content;
        }

        /// <summary>
        /// Minimal JSON transcript extraction without bringing in a full JSON parser.
        /// Google response includes: results[0].alternatives[0].transcript
        /// </summary>
        private static class SpeechResponseParser
        {
            public static string TryGetFirstTranscript(string json)
            {
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                const string key = "\"transcript\"";
                var i = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (i < 0)
                    return null;

                i = json.IndexOf(':', i);
                if (i < 0)
                    return null;

                // Find first quote after ':'
                i = json.IndexOf('"', i);
                if (i < 0)
                    return null;

                var j = json.IndexOf('"', i + 1);
                if (j < 0)
                    return null;

                return json.Substring(i + 1, j - (i + 1));
            }
        }

        /// <summary>
        /// Minimal WAV encoder for mono 16-bit PCM.
        /// </summary>
        private static class WavUtility
        {
            public static byte[] FromAudioClipToWav(AudioClip clip, int hz)
            {
                if (clip == null)
                    return null;

                var samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);

                // Convert float samples (-1..1) to 16-bit PCM
                var pcm = new byte[samples.Length * 2];
                int offset = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    var v = Mathf.Clamp(samples[i], -1f, 1f);
                    short s = (short)Mathf.RoundToInt(v * short.MaxValue);
                    pcm[offset++] = (byte)(s & 0xff);
                    pcm[offset++] = (byte)((s >> 8) & 0xff);
                }

                int channels = clip.channels;
                int byteRate = hz * channels * 2;
                int blockAlign = channels * 2;
                int subChunk2Size = pcm.Length;
                int chunkSize = 36 + subChunk2Size;

                var wav = new byte[44 + subChunk2Size];

                // RIFF header
                WriteAscii(wav, 0, "RIFF");
                WriteInt(wav, 4, chunkSize);
                WriteAscii(wav, 8, "WAVE");

                // fmt chunk
                WriteAscii(wav, 12, "fmt ");
                WriteInt(wav, 16, 16); // PCM
                WriteShort(wav, 20, 1); // audio format
                WriteShort(wav, 22, (short)channels);
                WriteInt(wav, 24, hz);
                WriteInt(wav, 28, byteRate);
                WriteShort(wav, 32, (short)blockAlign);
                WriteShort(wav, 34, 16); // bits per sample

                // data chunk
                WriteAscii(wav, 36, "data");
                WriteInt(wav, 40, subChunk2Size);

                Buffer.BlockCopy(pcm, 0, wav, 44, pcm.Length);
                return wav;
            }

            private static void WriteAscii(byte[] buffer, int offset, string text)
            {
                var bytes = Encoding.ASCII.GetBytes(text);
                Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
            }

            private static void WriteInt(byte[] buffer, int offset, int value)
            {
                buffer[offset + 0] = (byte)(value & 0xff);
                buffer[offset + 1] = (byte)((value >> 8) & 0xff);
                buffer[offset + 2] = (byte)((value >> 16) & 0xff);
                buffer[offset + 3] = (byte)((value >> 24) & 0xff);
            }

            private static void WriteShort(byte[] buffer, int offset, short value)
            {
                buffer[offset + 0] = (byte)(value & 0xff);
                buffer[offset + 1] = (byte)((value >> 8) & 0xff);
            }
        }
    }
}
