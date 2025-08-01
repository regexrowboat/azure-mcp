// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Core.Commands;
using AzureMcp.Core.Services.Telemetry;
using AzureMcp.Sql.Commands;
using AzureMcp.Sql.Models;
using AzureMcp.Sql.Options.Database;
using AzureMcp.Sql.Services;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Sql.Commands.Database;

public sealed class DatabaseListCommand(ILogger<DatabaseListCommand> logger)
    : BaseSqlCommand<DatabaseListOptions>(logger)
{
    private const string CommandTitle = "List SQL Databases";

    public override string Name => "list";

    public override string Description =>
        """
        Lists all databases in an Azure SQL Server with their configuration, status, SKU, and performance details.
        Use when you need to: view database inventory, check database status across a server, compare database configurations,
        or find databases for management operations.
        Requires: subscription ID, resource group name, server name.
        Returns: JSON array of databases with complete configuration details including SKU, status, and size information.
        Equivalent to 'az sql db list'.
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

            var databases = await sqlService.ListDatabasesAsync(
                options.Server!,
                options.ResourceGroup!,
                options.Subscription!,
                options.RetryPolicy);

            context.Response.Results = ResponseResult.Create(
                new DatabaseListResult(databases),
                SqlJsonContext.Default.DatabaseListResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error listing SQL databases. Server: {Server}, ResourceGroup: {ResourceGroup}, Options: {@Options}",
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

    internal record DatabaseListResult(List<SqlDatabase> Databases);
}
