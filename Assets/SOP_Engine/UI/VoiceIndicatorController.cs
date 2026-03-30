using System.Collections;
using SOP_Engine.Voice;
using UnityEngine;
using UnityEngine.UI;

namespace SOP_Engine.UI
{
    public class VoiceIndicatorController : MonoBehaviour
    {
        [SerializeField] private Image icon;

        [Header("Colors")]
        [SerializeField] private Color idleColor = new(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color listeningColor = new(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color successColor = new(0.2f, 1f, 0.2f, 1f);

        [Header("Pulse")]
        [SerializeField] private float pulseSpeed = 4f;
        [SerializeField] private float pulseScale = 0.15f;

        private bool _listening;
        private Coroutine _successRoutine;

        private void Reset()
        {
            icon = GetComponent<Image>();
        }

        private void OnEnable()
        {
            VoiceCommandBus.ListeningStateChanged += OnListeningState;
            VoiceCommandBus.CommandRecognized += OnCommand;

            ApplyIdle();
        }

        private void OnDisable()
        {
            VoiceCommandBus.ListeningStateChanged -= OnListeningState;
            VoiceCommandBus.CommandRecognized -= OnCommand;
        }

        private void Update()
        {
            if (!_listening)
                return;

            if (icon == null)
                return;

            var s = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseScale;
            icon.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        private void OnListeningState(bool listening)
        {
            _listening = listening;
            if (_listening)
                ApplyListening();
            else
                ApplyIdle();
        }

        private void OnCommand(string cmd)
        {
            if (_successRoutine != null)
                StopCoroutine(_successRoutine);

            _successRoutine = StartCoroutine(SuccessFlash());
        }

        private IEnumerator SuccessFlash()
        {
            if (icon != null)
                icon.color = successColor;

            yield return new WaitForSecondsRealtime(0.35f);

            if (_listening)
                ApplyListening();
            else
                ApplyIdle();
        }

        private void ApplyIdle()
        {
            if (icon == null)
                return;

            icon.color = idleColor;
            icon.rectTransform.localScale = Vector3.one;
        }

        private void ApplyListening()
        {
            if (icon == null)
                return;

            icon.color = listeningColor;
        }
    }
}
