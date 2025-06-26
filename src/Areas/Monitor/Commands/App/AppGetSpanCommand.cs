
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using AzureMcp.Areas.Monitor.Models;
using AzureMcp.Areas.Monitor.Options;
using AzureMcp.Areas.Monitor.Options.App;
using AzureMcp.Areas.Monitor.Services;
using AzureMcp.Commands;
using AzureMcp.Commands.Monitor;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.Monitor.Commands.App
{
    public sealed class AppGetSpanCommand(ILogger<AppGetSpanCommand> logger)
        : BaseAppCommand<AppGetSpanOptions>
    {
        private readonly ILogger<AppGetSpanCommand> _logger = logger;
        private const string CommandTitle = "App Diagnose Get Span";

        // Define options from OptionDefinitions
        private readonly Option<string> _itemIdOption = MonitorOptionDefinitions.App.ItemId;
        private readonly Option<string> _startTimeOption = MonitorOptionDefinitions.App.StartTime;
        private readonly Option<string> _endTimeOption = MonitorOptionDefinitions.App.EndTime;
        private readonly Option<string> _itemTypeOption = MonitorOptionDefinitions.App.ItemType;

        public override string Name => "get-span";
        public override string Description =>
            $$"""
            Retrieve a single span with full details from a distributed trace based on the ItemId.

            This tool is useful for getting exception stack traces, details about dependency calls and other specific information
            from a distributed trace.
            
            Use this tool for investigating issues with Application Insights resources.
            Required options:
            - {{_resourceNameOption.Name}}: {{_resourceNameOption.Description}} or {{_resourceIdOption.Name}}: {{_resourceIdOption.Description}}
            - {{_itemIdOption.Name}}: {{_itemIdOption.Description}}
            - {{_itemTypeOption.Name}}: {{_itemTypeOption.Description}}
            Optional options:
            - {{_resourceGroupOption.Name}}: {{_resourceGroupOption.Description}}
            - {{_startTimeOption.Name}}: {{_startTimeOption.Description}}
            - {{_endTimeOption.Name}}: {{_endTimeOption.Description}}
            """;
        
        public override string Title => CommandTitle;
        protected override void RegisterOptions(Command command)
        {
            command.AddOption(_startTimeOption);
            command.AddOption(_endTimeOption);
            command.AddOption(_itemIdOption);
            command.AddOption(_itemTypeOption);
            base.RegisterOptions(command);
        }
        protected override AppGetSpanOptions BindOptions(ParseResult parseResult)
        {
            var options = base.BindOptions(parseResult);
            options.StartTime = DateTimeOffset.Parse(parseResult.GetValueForOption(_startTimeOption)!).UtcDateTime;
            options.EndTime = DateTimeOffset.Parse(parseResult.GetValueForOption(_endTimeOption)!).UtcDateTime;
            options.ItemId = parseResult.GetValueForOption(_itemIdOption);
            options.ItemType = parseResult.GetValueForOption(_itemTypeOption);
            return options;
        }

        public override ValidationResult Validate(CommandResult commandResult, CommandResponse? commandResponse = null)
        {
            var result = base.Validate(commandResult, commandResponse);

            if (result.IsValid)
            {
                if (!DateTime.TryParse(commandResult.GetValueForOption(_startTimeOption), out DateTime startTime) ||
                    !DateTime.TryParse(commandResult.GetValueForOption(_endTimeOption), out DateTime endTime) ||
                    startTime >= endTime)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Invalid time range specified. Ensure that --{_startTimeOption.Name} is before --{_endTimeOption.Name} and that --{_startTimeOption.Name} and --{_endTimeOption.Name} are valid dates in ISO format.";
                    if (commandResponse != null)
                    {
                        commandResponse.Status = 400;
                        commandResponse.Message = result.ErrorMessage;
                    }
                }
            }

            return result;
        }

        [McpServerTool(
            Destructive = false,
            ReadOnly = true,
            Title = CommandTitle)]
        public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
        {
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            var options = BindOptions(parseResult);

            try
            {
                var service = context.GetService<IAppDiagnoseService>();

                var result = await service.GetSpan(
                    options.Subscription!,
                    options.ResourceGroup,
                    options.ResourceName,
                    options.ResourceId,
                    options.ItemId!,
                    options.ItemType!,
                    options.StartTime,
                    options.EndTime,
                    options.Tenant,
                    options.RetryPolicy);

                var results = result != null ? new AppGetSpanCommandResult(result, null) : null;

                string? summary = null;
                if (results != null)
                {
                    summary = await service.SummarizeWithSampling(context.Server, options.Intent!, results, MonitorJsonContext.Default.AppGetSpanCommandResult, CancellationToken.None);
                }

                context.Response.Results = results != null ?
                    ResponseResult.Create(
                        summary != null ?
                            new AppGetSpanCommandResult(null, summary) : results,
                        MonitorJsonContext.Default.AppGetSpanCommandResult) : null;
            }
            catch (Exception ex)
            {
                // Log error with all relevant context
                _logger.LogError(ex,
                    "Error in {Name}. Subscription: {Subscription}, ResourceGroup: {ResourceGroup}, ResourceName: {ResourceName}, Options: {@Options}",
                    Name, options.Subscription, options.ResourceGroup, options.ResourceName, options);
                HandleException(context.Response, ex);
            }

            return context.Response;
        }

        public record AppGetSpanCommandResult(
            [property: JsonPropertyName("result"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            SpanDetails[]? Result,
            [property: JsonPropertyName("summary"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            string? Summary);
    }
}
