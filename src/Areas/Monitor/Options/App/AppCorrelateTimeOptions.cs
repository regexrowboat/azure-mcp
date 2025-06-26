// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Areas.Monitor.Models;

namespace AzureMcp.Areas.Monitor.Options.App;

public class AppCorrelateTimeOptions : BaseAppOptions
{
    /// <summary>
    /// The user-reported description of the problem occurring. Required.
    /// </summary>
    [JsonPropertyName(MonitorOptionDefinitions.App.SymptomName)]
    public string? Symptom { get; set; } = string.Empty;

    /// <summary>
    /// The start time of the investigation in ISO format (optional).
    /// </summary>
    [JsonPropertyName(MonitorOptionDefinitions.App.StartTimeName)]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// The end time of the investigation in ISO format (optional).
    /// </summary>
    [JsonPropertyName(MonitorOptionDefinitions.App.EndTimeName)]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// The list of data sets to correlate.
    /// </summary>
    [JsonPropertyName(MonitorOptionDefinitions.App.CorrelationDataSetName)]
    public List<AppCorrelateDataSet> DataSets { get; set; } = new();
}
