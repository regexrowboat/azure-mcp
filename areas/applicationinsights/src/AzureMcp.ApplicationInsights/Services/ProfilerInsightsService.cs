using System.Text.Json.Nodes;
using Azure.Core;
using AzureMcp.Core.Services.Azure;
using Microsoft.Extensions.Logging;
using ServiceProfiler.DataPlane.Client;

namespace AzureMcp.ApplicationInsights.Services;

internal sealed class ProfilerInsightsService : BaseAzureService, IProfilerInsightsService
{
    private readonly DiagServiceClientFactory _diagServiceClientFactory;
    private readonly ILogger _logger;

    public ProfilerInsightsService(
        DiagServiceClientFactory diagServiceClientFactory,
        ILogger<ProfilerInsightsService> logger)
    {
        _diagServiceClientFactory = diagServiceClientFactory ?? throw new ArgumentNullException(nameof(diagServiceClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task<List<JsonNode>> GetInsightsAsync(ResourceIdentifier resourceId, DateTime startDateTimeUtc, DateTime endDateTimeUtc, CancellationToken cancellationToken)
    {
        TokenCredential tokenCredential = await GetCredential(tenant: null).ConfigureAwait(false);
        IDiagServiceClient diagServiceClient = _diagServiceClientFactory.CreateClient(tokenCredential);
        try
        {
            IEnumerable<JsonNode> insights = await diagServiceClient.GetRawInsightsAsync(
                resourceId,
                startDateTimeUtc,
                endDateTimeUtc,
                cancellationToken).ConfigureAwait(false);

            return insights.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get insights for resource {ResourceIdentifier}", resourceId);
            throw;
        }
        finally
        {
            (diagServiceClient as IDisposable)?.Dispose();
        }
    }
}
