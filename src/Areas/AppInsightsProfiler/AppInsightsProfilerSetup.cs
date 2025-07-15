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
        throw new NotImplementedException();
    }
}

