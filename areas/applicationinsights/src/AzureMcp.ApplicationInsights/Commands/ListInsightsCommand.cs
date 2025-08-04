using Azure.Core;
using AzureMcp.ApplicationInsights.Options;
using AzureMcp.ApplicationInsights.Services;
using AzureMcp.Core.Commands;
using AzureMcp.Core.Models;
using AzureMcp.Core.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.ApplicationInsights.Commands;

public sealed class ListInsightsCommand(
    ILogger<ListInsightsCommand> logger) : BaseProfilerCommand(logger)
{
    private const string CommandTitle = "List Code Optimization Insights";

    public override string Name => "list-insights";

    public override string Description => "List code optimization insights for an application identified by the app id of the application insights resource.";

    public override string Title => CommandTitle;

    public override ToolMetadata Metadata => new() { Destructive = false, ReadOnly = true };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);

        command.AddOption(ApplicationInsightsOptionDefinitions.ResourceGroup);
        command.AddOption(ApplicationInsightsOptionDefinitions.ResourceName);
        command.AddOption(ApplicationInsightsOptionDefinitions.ResourceId);

        command.AddOption(ProfilerOptionDefinitions.MaxInsights);
        command.AddOption(ProfilerOptionDefinitions.StartDateTimeUtc);
        command.AddOption(ProfilerOptionDefinitions.EndDateTimeUtc);
    }

    protected override ProfilerOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);

        options.ResourceId = parseResult.GetValueForOption(ApplicationInsightsOptionDefinitions.ResourceId);
        options.ResourceGroup = parseResult.GetValueForOption(ApplicationInsightsOptionDefinitions.ResourceGroup);
        options.ResourceName = parseResult.GetValueForOption(ApplicationInsightsOptionDefinitions.ResourceName);

        options.MaxInsights = parseResult.GetValueForOption(ProfilerOptionDefinitions.MaxInsights);
        options.StartDateTimeUtc = parseResult.GetValueForOption(ProfilerOptionDefinitions.StartDateTimeUtc);
        options.EndDateTimeUtc = parseResult.GetValueForOption(ProfilerOptionDefinitions.EndDateTimeUtc);

        if (options.EndDateTimeUtc == default)
        {
            options.EndDateTimeUtc = DateTime.UtcNow;
        }

        if (options.StartDateTimeUtc == default)
        {
            options.StartDateTimeUtc = options.EndDateTimeUtc.AddDays(-1);
        }

        return options;
    }

    public override ValidationResult Validate(CommandResult commandResult, CommandResponse? commandResponse = null)
    {
        var result = base.Validate(commandResult, commandResponse);

        if (result.IsValid)
        {
            var resourceName = commandResult.GetValueForOption(ApplicationInsightsOptionDefinitions.ResourceName);
            var resourceId = commandResult.GetValueForOption(ApplicationInsightsOptionDefinitions.ResourceId);

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

    [McpServerTool(Destructive = false, ReadOnly = true, Title = CommandTitle)]
    public async override Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        Logger.LogInformation("Executing {CommandName} command.", Name);

        ProfilerOptions options = BindOptions(parseResult);

        Logger.LogInformation("Options: {resourceId}, {startDateTimeUtc}, {endDateTimeUtc}, {maxInsights}, {resourceGroup}, {resourceName}",
            options.ResourceId,
            options.StartDateTimeUtc,
            options.EndDateTimeUtc,
            options.MaxInsights,
            options.ResourceGroup,
            options.ResourceName);

        try
        {
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            context.Activity?.WithSubscriptionTag(options);

            string resourceIdString = options.ResourceId ?? throw new InvalidOperationException("Application Insights Component resource id is required.");
            ResourceIdentifier resolvedResourceId = ResourceIdentifier.Parse(resourceIdString);

            IProfilerInsightsService profilerInsightsService = context.GetService<IProfilerInsightsService>();
            var insights = await profilerInsightsService.GetInsightsAsync(
                resolvedResourceId,
                options.StartDateTimeUtc,
                options.EndDateTimeUtc,
                cancellationToken: default).ConfigureAwait(false);

            context.Response.Results = insights?.Count > 0 ?
                ResponseResult.Create(
                    insights,
                    ProfilerJsonContext.Default.ListJsonNode) :
                null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting Profiler Insights.");
            HandleException(context, ex);
        }

        return context.Response;
    }
}
