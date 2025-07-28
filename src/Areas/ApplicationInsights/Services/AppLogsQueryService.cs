using Azure.Core;
using Azure.Monitor.Query;
using AzureMcp.Options;
using AzureMcp.Services.Azure;

namespace AzureMcp.Areas.ApplicationInsights.Services
{
    public class AppLogsQueryService() : BaseAzureService, IAppLogsQueryService
    {
        public async Task<IAppLogsQueryClient> CreateClientAsync(ResourceIdentifier resolvedResource, string? tenant = null, RetryPolicyOptions? retryPolicy = null)
        {
            var credential = await GetCredential(tenant);
            var options = AddDefaultPolicies(new LogsQueryClientOptions());

            if (retryPolicy != null)
            {
                options.Retry.Delay = TimeSpan.FromSeconds(retryPolicy.DelaySeconds);
                options.Retry.MaxDelay = TimeSpan.FromSeconds(retryPolicy.MaxDelaySeconds);
                options.Retry.MaxRetries = retryPolicy.MaxRetries;
                options.Retry.Mode = retryPolicy.Mode;
                options.Retry.NetworkTimeout = TimeSpan.FromSeconds(retryPolicy.NetworkTimeoutSeconds);
            }

            var client = new LogsQueryClient(credential, options);

            return new AppLogsQueryClient(client);
        }
    }
}
