using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureMcp.Areas.Monitor.Commands.App;
using AzureMcp.Areas.Monitor.Services;
using AzureMcp.Models.Command;
using AzureMcp.Services.Azure.Subscription;
using AzureMcp.Services.Azure.Tenant;
using AzureMcp.Services.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AzureMcp.Tests.Areas.Monitor.LiveTests
{
    public class AppDiagnoseServiceTests
    {

        [Fact]
        public async Task ActualCommand0()
        {
            var sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));
            sc.AddSingleton<ICacheService, CacheService>();
            sc.AddSingleton<ITenantService, TenantService>();
            sc.AddSingleton<ISubscriptionService, SubscriptionService>();
            sc.AddSingleton<IResourceResolverService, ResourceResolverService>();
            sc.AddSingleton<IAppDiagnoseService, AppDiagnoseService>();

            var sp = sc.BuildServiceProvider();

            var command = new AppCorrelateTimeCommand(Substitute.For<ILogger<AppCorrelateTimeCommand>>());

            var args = command.GetCommand().Parse($"monitor app diagnose time --subscription 4e960cbd-a6b8-49db-98ca-7d3fa323a005 --resource-name cdsfe-int-servicetelemetry-ai --symptom Correlate failing requests with exceptions by type using Application Insights time correlation analysis. --data-sets table:requests;filters:resultCode=\"500\";table:dependencies;filters:target=\"https://weu-nexus-int.documents.azure.com/\"");

            var context = new CommandContext(sp);

            var response = await command.ExecuteAsync(context, args);

        }
        [Fact]
        public async Task ActualCommand()
        {
            var sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));
            sc.AddSingleton<ICacheService, CacheService>();
            sc.AddSingleton<ITenantService, TenantService>();
            sc.AddSingleton<ISubscriptionService, SubscriptionService>();
            sc.AddSingleton<IResourceResolverService, ResourceResolverService>();
            sc.AddSingleton<IAppDiagnoseService, AppDiagnoseService>();

            var sp = sc.BuildServiceProvider();

            var command = new AppCorrelateTimeCommand(Substitute.For<ILogger<AppCorrelateTimeCommand>>());

            var args = command.GetCommand().Parse($"monitor app diagnose time --subscription 4e960cbd-a6b8-49db-98ca-7d3fa323a005 --resource-name cdsfe-int-servicetelemetry-ai --symptom Correlate failing requests with exceptions by type using Application Insights time correlation analysis. --data-sets table:requests;filters:success=false;splitBy:resultCode");

            var context = new CommandContext(sp);

            var response = await command.ExecuteAsync(context, args);

        }

        [Fact]
        public async Task ActualCommand2()
        {
            var sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));
            sc.AddSingleton<ICacheService, CacheService>();
            sc.AddSingleton<ITenantService, TenantService>();
            sc.AddSingleton<ISubscriptionService, SubscriptionService>();
            sc.AddSingleton<IResourceResolverService, ResourceResolverService>();
            sc.AddSingleton<IAppDiagnoseService, AppDiagnoseService>();

            var sp = sc.BuildServiceProvider();

            var command = new AppGetTraceCommand(Substitute.For<ILogger<AppGetTraceCommand>>());

            var args = command.GetCommand().Parse($"monitor app diagnose trace --subscription 4e960cbd-a6b8-49db-98ca-7d3fa323a005 --resource-name cdsfe-int-servicetelemetry-ai --trace-id 9dafc6072aae48bb9dc88d49611f8700 --start-time 2025-05-30T00:00:00Z --end-time 2025-06-01T00:00:00Z");

            var context = new CommandContext(sp);

            var response = await command.ExecuteAsync(context, args);

        }

        [Fact]
        public async Task ActualCommand8()
        {
            var sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));
            sc.AddSingleton<ICacheService, CacheService>();
            sc.AddSingleton<ITenantService, TenantService>();
            sc.AddSingleton<ISubscriptionService, SubscriptionService>();
            sc.AddSingleton<IResourceResolverService, ResourceResolverService>();
            sc.AddSingleton<IAppDiagnoseService, AppDiagnoseService>();

            var sp = sc.BuildServiceProvider();

            var command = new AppGetSpanCommand(Substitute.For<ILogger<AppGetSpanCommand>>());

            var args = command.GetCommand().Parse($"monitor app diagnose trace get-span --subscription 4e960cbd-a6b8-49db-98ca-7d3fa323a005 --resource-name cdsfe-int-servicetelemetry-ai --trace-id 9dafc6072aae48bb9dc88d49611f8700 --item-id 0d56f62e-3d5e-11f0-be51-002248a6681d --item-type exception --start-time 2025-05-30T00:00:00Z --end-time 2025-06-01T00:00:00Z");

            var context = new CommandContext(sp);

            var response = await command.ExecuteAsync(context, args);

        }

        [Fact]
        public async Task ActualCommand3()
        {
            var sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));
            sc.AddSingleton<ICacheService, CacheService>();
            sc.AddSingleton<ITenantService, TenantService>();
            sc.AddSingleton<ISubscriptionService, SubscriptionService>();
            sc.AddSingleton<IResourceResolverService, ResourceResolverService>();
            sc.AddSingleton<IAppDiagnoseService, AppDiagnoseService>();

            var sp = sc.BuildServiceProvider();

            var command = new AppListTraceCommand(Substitute.For<ILogger<AppListTraceCommand>>());

            var args = command.GetCommand().Parse($"monitor app correlate trace list --subscription 4e960cbd-a6b8-49db-98ca-7d3fa323a005 --resource-name cdsfe-int-servicetelemetry-ai --table dependencies --filters type='Azure DocumentDB'");

            var context = new CommandContext(sp);

            var response = await command.ExecuteAsync(context, args);
        }

        [Fact]
        public async Task ActualCommand4()
        {
            var sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));
            sc.AddSingleton<ICacheService, CacheService>();
            sc.AddSingleton<ITenantService, TenantService>();
            sc.AddSingleton<ISubscriptionService, SubscriptionService>();
            sc.AddSingleton<IResourceResolverService, ResourceResolverService>();
            sc.AddSingleton<IAppDiagnoseService, AppDiagnoseService>();

            var sp = sc.BuildServiceProvider();

            var command = new AppListTraceCommand(Substitute.For<ILogger<AppListTraceCommand>>());

            var args = command.GetCommand().Parse($"monitor app correlate trace list --subscription 4e960cbd-a6b8-49db-98ca-7d3fa323a005 --resource-name cdsfe-int-servicetelemetry-ai --table dependencies --filters duration='95p'");

            var context = new CommandContext(sp);

            var response = await command.ExecuteAsync(context, args);
        }

        [Fact]
        public async Task ActualCommand5()
        {
            var sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions()));
            sc.AddSingleton<ICacheService, CacheService>();
            sc.AddSingleton<ITenantService, TenantService>();
            sc.AddSingleton<ISubscriptionService, SubscriptionService>();
            sc.AddSingleton<IResourceResolverService, ResourceResolverService>();
            sc.AddSingleton<IAppDiagnoseService, AppDiagnoseService>();

            var sp = sc.BuildServiceProvider();

            var command = new AppImpactCommand(Substitute.For<ILogger<AppImpactCommand>>());

            var args = command.GetCommand().Parse($"monitor app correlate impact --subscription 4e960cbd-a6b8-49db-98ca-7d3fa323a005 --resource-name cdsfe-int-servicetelemetry-ai --table requests --filters resultCode='500'");

            var context = new CommandContext(sp);

            var response = await command.ExecuteAsync(context, args);
        }

        [Fact]
        public async Task RunThrough()
        {
            var cache = new CacheService(new MemoryCache(new MemoryCacheOptions()));
            var tenantService = new TenantService(cache);
            var subscriptionService = new SubscriptionService(cache, tenantService);
            var resourceResolver = new ResourceResolverService(subscriptionService, tenantService);
            var service = new AppDiagnoseService(Substitute.For<ILogger<AppDiagnoseService>>(), resourceResolver);

            var result = await service.CorrelateTimeSeries(
                subscription: "4e960cbd-a6b8-49db-98ca-7d3fa323a005",
                resourceGroup: null,
                resourceName: null,
                resourceId: "/subscriptions/4e960cbd-a6b8-49db-98ca-7d3fa323a005/resourceGroups/cds.int.monitoring/providers/microsoft.insights/components/cdsfe-int-servicetelemetry-ai",
                startTime: DateTime.UtcNow.AddDays(-1),
                endTime: DateTime.UtcNow,
                dataSets: new List<AzureMcp.Areas.Monitor.Models.AppCorrelateDataSet>
                {
                    new AzureMcp.Areas.Monitor.Models.AppCorrelateDataSet
                    {
                        Table = "requests",
                        Aggregation = "average"
                    },
                    new AzureMcp.Areas.Monitor.Models.AppCorrelateDataSet
                    {
                        Table = "dependencies",
                        Aggregation = "average",
                        SplitBy = "target"
                    }
                });

        }

    }
}
