// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Areas.Monitor.Options;

namespace AzureMcp.Areas.ApplicationInsights.Options;

/// <summary>
/// Base options for all Application Insights App commands
/// </summary>
public class BaseAppOptions : BaseMonitorOptions, IAppOptions
{
    /// <summary>
    /// The name of the Application Insights resource.
    /// </summary>
    [JsonPropertyName(ApplicationInsightsOptionDefinitions.ResourceNameName)]
    public string? ResourceName { get; set; }

    /// <summary>
    /// The name of the Azure resource group containing the Application Insights resource.
    /// </summary>
    [JsonPropertyName(ApplicationInsightsOptionDefinitions.ResourceGroupName)]
    public new string? ResourceGroup { get; set; }

    /// <summary>
    /// The resource ID of the Application Insights resource.
    /// </summary>
    [JsonPropertyName(ApplicationInsightsOptionDefinitions.ResourceIdName)]
    public string? ResourceId { get; set; }
}
