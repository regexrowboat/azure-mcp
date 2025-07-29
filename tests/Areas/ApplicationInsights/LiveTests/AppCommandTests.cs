﻿using System.Text.Json;
using AzureMcp.Areas.ApplicationInsights;
using AzureMcp.Areas.ApplicationInsights.Commands;
using AzureMcp.Areas.ApplicationInsights.Services;
using AzureMcp.Commands;
using AzureMcp.Models.Command;
using AzureMcp.Services.Azure.Resource;
using AzureMcp.Services.Azure.Subscription;
using AzureMcp.Services.Azure.Tenant;
using AzureMcp.Services.Caching;
using AzureMcp.Tests.Client;
using AzureMcp.Tests.Client.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using static AzureMcp.Areas.ApplicationInsights.Commands.AppCorrelateTimeCommand;
using static AzureMcp.Areas.ApplicationInsights.Commands.AppGetTraceCommand;
using static AzureMcp.Areas.ApplicationInsights.Commands.AppListTraceCommand;

namespace AzureMcp.Tests.Areas.ApplicationInsights.LiveTests
{
    [Trait("Area", "ApplicationInsights")]
    public class AppCommandTests(LiveTestFixture fixture, ITestOutputHelper output) : CommandTestsBase(fixture, output), IClassFixture<LiveTestFixture>, IAsyncLifetime
    {
        private CommandContext? _commandContext;

        ValueTask IAsyncLifetime.InitializeAsync()
        {
            var sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));
            sc.AddSingleton<ICacheService, CacheService>();
            sc.AddSingleton<ITenantService, TenantService>();
            sc.AddSingleton<ISubscriptionService, SubscriptionService>();
            sc.AddSingleton<IResourceResolverService, ResourceResolverService>();
            new ApplicationInsightsSetup().ConfigureServices(sc);

