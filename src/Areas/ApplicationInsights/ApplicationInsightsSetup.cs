using AzureMcp.Areas.ApplicationInsights.Commands;
using AzureMcp.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.ApplicationInsights;

public class ApplicationInsightsSetup : IAreaSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAppDiagnoseServices()
            .AddProfilerServices();
    }

    public void RegisterCommands(CommandGroup rootGroup, ILoggerFactory loggerFactory)
    {
        RegisterAppDiagnoseCommands(rootGroup, loggerFactory);
        RegisterProfilerCommands(rootGroup, loggerFactory);
    }

    private static void RegisterAppDiagnoseCommands(CommandGroup rootGroup, ILoggerFactory loggerFactory)
    {
        // App diagnose commands
        var app = new CommandGroup("applicationinsights", "Application Insights operations - Commands for diagnosing problems with applications monitored with Application Insights.");
        app.AddCommand("correlate-time", new AppCorrelateTimeCommand(loggerFactory.CreateLogger<AppCorrelateTimeCommand>()));
        app.AddCommand("get-impact", new AppImpactCommand(loggerFactory.CreateLogger<AppImpactCommand>()));
        app.AddCommand("get-trace", new AppGetTraceCommand(loggerFactory.CreateLogger<AppGetTraceCommand>()));
        app.AddCommand("list-traces", new AppListTraceCommand(loggerFactory.CreateLogger<AppListTraceCommand>()));

        rootGroup.AddSubGroup(app);
    }

    private static void RegisterProfilerCommands(CommandGroup rootGroup, ILoggerFactory loggerFactory)
    {
        // Profiler commands
        CommandGroup profilerGroup = new("profiler", "Azure Application Insights Profiler operations - Commands for profiling and analyzing application performance using Azure Application Insights.");

        profilerGroup.AddCommand("list-insights", new ListInsightsCommand(loggerFactory.CreateLogger<ListInsightsCommand>()));

        rootGroup.AddSubGroup(profilerGroup);
    }
}
