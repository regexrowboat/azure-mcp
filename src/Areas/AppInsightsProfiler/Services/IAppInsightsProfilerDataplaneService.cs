using System.Text.Json.Nodes;

namespace AzureMcp.Areas.AppInsightsProfiler.Services;

public interface IAppInsightsProfilerDataplaneService
{
    Task<List<JsonNode>> GetInsightsAsync(
        IEnumerable<Guid> appIds,
        DateTime startDateTimeUtc,
        DateTime endDateTimeUtc,
        CancellationToken cancellationToken);
}
