using AzureMcp.Areas.AppInsightsProfiler.Options;
using AzureMcp.Commands.Subscription;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.AppInsightsProfiler;

public abstract class AppInsightsProfilerBaseCommand(
    ILogger<AppInsightsProfilerBaseCommand> logger) : SubscriptionCommand<AppInsightsProfilerOptions>
{
    protected ILogger Logger { get; } = logger;
}
