using System.Text.Json.Nodes;
using Azure.Core;

namespace ServiceProfiler.DataPlane.Client;

public interface IDiagServiceClient
{
    void Dispose();

    Task<IEnumerable<JsonNode>> GetRawInsightsAsync(ResourceIdentifier resourceId, DateTime? startDateTimeUtc = null, DateTime? endDateTimeUtc = null, CancellationToken cancellationToken = default);
}
