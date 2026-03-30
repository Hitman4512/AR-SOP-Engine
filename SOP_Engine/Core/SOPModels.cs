using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SOP_Engine.Core
{
    /// <summary>
    /// Day-1 baseline models for SOP JSON.
    /// Kept permissive so the project can iterate on the schema without breaking parsing.
    /// </summary>
    [Serializable]
    public class SOPDocument
    {
        [JsonProperty("sopId")] public string SopId;
        [JsonProperty("title")] public string Title;
        [JsonProperty("version")] public string Version;

        // Some schemas use "steps", others use "procedure" etc. We support only "steps" for now.
        [JsonProperty("steps")] public List<SOPStep> Steps = new();
    }

    [Serializable]
    public class SOPStep
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("stepNumber")] public int StepNumber;

        [JsonProperty("title")] public string Title;
        [JsonProperty("instruction")] public string Instruction;

        // AR annotations to be placed for this step.
        [JsonProperty("annotations")] public List<SOPAnnotation> Annotations;

        // Voice commands/keywords that are allowed for this step.
        // (Supports a couple of common field names to keep the JSON schema flexible.)
        [JsonProperty("commands")] public List<string> Commands;
        [JsonProperty("allowedCommands")] public List<string> AllowedCommands;

        // Optional future-facing fields.
        [JsonProperty("voice")] public string Voice;
        [JsonProperty("image")] public string Image;
        [JsonProperty("tags")] public List<string> Tags;
    }

    [Serializable]
    public class SOPAnnotation
    {
        [JsonProperty("label")] public string Label;

        // e.g. "arrow", "warning", etc.
        [JsonProperty("type")] public string Type;
    }
}
