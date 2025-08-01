// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.AppConfig.Commands;
using AzureMcp.AppConfig.Models;
using AzureMcp.AppConfig.Options.KeyValue;
using AzureMcp.AppConfig.Services;
using AzureMcp.Core.Commands;
using AzureMcp.Core.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.AppConfig.Commands.KeyValue;

public sealed class KeyValueShowCommand(ILogger<KeyValueShowCommand> logger) : BaseKeyValueCommand<KeyValueShowOptions>()
{
    private const string CommandTitle = "Show App Configuration Key-Value Setting";
    private readonly ILogger<KeyValueShowCommand> _logger = logger;

    public override string Name => "show";

    public override string Description =>
        """
        Show a specific key-value setting in an App Configuration store. This command retrieves and displays the value,
        label, content type, ETag, last modified time, and lock status for a specific setting. You must specify an
        account name and key. Optionally, you can specify a label otherwise the setting with default label will be retrieved.
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

            var appConfigService = context.GetService<IAppConfigService>();
            var setting = await appConfigService.GetKeyValue(
                options.Account!,
                options.Key!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy,
                options.Label);

            context.Response.Results = ResponseResult.Create(
                new KeyValueShowResult(setting),
                AppConfigJsonContext.Default.KeyValueShowResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred getting value. Key: {Key}.", options.Key);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record KeyValueShowResult(KeyValueSetting Setting);
}
