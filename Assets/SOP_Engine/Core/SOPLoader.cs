using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace SOP_Engine.Core
{
    public static class SOPLoader
    {
        public static string GetSopsDirectoryPath()
        {
            return Path.Combine(Application.streamingAssetsPath, "SOPS");
        }

        public static string GetSopFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("fileName is null/empty", nameof(fileName));

            // Allow passing either "maintenance-001" or "maintenance-001.json".
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                fileName += ".json";

            return Path.Combine(GetSopsDirectoryPath(), fileName);
        }

        /// <summary>
        /// Android-safe asynchronous load (StreamingAssets is not a normal filesystem path on Android).
        /// Use as: StartCoroutine(SOPLoader.LoadAsync("maintenance-001", doc => { ... }));
        /// </summary>
        public static IEnumerator LoadAsync(string fileName, Action<SOPDocument> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            var path = GetSopFilePath(fileName);

            // On Android, StreamingAssets are inside the APK (jar:file://...).
            // On desktop, it's a normal path. UnityWebRequest supports both, but desktop needs a file:// URL.
            var url = path.Contains("://", StringComparison.Ordinal)
                ? path
                : new Uri(path).AbsoluteUri;

            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load SOP JSON via UnityWebRequest. URL: {url} Error: {req.error}");
                onCompleted(null);
                yield break;
            }

            try
            {
                var json = req.downloadHandler.text;
                var doc = JsonConvert.DeserializeObject<SOPDocument>(json);

                if (doc == null)
                {
                    Debug.LogError($"Failed to deserialize SOP JSON (null). URL: {url}");
                    onCompleted(null);
                    yield break;
                }

                if (doc.Steps == null)
                    doc.Steps = new();

                onCompleted(doc);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                onCompleted(null);
            }
        }

        /// <summary>
        /// Synchronous load (editor/standalone only). Avoid on Android.
        /// </summary>
        public static SOPDocument Load(string fileName)
        {
            var path = GetSopFilePath(fileName);

            if (!File.Exists(path))
            {
                Debug.LogError($"SOP file not found at: {path}");
                return null;
            }

            try
            {
                var json = File.ReadAllText(path);
                var doc = JsonConvert.DeserializeObject<SOPDocument>(json);

                if (doc == null)
                {
                    Debug.LogError($"Failed to deserialize SOP JSON (null). Path: {path}");
                    return null;
                }

                if (doc.Steps == null)
                    doc.Steps = new();

                return doc;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }
    }
}
