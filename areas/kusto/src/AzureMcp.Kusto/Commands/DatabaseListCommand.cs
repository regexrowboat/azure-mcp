// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Core.Commands;
using AzureMcp.Core.Services.Telemetry;
using AzureMcp.Kusto.Commands;
using AzureMcp.Kusto.Options;
using AzureMcp.Kusto.Services;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Kusto.Commands;

public sealed class DatabaseListCommand(ILogger<DatabaseListCommand> logger) : BaseClusterCommand<DatabaseListOptions>()
{
    private const string CommandTitle = "List Kusto Databases";
    private readonly ILogger<DatabaseListCommand> _logger = logger;

    public override string Name => "list";

    public override string Description =>
        """
        List all databases in a Kusto cluster.
        Requires `cluster-uri` ( or `subscription` and `cluster-name`).
        Result is a list of database names, returned as a JSON array.
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

            var kusto = context.GetService<IKustoService>();

            List<string> databasesNames = [];

            if (UseClusterUri(options))
            {
                databasesNames = await kusto.ListDatabases(
                    options.ClusterUri!,
                    options.Tenant,
                    options.AuthMethod,
                    options.RetryPolicy);
            }
            else
            {
                databasesNames = await kusto.ListDatabases(
                    options.Subscription!,
                    options.ClusterName!,
                    options.Tenant,
                    options.AuthMethod,
                    options.RetryPolicy);
            }

            context.Response.Results = databasesNames?.Count > 0 ?
                ResponseResult.Create(new DatabaseListCommandResult(databasesNames), KustoJsonContext.Default.DatabaseListCommandResult) :
                null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred listing databases. Cluster: {Cluster}.", options.ClusterUri ?? options.ClusterName);
            HandleException(context, ex);
        }
        return context.Response;
    }

    public record DatabaseListCommandResult(List<string> Databases);
}
