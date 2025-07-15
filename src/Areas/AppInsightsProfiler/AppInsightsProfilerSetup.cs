using AzureMcp.Areas.AppInsightsProfiler.Commands;
using AzureMcp.Areas.AppInsightsProfiler.Services;
using AzureMcp.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.AppInsightsProfiler;

public class MonitorSetup : IAreaSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAppInsightsProfilerDataplaneService, AppInsightsProfilerDataplaneService>();
    }

    public void RegisterCommands(CommandGroup rootGroup, ILoggerFactory loggerFactory)
    {
        CommandGroup commandGroup = new("profiler", "Azure Application Insights Profiler operations - Commands for profiling and analyzing application performance using Azure Application Insights.");
        rootGroup.AddSubGroup(commandGroup);

        commandGroup.AddCommand("list-insights", new ListInsightsCommand(loggerFactory.CreateLogger<ListInsightsCommand>()));
    }
}

