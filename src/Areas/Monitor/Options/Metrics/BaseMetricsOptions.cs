// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Models.Option;
using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Options.Metrics;

/// <summary>
/// Base options for all metrics commands
/// </summary>
public class BaseMetricsOptions : BaseMonitorOptions, IMetricsOptions
{
    /// <summary>
    /// The resource type (optional, e.g., 'Microsoft.Storage/storageAccounts')
    /// </summary>
    [JsonPropertyName(OptionDefinitions.Common.ResourceTypeName)]
    public string? ResourceType { get; set; }

    /// <summary>
    /// The resource name (required)
    /// </summary>
    [JsonPropertyName(OptionDefinitions.Common.ResourceNameName)]
    public string? ResourceName { get; set; }

    /// <summary>
    /// The metric namespace to query
    /// </summary>
    [JsonPropertyName(MonitorOptionDefinitions.Metrics.MetricNamespaceName)]
    public string? MetricNamespace { get; set; }
}
