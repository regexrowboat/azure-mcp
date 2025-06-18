// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AzureMcp.Areas.Monitor.Options.Metrics;
using AzureMcp.Commands;
using AzureMcp.Commands.Subscription;
using AzureMcp.Models.Option;
using AzureMcp.Options;

namespace AzureMcp.Areas.Monitor.Commands.Metrics;

/// <summary>
/// Base command for all metrics operations
/// </summary>
public abstract class BaseMetricsCommand<
    [DynamicallyAccessedMembers(TrimAnnotations.CommandAnnotations)] TOptions>
    : SubscriptionCommand<TOptions>
    where TOptions : SubscriptionOptions, IMetricsOptions, new()
{
    protected readonly Option<string> _resourceTypeOption = OptionDefinitions.Common.ResourceType;
    protected readonly Option<string> _resourceNameOption = OptionDefinitions.Common.ResourceName;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_resourceTypeOption);
        command.AddOption(_resourceNameOption);
        command.AddOption(_resourceGroupOption);
    }

    protected override TOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceType = parseResult.GetValueForOption(_resourceTypeOption);
        options.ResourceName = parseResult.GetValueForOption(_resourceNameOption);
        options.ResourceGroup = parseResult.GetValueForOption(_resourceGroupOption);
        return options;
    }
}
