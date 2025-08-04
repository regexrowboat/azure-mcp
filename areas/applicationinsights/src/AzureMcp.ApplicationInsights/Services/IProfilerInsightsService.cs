using System.Text.Json.Nodes;
using Azure.Core;

namespace AzureMcp.ApplicationInsights.Services;

public interface IProfilerInsightsService
{
    Task<List<JsonNode>> GetInsightsAsync(
        ResourceIdentifier resourceIdentifier,
        DateTime startDateTimeUtc,
        DateTime endDateTimeUtc,
        CancellationToken cancellationToken);
}
