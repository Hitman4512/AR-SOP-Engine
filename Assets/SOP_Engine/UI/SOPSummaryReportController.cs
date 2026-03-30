using System;
using System.Text;
using SOP_Engine.Core;
using TMPro;
using UnityEngine;

namespace SOP_Engine.UI
{
    public class SOPSummaryReportController : MonoBehaviour
    {
        public const string LastCompletedAtPlayerPrefsKey = "SOP_LAST_COMPLETED_AT_UTC";

        [Header("UI")]
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text checklistText;
        [SerializeField] private TMP_Text timeText;

        public void Show(SOPDocument doc)
        {
            gameObject.SetActive(true);

            if (headerText != null)
                headerText.text = "Job Success";

            if (checklistText != null)
                checklistText.text = BuildChecklist(doc);

            var utc = DateTime.UtcNow;
            PlayerPrefs.SetString(LastCompletedAtPlayerPrefsKey, utc.ToString("o"));
            PlayerPrefs.Save();

            if (timeText != null)
                timeText.text = $"Completed (UTC): {utc:yyyy-MM-dd HH:mm:ss}";
        }

        private static string BuildChecklist(SOPDocument doc)
        {
            if (doc == null || doc.Steps == null || doc.Steps.Count == 0)
                return "• No steps loaded";

            var sb = new StringBuilder();
            foreach (var s in doc.Steps)
            {
                sb.Append('•');
                sb.Append(' ');
                sb.Append(s.StepNumber);
                sb.Append(". ");
                sb.Append(string.IsNullOrWhiteSpace(s.Title) ? s.Id : s.Title);
                sb.Append("  ✓\n");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
