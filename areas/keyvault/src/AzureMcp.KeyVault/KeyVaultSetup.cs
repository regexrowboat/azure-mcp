// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Core.Areas;
using AzureMcp.Core.Commands;
using AzureMcp.KeyVault.Commands.Certificate;
using AzureMcp.KeyVault.Commands.Key;
using AzureMcp.KeyVault.Commands.Secret;
using AzureMcp.KeyVault.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureMcp.KeyVault;

public class KeyVaultSetup : IAreaSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IKeyVaultService, KeyVaultService>();
    }

    public void RegisterCommands(CommandGroup rootGroup, ILoggerFactory loggerFactory)
    {
        var keyVault = new CommandGroup("keyvault", "Key Vault operations - Commands for managing and accessing Azure Key Vault resources.");
        rootGroup.AddSubGroup(keyVault);

        var keys = new CommandGroup("key", "Key Vault key operations - Commands for managing and accessing keys in Azure Key Vault.");
        keyVault.AddSubGroup(keys);

        var secret = new CommandGroup("secret", "Key Vault secret operations - Commands for managing and accessing secrets in Azure Key Vault.");
        keyVault.AddSubGroup(secret);

        var certificate = new CommandGroup("certificate", "Key Vault certificate operations - Commands for managing and accessing certificates in Azure Key Vault.");
        keyVault.AddSubGroup(certificate);

        keys.AddCommand("list", new KeyListCommand(loggerFactory.CreateLogger<KeyListCommand>()));
        //keys.AddCommand("get", new KeyGetCommand(loggerFactory.CreateLogger<KeyGetCommand>()));
        keys.AddCommand("create", new KeyCreateCommand(loggerFactory.CreateLogger<KeyCreateCommand>()));

        secret.AddCommand("list", new SecretListCommand(loggerFactory.CreateLogger<SecretListCommand>()));
        secret.AddCommand("create", new SecretCreateCommand(loggerFactory.CreateLogger<SecretCreateCommand>()));
        //secret.AddCommand("get", new SecretGetCommand(loggerFactory.CreateLogger<SecretGetCommand>()));

        certificate.AddCommand("list", new CertificateListCommand(loggerFactory.CreateLogger<CertificateListCommand>()));
        certificate.AddCommand("get", new CertificateGetCommand(loggerFactory.CreateLogger<CertificateGetCommand>()));
        certificate.AddCommand("create", new CertificateCreateCommand(loggerFactory.CreateLogger<CertificateCreateCommand>()));
    }
}
