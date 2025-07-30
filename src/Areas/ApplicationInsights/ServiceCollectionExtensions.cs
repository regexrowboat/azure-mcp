using AzureMcp.Areas.ApplicationInsights.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AzureMcp.Areas.ApplicationInsights;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the application insights services in the service collection.
    /// </summary>
    public static IServiceCollection AddAppDiagnoseServices(this IServiceCollection services)
    {
        services.AddSingleton<IAppLogsQueryService, AppLogsQueryService>();
        services.AddSingleton<IAppDiagnoseService, AppDiagnoseService>();

        return services;
    }

    /// <summary>
    /// Registers the profiler services in the service collection.
    /// </summary>
    public static IServiceCollection AddProfilerServices(this IServiceCollection services)
    {
        return services.AddSingleton<IProfilerDataplaneService, ProfilerDataplaneService>();
    }
}
