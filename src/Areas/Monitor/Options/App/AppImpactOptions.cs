// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Options.App;

public class AppImpactOptions : BaseAppOptions
{
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
    /// The table to list traces on
    /// </summary>
    [JsonPropertyName(MonitorOptionDefinitions.App.TableName)]
    public string? Table { get; set; }

    /// <summary>
    /// Filters for the traces
    /// </summary>
    [JsonPropertyName(MonitorOptionDefinitions.App.FiltersName)]
    public string? Filters { get; set; }
}
