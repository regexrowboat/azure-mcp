using AzureMcp.Areas.AppInsightsProfiler.Options;
using AzureMcp.Areas.AppInsightsProfiler.Services;
using AzureMcp.Commands.AppInsightsProfiler;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.AppInsightsProfiler.Commands;

public class ListInsightsCommand : AppInsightsProfilerBaseCommand
{
    private const string CommandTitle = "List Code Optimization Insights";

    public ListInsightsCommand(ILogger<ListInsightsCommand> logger)
        : base(logger)
    {
    }

    public override string Name => "list-insights";

    public override string Description => "List code optimization insights for an application identified by the app id of the application insights resource.";

    public override string Title => CommandTitle;

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
            var insights = await dataplane.GetInsightsAsync(options.AppIds, options.StartDateTimeUtc, options.EndDateTimeUtc, cancellationToken: default).ConfigureAwait(false);

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