            var sp = sc.BuildServiceProvider();
            _commandContext = new CommandContext(sp);

            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            base.Dispose();
            return ValueTask.CompletedTask;
        }

        [Fact]
        [Trait("Category", "Live")]
        public async Task Should_produce_time_correlation()
        {
            var result = await CallToolAsync(
            "azmcp_applicationinsights_correlate-time",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-name", Settings.ResourceBaseName },
                { "data-sets",
                    new[]
                    {
                        new {
                            table = "availabilityResults",
                            splitBy = "location"
                        }
                    }
                }
            });

            Assert.NotNull(result);

            AppCorrelateCommandResult? actual = JsonSerializer.Deserialize(result.Value.GetRawText(), ApplicationInsightsJsonContext.Default.AppCorrelateCommandResult);

            Assert.True(actual?.Result?.Count() > 0, "Expected at least one correlation result");
        }

        [Fact]
        [Trait("Category", "Live")]
        public async Task Should_list_traces_and_get_trace()
        {
            var result = await CallToolAsync(
            "azmcp_applicationinsights_list-traces",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-name", Settings.ResourceBaseName },
                { "table", "availabilityResults" },
                { "filters", new[] { "success='false'" } }
            });

            Assert.NotNull(result);

            AppListTraceCommandResult? actual = JsonSerializer.Deserialize(result.Value.GetRawText(), ApplicationInsightsJsonContext.Default.AppListTraceCommandResult);

            Assert.Equal("availabilityResults", actual?.Result?.Table);

            var traces = actual?.Result?.Rows?.SelectMany(t => t.Traces).ToList();

            Assert.True(traces?.Count > 0, "Expected at least one trace in the result");

            var firstTrace = traces?.FirstOrDefault()!;

            // now get the trace details

            result = await CallToolAsync(
            "azmcp_applicationinsights_get-trace",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-name", Settings.ResourceBaseName },
                { "trace-id", firstTrace.TraceId }
            });

            Assert.NotNull(result);

            AppGetTraceCommandResult? traceResult = JsonSerializer.Deserialize(result.Value.GetRawText(), ApplicationInsightsJsonContext.Default.AppGetTraceCommandResult);
            Assert.Equal(firstTrace.TraceId, traceResult?.Result?.TraceId);
            Assert.NotEmpty(traceResult?.Result?.TraceDetails!);
        }

        [Fact]
        [Trait("Category", "Live")]
        public async Task Debuggable_Should_produce_time_correlation()
        {
            var command = new AppCorrelateTimeCommand(Substitute.For<ILogger<AppCorrelateTimeCommand>>());

            var args = command.GetCommand().ParseFromDictionary(new Dictionary<string, JsonElement>
            {
                { "subscription", JsonSerializer.SerializeToElement(Settings.SubscriptionId) },
                { "resource-name", JsonSerializer.SerializeToElement(Settings.ResourceBaseName) },
                { "data-sets",
                    JsonSerializer.SerializeToElement(new[]
                    {
                        new
                        {
                            table = "availabilityResults",
                            splitBy = "location"
                        }
                    })
                }
            });
            var response = await command.ExecuteAsync(_commandContext!, args);

            var result = GetResult(response);

            AppCorrelateCommandResult? actual = JsonSerializer.Deserialize(result.GetRawText(), ApplicationInsightsJsonContext.Default.AppCorrelateCommandResult);

            Assert.True(actual?.Result?.Count() > 0, "Expected at least one correlation result");
        }

        [Fact]
        [Trait("Category", "Live")]
        public async Task Debuggable_Should_list_traces_and_get_trace()
        {
            var listTraceCommand = new AppListTraceCommand(Substitute.For<ILogger<AppListTraceCommand>>());
            var getTraceCommand = new AppGetTraceCommand(Substitute.For<ILogger<AppGetTraceCommand>>());

            var args = listTraceCommand.GetCommand().ParseFromDictionary(new Dictionary<string, JsonElement>
            {
                { "subscription", JsonSerializer.SerializeToElement(Settings.SubscriptionId) },
                { "resource-name", JsonSerializer.SerializeToElement(Settings.ResourceBaseName) },
                { "table", JsonSerializer.SerializeToElement("availabilityResults") },
                { "filters", JsonSerializer.SerializeToElement(new string[] { "success='false'" }) }
            });

            var result = await listTraceCommand.ExecuteAsync(_commandContext!, args);

            Assert.NotNull(result);

            AppListTraceCommandResult? actual = JsonSerializer.Deserialize(GetResult(result), ApplicationInsightsJsonContext.Default.AppListTraceCommandResult);

            Assert.Equal("availabilityResults", actual?.Result?.Table);

            var traces = actual?.Result?.Rows?.SelectMany(t => t.Traces).ToList();

            Assert.True(traces?.Count > 0, "Expected at least one trace in the result");

            var firstTrace = traces?.FirstOrDefault()!;

            // now get the trace details

            args = getTraceCommand.GetCommand().ParseFromDictionary(new Dictionary<string, JsonElement>
            {
                { "subscription", JsonSerializer.SerializeToElement(Settings.SubscriptionId) },
                { "resource-name", JsonSerializer.SerializeToElement(Settings.ResourceBaseName) },
                { "trace-id", JsonSerializer.SerializeToElement(firstTrace.TraceId) }
            });

            result = await getTraceCommand.ExecuteAsync(_commandContext!, args);

            Assert.NotNull(result);

            AppGetTraceCommandResult? traceResult = JsonSerializer.Deserialize(GetResult(result), ApplicationInsightsJsonContext.Default.AppGetTraceCommandResult);
            Assert.Equal(firstTrace.TraceId, traceResult?.Result?.TraceId);
            Assert.NotEmpty(traceResult?.Result?.TraceDetails!);
        }

        private static JsonElement GetResult(CommandResponse response)
        {
            MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);

            response.Results?.Write(writer);

            writer.Flush();
            ms.Position = 0;

            return JsonDocument.Parse(ms).RootElement;
        }
    }
}
