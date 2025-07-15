using AzureMcp.Areas.AppInsightsProfiler.Options;
using AzureMcp.Areas.AppInsightsProfiler.Services;
using AzureMcp.Commands.AppInsightsProfiler;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.AppInsightsProfiler.Commands;

public class ListInsightsCommand(ILogger<ListInsightsCommand> logger) : AppInsightsProfilerBaseCommand(logger)
{
    private const string CommandTitle = "List Code Optimization Insights";
    private readonly Option<Guid> _appIdOption = new(
        "--app-id",
        "The application insights resource ID to query for code optimization insights.")
    {
        IsRequired = true
    };

    public override string Name => "list-insights";

    public override string Description => "List code optimization insights for an application identified by the app id of the application insights resource.";

    public override string Title => CommandTitle;

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_appIdOption);
    }

    protected override AppInsightsProfilerOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.AppId = parseResult.GetValueForOption(_appIdOption);
        return options;
    }

    [McpServerTool(Destructive = false, ReadOnly = true, Title = CommandTitle)]
    public async override Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        AppInsightsProfilerOptions options = BindOptions(parseResult);

        try
        {
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            context.Activity?.WithSubscriptionTag(options);

            IAppInsightsProfilerDataplaneService dataplane = context.GetService<IAppInsightsProfilerDataplaneService>();
            var insights = await dataplane.GetInsightsAsync(
                [options.AppId],
                options.StartDateTimeUtc,
                options.EndDateTimeUtc,
                cancellationToken: default).ConfigureAwait(false);

            context.Response.Results = insights?.Count > 0 ?
                ResponseResult.Create(
                    insights,
                    AppInsightsProfilerJsonContext.Default.ListJsonNode) :
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
