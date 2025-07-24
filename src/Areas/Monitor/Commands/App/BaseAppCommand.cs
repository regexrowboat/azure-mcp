// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AzureMcp.Commands.Subscription;
using AzureMcp.Options;
using AzureMcp.Areas.Monitor.Options;
using AzureMcp.Commands;
using AzureMcp.Areas.Monitor.Options.App;

namespace AzureMcp.Areas.Monitor.Commands.App;

public abstract class BaseAppCommand<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] TOptions>
    : SubscriptionCommand<TOptions>
    where TOptions : SubscriptionOptions, IAppOptions, new()
{
    protected readonly Option<string> _resourceNameOption = MonitorOptionDefinitions.App.ResourceName;
    protected new readonly Option<string> _resourceGroupOption = MonitorOptionDefinitions.App.ResourceGroup;
    protected readonly Option<string> _resourceIdOption = MonitorOptionDefinitions.App.ResourceId;
    protected readonly Option<string> _intentOption = MonitorOptionDefinitions.App.Intent;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_resourceNameOption);
        command.AddOption(_resourceGroupOption);
        command.AddOption(_resourceIdOption);
        command.AddOption(_intentOption);
    }

    protected override TOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);

        options.ResourceGroup = parseResult.GetValueForOption(_resourceGroupOption);
        options.ResourceName = parseResult.GetValueForOption(_resourceNameOption);
        options.ResourceId = parseResult.GetValueForOption(_resourceIdOption);
        options.Intent = parseResult.GetValueForOption(_intentOption);

        return options;
    }

    public override ValidationResult Validate(CommandResult commandResult, CommandResponse? commandResponse = null)
    {
        var result = base.Validate(commandResult, commandResponse);

        if (result.IsValid)
        {
            var resourceName = commandResult.GetValueForOption(_resourceNameOption);
            var resourceId = commandResult.GetValueForOption(_resourceIdOption);

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
