// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Core.Commands;
using AzureMcp.Core.Commands.Subscription;
using AzureMcp.Core.Services.Telemetry;
using AzureMcp.KeyVault.Commands;
using AzureMcp.KeyVault.Options;
using AzureMcp.KeyVault.Options.Key;
using AzureMcp.KeyVault.Services;
using Microsoft.Extensions.Logging;

namespace AzureMcp.KeyVault.Commands.Key;

public sealed class KeyGetCommand(ILogger<KeyGetCommand> logger) : SubscriptionCommand<KeyGetOptions>
{
    private const string CommandTitle = "Get Key Vault Key";
    private readonly ILogger<KeyGetCommand> _logger = logger;
    private readonly Option<string> _vaultOption = KeyVaultOptionDefinitions.VaultName;
    private readonly Option<string> _keyOption = KeyVaultOptionDefinitions.KeyName;

    public override string Name => "get";

    public override string Title => CommandTitle;

    public override ToolMetadata Metadata => new() { Destructive = false, ReadOnly = true };

    public override string Description =>
        """
        Get a key from an Azure Key Vault. This command retrieves and displays details
        about a specific key in the specified vault.
        """;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_vaultOption);
        command.AddOption(_keyOption);
    }

    protected override KeyGetOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.VaultName = parseResult.GetValueForOption(_vaultOption);
        options.KeyName = parseResult.GetValueForOption(_keyOption);
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

            var keyVaultService = context.GetService<IKeyVaultService>();
            var key = await keyVaultService.GetKey(
                options.VaultName!,
                options.KeyName!,
                options.Subscription!,
                options.Tenant,
                options.RetryPolicy);

            context.Response.Results = ResponseResult.Create(
                new KeyGetCommandResult(
                    key.Name,
                    key.KeyType.ToString(),
                    key.Properties.Enabled,
                    key.Properties.NotBefore,
                    key.Properties.ExpiresOn,
                    key.Properties.CreatedOn,
                    key.Properties.UpdatedOn),
                KeyVaultJsonContext.Default.KeyGetCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key {KeyName} from vault {VaultName}", options.KeyName, options.VaultName);
            HandleException(context, ex);
        }

        return context.Response;
    }

    internal record KeyGetCommandResult(string Name, string KeyType, bool? Enabled, DateTimeOffset? NotBefore, DateTimeOffset? ExpiresOn, DateTimeOffset? CreatedOn, DateTimeOffset? UpdatedOn);
}
