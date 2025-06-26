// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Options.App;

public class AppFocusOptions : BaseAppOptions
{
    [JsonPropertyName(MonitorOptionDefinitions.App.SymptomName)]
    public string? Symptom { get; set; }

    [JsonPropertyName(MonitorOptionDefinitions.App.StartTimeName)]
    public string? StartTime { get; set; }

    [JsonPropertyName(MonitorOptionDefinitions.App.EndTimeName)]
    public string? EndTime { get; set; }
}
