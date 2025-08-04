using AzureMcp.ApplicationInsights.Options;
using AzureMcp.Core.Commands.Subscription;
using Microsoft.Extensions.Logging;

namespace AzureMcp.ApplicationInsights.Commands;

public abstract class BaseProfilerCommand(
    ILogger<BaseProfilerCommand> logger) : SubscriptionCommand<ProfilerOptions>
{
    protected ILogger Logger { get; } = logger;
}
