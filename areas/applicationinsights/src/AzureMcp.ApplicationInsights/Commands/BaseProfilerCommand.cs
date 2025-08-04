using AzureMcp.ApplicationInsights.Options;
using AzureMcp.Core.Commands;
using AzureMcp.Core.Commands.Subscription;
using Microsoft.Extensions.Logging;

namespace AzureMcp.ApplicationInsights.Commands;

public abstract class BaseProfilerCommand(
    ILogger<BaseProfilerCommand> logger) : SubscriptionCommand<ProfilerOptions>
{
    protected ILogger Logger { get; } = logger;

    public override ValidationResult Validate(CommandResult commandResult, CommandResponse? commandResponse = null)
    {
        var result = base.Validate(commandResult, commandResponse);

        if (result.IsValid)
        {
            var resourceName = commandResult.GetValueForOption(ApplicationInsightsOptionDefinitions.ResourceName);
            var resourceId = commandResult.GetValueForOption(ApplicationInsightsOptionDefinitions.ResourceId);

            // Enforce that at least one of resourceName or resourceId is provided
            if (string.IsNullOrWhiteSpace(resourceName) && string.IsNullOrWhiteSpace(resourceId))
            {
                result.IsValid = false;
                result.ErrorMessage = "You must specify at least one of --resource-name or --resource-id for the Application Insights resource.";
                if (commandResponse != null)
                {
                    commandResponse.Status = 400;
                    commandResponse.Message = result.ErrorMessage;
                }
            }
        }

        return result;
    }
}
