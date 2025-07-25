// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.Monitor.Commands.HealthModels.Entity;
using AzureMcp.Areas.Monitor.Commands.Log;
using AzureMcp.Areas.Monitor.Commands.Metrics;
using AzureMcp.Areas.Monitor.Commands.Table;
using AzureMcp.Areas.Monitor.Commands.TableType;
using AzureMcp.Areas.Monitor.Commands.Workspace;
using AzureMcp.Areas.Monitor.Services;
using AzureMcp.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Monitor;

public class MonitorSetup : IAreaSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMonitorService, MonitorService>();
        services.AddSingleton<IMonitorHealthModelService, MonitorHealthModelService>();
        services.AddSingleton<IMetricsQueryClientService, MetricsQueryClientService>();
        services.AddSingleton<IMonitorMetricsService, MonitorMetricsService>();
    }

    public void RegisterCommands(CommandGroup rootGroup, ILoggerFactory loggerFactory)
    {
        // Create Monitor command group
        var monitor = new CommandGroup("monitor", "Azure Monitor operations - Commands for querying and analyzing Azure Monitor logs and metrics.");
        rootGroup.AddSubGroup(monitor);

        // Create Monitor subgroups
        var workspaces = new CommandGroup("workspace", "Log Analytics workspace operations - Commands for managing Log Analytics workspaces.");
        monitor.AddSubGroup(workspaces);

        var resources = new CommandGroup("resource", "Azure Monitor resource operations - Commands for resource-centric operations.");
        monitor.AddSubGroup(resources);

        var monitorTable = new CommandGroup("table", "Log Analytics workspace table operations - Commands for listing tables in Log Analytics workspaces.");
        monitor.AddSubGroup(monitorTable);

        var monitorTableType = new CommandGroup("type", "Log Analytics workspace table type operations - Commands for listing table types in Log Analytics workspaces.");
        monitorTable.AddSubGroup(monitorTableType);

        var workspaceLogs = new CommandGroup("log", "Azure Monitor logs operations - Commands for querying Log Analytics workspaces using KQL.");
        workspaces.AddSubGroup(workspaceLogs);

        var resourceLogs = new CommandGroup("log", "Azure Monitor resource logs operations - Commands for querying resource logs using KQL.");
        resources.AddSubGroup(resourceLogs);

        // Register Monitor commands

        workspaceLogs.AddCommand("query", new WorkspaceLogQueryCommand(loggerFactory.CreateLogger<WorkspaceLogQueryCommand>()));
        resourceLogs.AddCommand("query", new ResourceLogQueryCommand(loggerFactory.CreateLogger<ResourceLogQueryCommand>()));

        workspaces.AddCommand("list", new WorkspaceListCommand(loggerFactory.CreateLogger<WorkspaceListCommand>()));
        monitorTable.AddCommand("list", new TableListCommand(loggerFactory.CreateLogger<TableListCommand>()));

        monitorTableType.AddCommand("list", new TableTypeListCommand(loggerFactory.CreateLogger<TableTypeListCommand>()));

        var health = new CommandGroup("healthmodels", "Azure Monitor Health Models operations - Commands for working with Azure Monitor Health Models.");
        monitor.AddSubGroup(health);

        var entity = new CommandGroup("entity", "Entity operations - Commands for working with entities in Azure Monitor Health Models.");
        health.AddSubGroup(entity);

        entity.AddCommand("gethealth", new EntityGetHealthCommand(loggerFactory.CreateLogger<EntityGetHealthCommand>()));

        // Create Metrics command group and register commands
        var metrics = new CommandGroup("metrics", "Azure Monitor metrics operations - Commands for querying and analyzing Azure Monitor metrics.");
        monitor.AddSubGroup(metrics);

        metrics.AddCommand("query", new MetricsQueryCommand(loggerFactory.CreateLogger<MetricsQueryCommand>()));
        metrics.AddCommand("definitions", new MetricsDefinitionsCommand(loggerFactory.CreateLogger<MetricsDefinitionsCommand>()));
    }
}
