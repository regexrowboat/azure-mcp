// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Aks.Commands;
using AzureMcp.Aks.Models;
using AzureMcp.Aks.Options;
using AzureMcp.Aks.Options.Cluster;
using AzureMcp.Aks.Services;
using AzureMcp.Core.Commands;
using AzureMcp.Core.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Aks.Commands.Cluster;

public sealed class ClusterGetCommand(ILogger<ClusterGetCommand> logger) : BaseAksCommand<ClusterGetOptions>()
{
    private const string CommandTitle = "Get AKS Cluster Details";
    private readonly ILogger<ClusterGetCommand> _logger = logger;

    // Define options from OptionDefinitions
    private readonly Option<string> _clusterNameOption = AksOptionDefinitions.Cluster;

    public override string Name => "get";

    public override string Description =>
        """
        Get details for a specific Azure Kubernetes Service (AKS) cluster.
        Returns detailed cluster information including configuration, network settings, and status.
        """;

    public override string Title => CommandTitle;

    public override ToolMetadata Metadata => new() { Destructive = false, ReadOnly = true };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_resourceGroupOption);
        command.AddOption(_clusterNameOption);
    }

    protected override ClusterGetOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.ResourceGroup = parseResult.GetValueForOption(_resourceGroupOption);
        options.ClusterName = parseResult.GetValueForOption(_clusterNameOption);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);

        try
        {
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            context.Activity?.WithSubscriptionTag(options);

            var aksService = context.GetService<IAksService>();
            var cluster = await aksService.GetCluster(
                options.Subscription!,
                options.ClusterName!,
                options.ResourceGroup!,
                options.Tenant,
                options.RetryPolicy);

            context.Response.Results = cluster is null ?
                null : ResponseResult.Create(
                    new ClusterGetCommandResult(cluster),
                    AksJsonContext.Default.ClusterGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting AKS cluster. Subscription: {Subscription}, ResourceGroup: {ResourceGroup}, ClusterName: {ClusterName}, Options: {@Options}",
                options.Subscription, options.ResourceGroup, options.ClusterName, options);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        Azure.RequestFailedException reqEx when reqEx.Status == 404 =>
            "AKS cluster not found. Verify the cluster name, resource group, and subscription, and ensure you have access.",
        Azure.RequestFailedException reqEx when reqEx.Status == 403 =>
            $"Authorization failed accessing the AKS cluster. Details: {reqEx.Message}",
        Azure.RequestFailedException reqEx => reqEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    protected override int GetStatusCode(Exception ex) => ex switch
    {
        Azure.RequestFailedException reqEx => reqEx.Status,
        _ => base.GetStatusCode(ex)
    };

    internal record ClusterGetCommandResult(AzureMcp.Aks.Models.Cluster Cluster);
}
