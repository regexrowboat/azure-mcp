using System.Text.Json.Nodes;

namespace AzureMcp.Areas.ApplicationInsights.Services;

public interface IProfilerDataplaneService
{
    Task<List<JsonNode>> GetInsightsAsync(
        IEnumerable<Guid> appIds,
        DateTime startDateTimeUtc,
        DateTime endDateTimeUtc,
        CancellationToken cancellationToken);
}
