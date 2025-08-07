using AzureMcp.ApplicationInsights.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AzureMcp.ApplicationInsights;

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
        services
            .AddSingleton<DiagServiceClientFactory>()
            .AddSingleton<IProfilerInsightsService, ProfilerInsightsService>();

        return services;
    }
}
