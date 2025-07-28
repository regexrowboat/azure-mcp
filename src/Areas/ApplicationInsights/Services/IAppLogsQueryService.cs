using Azure.Core;
using AzureMcp.Options;

namespace AzureMcp.Areas.ApplicationInsights.Services
{
    public interface IAppLogsQueryService
    {
        Task<IAppLogsQueryClient> CreateClientAsync(ResourceIdentifier resolvedResource, string? tenant = null, RetryPolicyOptions? retryPolicy = null);
    }
}
