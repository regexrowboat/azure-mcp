// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Messaging.ServiceBus;
using AzureMcp.Core.Commands;
using AzureMcp.Core.Commands.Subscription;
using AzureMcp.Core.Services.Telemetry;
using AzureMcp.ServiceBus.Commands;
using AzureMcp.ServiceBus.Models;
using AzureMcp.ServiceBus.Options;
using AzureMcp.ServiceBus.Options.Topic;
using AzureMcp.ServiceBus.Services;

namespace AzureMcp.ServiceBus.Commands.Topic;

public sealed class TopicDetailsCommand : SubscriptionCommand<BaseTopicOptions>
{
    private const string CommandTitle = "Get Service Bus Topic Details";
    private readonly Option<string> _topicOption = ServiceBusOptionDefinitions.Topic;
    private readonly Option<string> _namespaceOption = ServiceBusOptionDefinitions.Namespace;

    public override string Name => "details";

    public override string Description =>
        """
        Get details about a Service Bus topic. Returns topic properties and runtime information. Properties returned include
        number of subscriptions, max message size, max topic size, number of scheduled messages, etc.

        Required arguments:
        - namespace: The fully qualified Service Bus namespace host name. (This is usually in the form <namespace>.servicebus.windows.net)
        - topic-name: Topic name to get information about.
        """;

    public override string Title => CommandTitle;

    public override ToolMetadata Metadata => new() { Destructive = false, ReadOnly = true };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_namespaceOption);
        command.AddOption(_topicOption);
    }



    protected override BaseTopicOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.TopicName = parseResult.GetValueForOption(_topicOption);
        options.Namespace = parseResult.GetValueForOption(_namespaceOption);
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

            var service = context.GetService<IServiceBusService>();
            var details = await service.GetTopicDetails(
                options.Namespace!,
                options.TopicName!,
                options.Tenant,
                options.RetryPolicy);

            context.Response.Results = ResponseResult.Create(
                new TopicDetailsCommandResult(details),
                ServiceBusJsonContext.Default.TopicDetailsCommandResult);
        }
        catch (Exception ex)
        {
            HandleException(context, ex);
        }

        return context.Response;
    }

    protected override string GetErrorMessage(Exception ex) => ex switch
    {
        ServiceBusException exception when exception.Reason == ServiceBusFailureReason.MessagingEntityNotFound =>
            $"Subscription not found. Please check the topic and subscription name and try again.",
        _ => base.GetErrorMessage(ex)
    };

    protected override int GetStatusCode(Exception ex) => ex switch
    {
        ServiceBusException sbEx when sbEx.Reason == ServiceBusFailureReason.MessagingEntityNotFound => 404,
        _ => base.GetStatusCode(ex)
    };

    internal record TopicDetailsCommandResult(TopicDetails TopicDetails);
}
