// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using AzureMcp.Areas.AzureBestPractices.Options;
using AzureMcp.Commands;
using AzureMcp.Helpers;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.AzureBestPractices.Commands;

public sealed class BestPracticesCommand(ILogger<BestPracticesCommand> logger) : BaseCommand
{
    private const string CommandTitle = "Get Azure Best Practices";
    private readonly ILogger<BestPracticesCommand> _logger = logger;
    private static readonly Dictionary<string, string> s_bestPracticesCache = new();

    private readonly Option<string> _resourceOption = BestPracticesOptionDefinitions.Resource;
    private readonly Option<string> _actionOption = BestPracticesOptionDefinitions.Action;

    public override string Name => "get";

    public override string Description =>
        "Returns best practices for secure, production-grade Azure usage. " +
        "Call this command and follow its guidance before generating or suggesting any Azure code. " +
        "This command returns the content of the best practices file as a string array.";

    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        command.AddOption(_resourceOption);
        command.AddOption(_actionOption);
    }

    [McpServerTool(Destructive = false, ReadOnly = true, Title = CommandTitle)]
    public override Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        try
        {
            var validationResult = Validate(parseResult.CommandResult, context.Response);
            if (!validationResult.IsValid)
            {
                return Task.FromResult(context.Response);
            }

            var resource = parseResult.GetValueForOption(_resourceOption);
            var action = parseResult.GetValueForOption(_actionOption);

            var resourceFileName = GetResourceFileName(resource!, action!);
            var bestPractices = GetBestPracticesText(resourceFileName);

            context.Response.Status = 200;
            context.Response.Results = ResponseResult.Create(new List<string> { bestPractices }, JsonSourceGenerationContext.Default.ListString);
            context.Response.Message = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting best practices for Resource: {Resource}, Action: {Action}",
                parseResult.GetValueForOption(_resourceOption), parseResult.GetValueForOption(_actionOption));
            HandleException(context, ex);
        }

        return Task.FromResult(context.Response);
    }

    public override ValidationResult Validate(CommandResult commandResult, CommandResponse? commandResponse = null)
    {
        var validationResult = new ValidationResult { IsValid = true };

        var resource = commandResult.GetValueForOption(BestPracticesOptionDefinitions.Resource);
        var action = commandResult.GetValueForOption(BestPracticesOptionDefinitions.Action);

        if (string.IsNullOrEmpty(resource) || string.IsNullOrEmpty(action))
        {
            validationResult.IsValid = false;
            validationResult.ErrorMessage = "Both resource and action parameters are required.";
        }
        else if (resource != "general" && resource != "azurefunctions")
        {
            validationResult.IsValid = false;
            validationResult.ErrorMessage = "Invalid resource. Must be 'general' or 'azurefunctions'.";
        }
        else if (action != "all" && action != "code-generation" && action != "deployment")
        {
            validationResult.IsValid = false;
            validationResult.ErrorMessage = "Invalid action. Must be 'all', 'code-generation' or 'deployment'.";
        }
        else if (resource == "general" && (action == "deployment" || action == "code-generation"))
        {
            validationResult.IsValid = false;
            validationResult.ErrorMessage = "The 'general' resource only supports 'all' action.";
        }

        if (!validationResult.IsValid && commandResponse != null)
        {
            commandResponse.Status = 400;
            commandResponse.Message = validationResult.ErrorMessage!;
        }

        return validationResult;
    }

    private static string GetResourceFileName(string resource, string action)
    {
        return (resource, action) switch
        {
            ("general", "all") => "azure-best-practices.txt",
            ("azurefunctions", "code-generation") => "azure-functions-codegen-best-practices.txt",
            ("azurefunctions", "deployment") => "azure-functions-deployment-best-practices.txt",
            _ => throw new ArgumentException($"Invalid combination of resource '{resource}' and action '{action}'")
        };
    }

    private string GetBestPracticesText(string resourceFileName)
    {
        if (!s_bestPracticesCache.TryGetValue(resourceFileName, out string? bestPractices))
        {
            bestPractices = LoadBestPracticesText(resourceFileName);
            s_bestPracticesCache[resourceFileName] = bestPractices;
        }
        return bestPractices;
    }

    private string LoadBestPracticesText(string resourceFileName)
    {
        Assembly assembly = typeof(BestPracticesCommand).Assembly;
        string resourceName = EmbeddedResourceHelper.FindEmbeddedResource(assembly, resourceFileName);
        return EmbeddedResourceHelper.ReadEmbeddedResource(assembly, resourceName);
    }
}
