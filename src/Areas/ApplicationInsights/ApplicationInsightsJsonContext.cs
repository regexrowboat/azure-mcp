// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Areas.ApplicationInsights.Commands;
using AzureMcp.Areas.ApplicationInsights.Models;
using AzureMcp.Areas.ApplicationInsights.Options;

namespace AzureMcp.Areas.ApplicationInsights;

[JsonSerializable(typeof(List<AppCorrelateDataSet>))]
[JsonSerializable(typeof(AppCorrelateTimeCommand.AppCorrelateCommandResult))]
[JsonSerializable(typeof(AppListTraceCommand.AppListTraceCommandResult))]
[JsonSerializable(typeof(AppImpactCommand.AppImpactCommandResult))]
[JsonSerializable(typeof(AppGetTraceCommand.AppGetTraceCommandResult))]
[JsonSerializable(typeof(List<TraceIdEntry>))]
[JsonSerializable(typeof(double[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class ApplicationInsightsJsonContext : JsonSerializerContext
{
}
