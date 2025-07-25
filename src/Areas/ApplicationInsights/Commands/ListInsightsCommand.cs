using AzureMcp.Areas.ApplicationInsights.Options;
using AzureMcp.Areas.ApplicationInsights.Services;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.ApplicationInsights.Commands;

public class ListInsightsCommand(ILogger<ListInsightsCommand> logger) : BaseProfilerCommand(logger)
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

    protected override ProfilerOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.AppId = parseResult.GetValueForOption(_appIdOption);
        return options;
    }

    [McpServerTool(Destructive = false, ReadOnly = true, Title = CommandTitle)]
    public async override Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        ProfilerOptions options = BindOptions(parseResult);

        try
        {
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            context.Activity?.WithSubscriptionTag(options);

            IProfilerDataplaneService dataplane = context.GetService<IProfilerDataplaneService>();
            var insights = await dataplane.GetInsightsAsync(
                [options.AppId],
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
