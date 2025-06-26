// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Options.App;

/// <summary>
/// Base options for all Application Insights App commands
/// </summary>
public class BaseAppOptions : BaseMonitorOptions, IAppOptions
{
    /// <summary>
    /// The name of the Application Insights resource.
    /// </summary>
    [JsonPropertyName(MonitorOptionDefinitions.App.ResourceNameName)]
    public string? ResourceName { get; set; }

    /// <summary>
    /// The name of the Azure resource group containing the Application Insights resource.
    /// </summary>
    [JsonPropertyName(MonitorOptionDefinitions.App.ResourceGroupName)]
    public new string? ResourceGroup { get; set; }

    /// <summary>
    /// The resource ID of the Application Insights resource.
    /// </summary>
    [JsonPropertyName(MonitorOptionDefinitions.App.ResourceIdName)]
    public string? ResourceId { get; set; }

    [JsonPropertyName(MonitorOptionDefinitions.App.IntentName)]
    public string? Intent { get; set; }
}
