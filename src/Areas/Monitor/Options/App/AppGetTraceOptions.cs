// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Options.App;

public class AppGetTraceOptions : BaseAppOptions
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

    [JsonPropertyName(MonitorOptionDefinitions.App.TraceIdName)]
    public string? TraceId { get; set; }

    [JsonPropertyName(MonitorOptionDefinitions.App.SpanIdName)]
    public string? SpanId { get; set; }
}
