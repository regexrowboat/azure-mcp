// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Core.Commands;
using AzureMcp.Core.Services.Telemetry;
using AzureMcp.Sql.Models;
using AzureMcp.Sql.Options.ElasticPool;
using AzureMcp.Sql.Services;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Sql.Commands.ElasticPool;

public sealed class ElasticPoolListCommand(ILogger<ElasticPoolListCommand> logger)
    : BaseElasticPoolCommand<ElasticPoolListOptions>(logger)
{
    private const string CommandTitle = "List SQL Elastic Pools";

    public override string Name => "list";

    public override string Description =>
        """
        Lists all SQL elastic pools in an Azure SQL Server with their SKU, capacity, state, and database limits.
        Use when you need to: view elastic pool inventory, check pool utilization, compare pool configurations, 
        or find available pools for database placement.
        Requires: subscription ID, resource group name, server name.
        Returns: JSON array of elastic pools with complete configuration details.
        Equivalent to 'az sql elastic-pool list'.
        """;

    public override string Title => CommandTitle;

    public override ToolMetadata Metadata => new() { Destructive = false, ReadOnly = true };

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

            var sqlService = context.GetService<ISqlService>();

            var elasticPools = await sqlService.GetElasticPoolsAsync(
                options.Server!,
                options.ResourceGroup!,
                options.Subscription!,
                options.RetryPolicy);

            context.Response.Results = ResponseResult.Create(
                new ElasticPoolListResult(elasticPools),
                SqlJsonContext.Default.ElasticPoolListResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error listing SQL elastic pools. Server: {Server}, ResourceGroup: {ResourceGroup}, Options: {@Options}",
                options.Server, options.ResourceGroup, options);
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        Azure.RequestFailedException reqEx when reqEx.Status == 404 =>
            "SQL server not found. Verify the server name, resource group, and that you have access.",
        Azure.RequestFailedException reqEx when reqEx.Status == 403 =>
            $"Authorization failed accessing the SQL server. Verify you have appropriate permissions. Details: {reqEx.Message}",
        Azure.RequestFailedException reqEx => reqEx.Message,
        _ => base.GetErrorMessage(ex)
    };

    protected override int GetStatusCode(Exception ex) => ex switch
    {
        Azure.RequestFailedException reqEx => reqEx.Status,
        _ => base.GetStatusCode(ex)
    };

    internal record ElasticPoolListResult(List<SqlElasticPool> ElasticPools);
}
