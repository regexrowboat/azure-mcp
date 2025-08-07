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
            var resourceGroup = commandResult.GetValueForOption(ApplicationInsightsOptionDefinitions.ResourceGroup);
            var resourceId = commandResult.GetValueForOption(ApplicationInsightsOptionDefinitions.ResourceId);

            // Enforce that either resourceId is provided OR both resourceName and resourceGroup are provided
            var hasResourceId = !string.IsNullOrWhiteSpace(resourceId);
            var hasResourceNameAndGroup = !string.IsNullOrWhiteSpace(resourceName) && !string.IsNullOrWhiteSpace(resourceGroup);

            if (!hasResourceId && !hasResourceNameAndGroup)
            {
                result.IsValid = false;
                result.ErrorMessage = "You must specify either --resource-id OR both --resource-name and --resource-group for the Application Insights resource.";
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
