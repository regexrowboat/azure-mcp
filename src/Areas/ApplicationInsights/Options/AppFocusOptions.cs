// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Areas.Monitor.Options;

namespace AzureMcp.Areas.ApplicationInsights.Options;

public class AppFocusOptions : BaseAppOptions
{
    [JsonPropertyName(ApplicationInsightsOptionDefinitions.SymptomName)]
    public string? Symptom { get; set; }

    [JsonPropertyName(ApplicationInsightsOptionDefinitions.StartTimeName)]
    public string? StartTime { get; set; }

    [JsonPropertyName(ApplicationInsightsOptionDefinitions.EndTimeName)]
    public string? EndTime { get; set; }
}
