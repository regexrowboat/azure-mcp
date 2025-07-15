using AzureMcp.Options;

namespace AzureMcp.Areas.AppInsightsProfiler.Options;

public class AppInsightsProfilerOptions : SubscriptionOptions
{
    /// <summary>
    /// The base URL for the App Insights Profiler API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.appinsightsprofiler.azure.com/";

    /// <summary>
    /// Gets or sets the app ids of the application insights components.
    /// </summary>
    public List<Guid> AppIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the start date time for the insights query.
    /// Defaults to 24 hours ago.
    /// </summary>
    /// <returns></returns>
    public DateTime StartDateTimeUtc { get; set; } = DateTime.UtcNow.AddDays(-1);

    /// <summary>
    /// Gets or sets the end date time for the insights query.
    /// Defaults to the current time.
    /// </summary>
    public DateTime EndDateTimeUtc { get; set; } = DateTime.UtcNow;
}
