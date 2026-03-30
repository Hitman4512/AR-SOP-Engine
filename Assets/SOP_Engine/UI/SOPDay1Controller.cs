using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SOP_Engine.Core;
using SOP_Engine.Voice;

namespace SOP_Engine.UI
{
    public class SOPDay1Controller : MonoBehaviour
    {
        public event Action<SOPStep> StepChanged;

        [Header("JSON")]
        [SerializeField] private string sopFileName = "maintenance-001";

        [Header("UI")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject stepCardRoot;

        [Header("Completion UI")]
        [SerializeField] private SOPSummaryReportController summaryReport;

        [Header("Voice")]
        [SerializeField] private IntentResolver intentResolver;
        [SerializeField] private UnityKeywordProvider keywordProvider;
        [SerializeField] private TTSController tts;

        private SOPDocument _doc;
        private int _index;
        private bool _completed;

        [Header("Input Debounce")]
        [Tooltip("Prevents double-advance when a tap and a voice command happen at the same time.")]
        [SerializeField] private float inputCooldownSeconds = 0.75f;

        private float _nextAllowedInputTime;

        private void Awake()
        {
            // Prevent double-advance if the Button also has an inspector-bound OnClick.
            if (nextButton != null)
            {
                // Remove inspector-bound listeners too (persistent + runtime) to avoid double-advance.
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(NextStep);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(PreviousStep);
            }

            if (intentResolver == null)
                intentResolver = FindFirstObjectByType<IntentResolver>();

            if (keywordProvider == null)
                keywordProvider = FindFirstObjectByType<UnityKeywordProvider>();

            if (tts == null)
                tts = FindFirstObjectByType<TTSController>();

            if (stepCardRoot == null && titleText != null)
            {
                // Canvas/StepCard/TitleText -> StepCard
                var t = titleText.transform;
                if (t.parent != null && t.parent.parent != null)
                    stepCardRoot = t.parent.parent.gameObject;
            }

            if (summaryReport == null)
                summaryReport = FindFirstObjectByType<SOPSummaryReportController>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            VoiceCommandBus.CommandRecognized += OnVoiceCommand;

            if (tts != null)
            {
                tts.SpeechStarted += OnTtsStarted;
                tts.SpeechCompleted += OnTtsCompleted;
            }
        }

        private void OnDisable()
        {
            VoiceCommandBus.CommandRecognized -= OnVoiceCommand;

            if (tts != null)
            {
                tts.SpeechStarted -= OnTtsStarted;
                tts.SpeechCompleted -= OnTtsCompleted;
            }
        }

        private void Start()
        {
            _doc = null;
            _index = 0;
            _completed = false;

            if (summaryReport != null)
                summaryReport.gameObject.SetActive(false);

            // StreamingAssets on Android is not a normal filesystem path; load via UnityWebRequest.
            StartCoroutine(SOPLoader.LoadAsync(sopFileName, doc =>
            {
                _doc = doc;
                _index = 0;
                _completed = false;

                Debug.Log(_doc != null
                    ? $"Loaded SOP '{_doc.SopId}' with {_doc.Steps.Count} steps."
                    : $"Failed to load SOP '{sopFileName}'.");

                Refresh();
            }));

            // Show a loading state until the async load finishes.
            Refresh();
        }

        public void NextStep()
        {
            if (_completed)
                return;

            if (Time.unscaledTime < _nextAllowedInputTime)
                return;

            _nextAllowedInputTime = Time.unscaledTime + inputCooldownSeconds;

            if (_doc == null || _doc.Steps == null || _doc.Steps.Count == 0)
            {
                Debug.LogWarning("NextStep called but SOP is not loaded / has no steps.");
                return;
            }

            // If user says "Next" on the final step, treat that as completion.
            if (_index >= _doc.Steps.Count - 1)
            {
                CompleteSop();
                return;
            }

            _index = Mathf.Clamp(_index + 1, 0, _doc.Steps.Count - 1);
            Debug.Log($"Advanced to step index {_index + 1}/{_doc.Steps.Count}");
            Refresh();
        }

        public void PreviousStep()
        {
            if (_completed)
                return;

            if (Time.unscaledTime < _nextAllowedInputTime)
                return;

            _nextAllowedInputTime = Time.unscaledTime + inputCooldownSeconds;

            if (_doc == null || _doc.Steps == null || _doc.Steps.Count == 0)
                return;

            _index = Mathf.Clamp(_index - 1, 0, _doc.Steps.Count - 1);
            Debug.Log($"Went back to step index {_index + 1}/{_doc.Steps.Count}");
            Refresh();
        }

        private void Refresh()
        {
            if (titleText == null || instructionText == null)
                return;

            if (_doc == null || _doc.Steps == null || _doc.Steps.Count == 0)
            {
                titleText.text = "SOP not loaded";
                instructionText.text = $"Could not load: {sopFileName}.json (StreamingAssets/SOPS)";
                if (nextButton != null) nextButton.interactable = false;
                return;
            }

            var step = _doc.Steps[_index];
            titleText.text = step.Title;
            instructionText.text = step.Instruction;

            if (nextButton != null)
                nextButton.interactable = _index < _doc.Steps.Count - 1;

            if (backButton != null)
                backButton.interactable = _index > 0;

            StepChanged?.Invoke(step);
            EnterStep(step);
        }

        private void EnterStep(SOPStep step)
        {
            if (step == null)
                return;

            // Constrain commands per-step (drives both intent resolver + keyword recognizer).
            var allowed = new System.Collections.Generic.List<string> { "next", "repeat" };

            if (step.Commands != null) allowed.AddRange(step.Commands);
            if (step.AllowedCommands != null) allowed.AddRange(step.AllowedCommands);

            allowed = allowed
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            intentResolver?.SetGrammar(allowed);
            keywordProvider?.SetKeywords(allowed);

            // Hands-free loop:
            // - stop listening while TTS talks
            // - start listening once TTS is done (VoiceIndicator will pulse while listening)
            keywordProvider?.StopListening();
            tts?.Speak(step.Instruction);
        }

        private void OnTtsStarted()
        {
            keywordProvider?.StopListening();
        }

        private void OnTtsCompleted()
        {
            if (_completed)
                return;

            keywordProvider?.StartListening();
        }

        private void OnVoiceCommand(string command)
        {
            if (_completed)
                return;

            if (Time.unscaledTime < _nextAllowedInputTime)
                return;

            if (string.IsNullOrWhiteSpace(command))
                return;

            command = command.Trim().ToLowerInvariant();

            switch (command)
            {
                case "next":
                    NextStep();
                    break;

                case "back":
                case "previous":
                    PreviousStep();
                    break;

                case "repeat":
                {
                    var step = GetCurrentStep();
                    if (step != null)
                    {
                        keywordProvider?.StopListening();
                        tts?.Speak(step.Instruction);
                    }
                    break;
                }

                case "done":
                {
                    // "Done" is the verify/finish confirmation.
                    CompleteSop();
                    break;
                }
            }
        }

        private void CompleteSop()
        {
            if (_completed)
                return;

            _completed = true;
            keywordProvider?.StopListening();

            // Proof-of-work: write a local completion record each time a technician finishes a job.
            SOPLoader_CompletionLog();

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            if (backButton != null)
                backButton.gameObject.SetActive(false);

            if (stepCardRoot != null)
                stepCardRoot.SetActive(false);

            if (summaryReport != null)
                summaryReport.Show(_doc);
            else
                Debug.LogWarning("SummaryReport not found in scene; SOP completed but no summary UI to show.");

            // Confirmation audio
            tts?.Speak("SOP complete. All steps have been recorded. You may now exit the app.");
        }

        private void SOPLoader_CompletionLog()
        {
            // Kept in a method to avoid breaking day-1 scenes if logger is removed.
            CompletionLogger.LogCompletion(_doc);
        }

        public SOPStep GetCurrentStep()
        {
            if (_doc == null || _doc.Steps == null || _doc.Steps.Count == 0)
                return null;

            return _doc.Steps[_index];
        }
    }
}
