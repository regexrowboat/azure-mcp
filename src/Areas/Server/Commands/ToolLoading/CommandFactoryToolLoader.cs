// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Reflection;
using AzureMcp.Areas.Server;
using AzureMcp.Areas.Server.Models;
using AzureMcp.Areas.Server.Options;
using AzureMcp.Commands;
using AzureMcp.Services.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using static AzureMcp.Services.Telemetry.TelemetryConstants;

namespace AzureMcp.Areas.Server.Commands.ToolLoading;

/// <summary>
/// A tool loader that creates MCP tools from the registered command factory.
/// Exposes AzureMcp commands as MCP tools that can be invoked through the MCP protocol.
/// </summary>
public sealed class CommandFactoryToolLoader(
    IServiceProvider serviceProvider,
    CommandFactory commandFactory,
    IOptions<ToolLoaderOptions> options,
    ITelemetryService telemetry,
    ILogger<CommandFactoryToolLoader> logger) : IToolLoader
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly CommandFactory _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
    private readonly IOptions<ToolLoaderOptions> _options = options;
    private readonly ITelemetryService _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    private IReadOnlyDictionary<string, IBaseCommand> _toolCommands =
        (options.Value.Namespace == null || options.Value.Namespace.Length == 0)
            ? commandFactory.AllCommands
            : commandFactory.GroupCommands(options.Value.Namespace);
    private readonly ILogger<CommandFactoryToolLoader> _logger = logger;

    /// <summary>
    /// Lists all tools available from the command factory.
    /// </summary>
    /// <param name="request">The request context containing parameters and metadata.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the list of available tools.</returns>
    public ValueTask<ListToolsResult> ListToolsHandler(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        var tools = CommandFactory.GetVisibleCommands(_toolCommands)
            .Select(kvp => GetTool(kvp.Key, kvp.Value))
            .Where(tool => !_options.Value.ReadOnly || (tool.Annotations?.ReadOnlyHint == true))
            .ToList();

        var listToolsResult = new ListToolsResult { Tools = tools };

        _logger.LogInformation("Listing {NumberOfTools} tools.", tools.Count);

        return ValueTask.FromResult(listToolsResult);
    }

    /// <summary>
    /// Handles tool calls by executing the corresponding command from the command factory.
    /// </summary>
    /// <param name="request">The request context containing parameters and metadata.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the tool call operation.</returns>
    public async ValueTask<CallToolResult> CallToolHandler(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.StartActivity(ActivityName.ToolExecuted, request.Server.ClientInfo);

        if (request.Params == null)
        {
            var content = new TextContentBlock
            {
                Text = "Cannot call tools with null parameters.",
            };

            activity?.SetStatus(ActivityStatusCode.Error)?.AddTag(TagName.ErrorDetails, content.Text);

            return new CallToolResult
            {
                Content = [content],
                IsError = true,
            };
        }

        var toolName = request.Params.Name;
        activity?.AddTag(TagName.ToolName, toolName);

        var command = _toolCommands.GetValueOrDefault(toolName);
        if (command == null)
        {
            var content = new TextContentBlock
            {
                Text = $"Could not find command: {request.Params.Name}",
            };

            activity?.SetStatus(ActivityStatusCode.Error)?.AddTag(TagName.ErrorDetails, content.Text);

            return new CallToolResult
            {
                Content = [content],
                IsError = true,
            };
        }
        var commandContext = new CommandContext(_serviceProvider);

        var realCommand = command.GetCommand();
        var commandOptions = realCommand.ParseFromDictionary(request.Params.Arguments);

        _logger.LogTrace("Invoking '{Tool}'.", realCommand.Name);

        try
        {
            var commandResponse = await command.ExecuteAsync(commandContext, commandOptions);
            var jsonResponse = JsonSerializer.Serialize(commandResponse, ModelsJsonContext.Default.CommandResponse);
            var isError = commandResponse.Status < 200 || commandResponse.Status >= 300;

            return new CallToolResult
            {
                Content = [
                    new TextContentBlock {
                        Text = jsonResponse
                    }
                ],
                IsError = isError
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred running '{Tool}'. ", realCommand.Name);
            activity?.SetStatus(ActivityStatusCode.Error)?.AddTag(TagName.ErrorDetails, ex.Message);

            throw;
        }
        finally
        {
            _logger.LogTrace("Finished executing '{Tool}'.", realCommand.Name);
        }
    }

    /// <summary>
    /// Converts a command to an MCP tool definition.
    /// </summary>
    /// <param name="fullName">The full name of the command.</param>
    /// <param name="command">The command to convert.</param>
    /// <returns>An MCP tool definition.</returns>
    private static Tool GetTool(string fullName, IBaseCommand command)
    {
        var underlyingCommand = command.GetCommand();
        var tool = new Tool
        {
            Name = fullName,
            Description = underlyingCommand.Description,
        };

        // Get the ExecuteAsync method info to check for McpServerToolAttribute
        var executeAsyncMethod = command.GetType().GetMethod(nameof(IBaseCommand.ExecuteAsync));
        if (executeAsyncMethod?.GetCustomAttribute<McpServerToolAttribute>() is { } mcpServerToolAttr)
        {
            tool.Annotations = new ToolAnnotations()
            {
                DestructiveHint = mcpServerToolAttr.Destructive,
                IdempotentHint = mcpServerToolAttr.Idempotent,
                OpenWorldHint = mcpServerToolAttr.OpenWorld,
                ReadOnlyHint = mcpServerToolAttr.ReadOnly,
                Title = mcpServerToolAttr.Title,
            };
        }

        var options = command.GetCommand().Options;

        var schema = new ToolInputSchema();

        if (options != null && options.Count > 0)
        {
            foreach (var option in options)
            {
                schema.Properties.Add(option.Name, new ToolPropertySchema
                {
                    Type = option.ValueType.ToJsonType(),
                    Description = option.Description,
                });
            }

            schema.Required = options.Where(p => p.IsRequired).Select(p => p.Name).ToArray();
        }

        tool.InputSchema = JsonSerializer.SerializeToElement(schema, ServerJsonContext.Default.ToolInputSchema);

        return tool;
    }

    /// <summary>
    /// Disposes resources owned by this tool loader.
    /// CommandFactoryToolLoader doesn't own external resources that need disposal.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // CommandFactoryToolLoader doesn't create or manage disposable resources
        await ValueTask.CompletedTask;
    }
}
