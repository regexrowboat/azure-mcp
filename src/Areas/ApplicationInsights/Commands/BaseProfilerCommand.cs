using AzureMcp.Areas.ApplicationInsights.Options;
using AzureMcp.Commands.Subscription;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.ApplicationInsights.Commands;

public abstract class BaseProfilerCommand(
    ILogger<BaseProfilerCommand> logger) : SubscriptionCommand<ProfilerOptions>
{
    protected ILogger Logger { get; } = logger;
}
