using System;
using System.IO;
using Newtonsoft.Json;
using SOP_Engine.Core;
using UnityEngine;

namespace SOP_Engine.Core
{
    /// <summary>
    /// Writes a local completion log whenever a technician finishes a job.
    /// Stored in Application.persistentDataPath for device-safe persistence.
    /// </summary>
    public static class CompletionLogger
    {
        [Serializable]
        private class CompletionEntry
        {
            public string sopId;
            public string title;
            public string version;
            public int stepCount;
            public string completedAtUtc;
        }

        public static string GetLogFilePath()
        {
            return Path.Combine(Application.persistentDataPath, "sop-completions.log");
        }

        public static void LogCompletion(SOPDocument doc)
        {
            try
            {
                var entry = new CompletionEntry
                {
                    sopId = doc?.SopId,
                    title = doc?.Title,
                    version = doc?.Version,
                    stepCount = doc?.Steps != null ? doc.Steps.Count : 0,
                    completedAtUtc = DateTime.UtcNow.ToString("o")
                };

                var line = JsonConvert.SerializeObject(entry);
                File.AppendAllText(GetLogFilePath(), line + Environment.NewLine);

                Debug.Log($"[CompletionLogger] Wrote completion log: {GetLogFilePath()}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
