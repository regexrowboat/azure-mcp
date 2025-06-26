// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Areas.Monitor.Commands.App;
using AzureMcp.Areas.Monitor.Commands.Metrics;
using AzureMcp.Areas.Monitor.Commands.Table;
using AzureMcp.Areas.Monitor.Commands.TableType;
using AzureMcp.Areas.Monitor.Commands.Workspace;
using AzureMcp.Areas.Monitor.Models;
using AzureMcp.Areas.Monitor.Options.App;

namespace AzureMcp.Commands.Monitor;

[JsonSerializable(typeof(WorkspaceListCommand.WorkspaceListCommandResult))]
[JsonSerializable(typeof(TableListCommand.TableListCommandResult))]
[JsonSerializable(typeof(TableTypeListCommand.TableTypeListCommandResult))]
[JsonSerializable(typeof(MetricsQueryCommand.MetricsQueryCommandResult))]
[JsonSerializable(typeof(MetricsDefinitionsCommand.MetricsDefinitionsCommandResult))]
[JsonSerializable(typeof(List<AppCorrelateDataSet>))]
[JsonSerializable(typeof(AppCorrelateTimeCommand.AppCorrelateCommandResult))]
[JsonSerializable(typeof(double[]))]
[JsonSerializable(typeof(AppGetTraceCommand.AppGetTraceCommandResult))]
[JsonSerializable(typeof(AppListTraceCommand.AppListTraceCommandResult))]
[JsonSerializable(typeof(AppImpactCommand.AppImpactCommandResult))]
[JsonSerializable(typeof(AppGetSpanCommand.AppGetSpanCommandResult))]
[JsonSerializable(typeof(List<TraceIdEntry>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class MonitorJsonContext : JsonSerializerContext
{
}
