// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using AzureMcp.ApplicationInsights.Commands;
using AzureMcp.ApplicationInsights.Services;
using AzureMcp.Core.Commands;
using AzureMcp.Core.Models.Command;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzureMcp.ApplicationInsights.UnitTests.Commands;

[Trait("Area", "ApplicationInsights")]
public class ListInsightsCommandTests
{
    private readonly CommandContext _context;
    private readonly IProfilerInsightsService _profilerInsightsService = Substitute.For<IProfilerInsightsService>();
    private readonly ILogger<ListInsightsCommand> _logger = Substitute.For<ILogger<ListInsightsCommand>>();

    private readonly ListInsightsCommand _sut;

    public ListInsightsCommandTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IProfilerInsightsService>(_profilerInsightsService);
        var sp = serviceCollection.BuildServiceProvider();

        _context = new CommandContext(sp);
        _sut = new ListInsightsCommand(_logger);
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Arrange & Act
        var command = new ListInsightsCommand(_logger);

        // Assert
        Assert.NotNull(command);
        Assert.Equal("list-insights", command.Name);
        Assert.Contains("code optimization insights", command.Description.ToLower());
    }

    [Fact]
    public async Task Validates_NoOptionsPassed_ReturnsInvalid()
    {
        var args = _sut.GetCommand().ParseFromDictionary(new Dictionary<string, JsonElement>());

        var response = await _sut.ExecuteAsync(_context, args);

        Assert.Equal(400, response.Status);
        Assert.Contains("subscription", response.Message.ToLower());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidOptions_CallsService()
    {
        // Arrange
        var insights = new List<JsonNode>
        {
            JsonNode.Parse("{\"insight\": \"test\"}")!
        };

        _profilerInsightsService.GetInsightsAsync(
            Arg.Any<ResourceIdentifier>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>())
            .Returns(insights);

        var options = new Dictionary<string, JsonElement>
        {
            { "subscription", JsonSerializer.SerializeToElement("test-sub") },
            { "resource-id", JsonSerializer.SerializeToElement("/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Insights/components/test") }
        };
        var parseResult = _sut.GetCommand().ParseFromDictionary(options);

        // Act
        var response = await _sut.ExecuteAsync(_context, parseResult);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.NotNull(response.Results);
        await _profilerInsightsService.Received(1).GetInsightsAsync(
            Arg.Any<ResourceIdentifier>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }
}
