using System.Text.Json.Serialization;
using AzureMcp.Options;

namespace AzureMcp.Areas.ApplicationInsights.Options;

public class ProfilerOptions : SubscriptionOptions
{
    [JsonPropertyName("base-url")]
    /// <summary>
    /// The base URL for the App Insights Profiler API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.appinsightsprofiler.azure.com/";

    [JsonPropertyName("app-id")]
   /// <summary>
    /// Gets or sets the app ids of the application insights components.
    /// </summary>
    public Guid AppId { get; set; }

    [JsonPropertyName("start-date-time-utc")]
    /// <summary>
    /// Gets or sets the start date time for the insights query.
    /// Defaults to 24 hours ago.
    /// </summary>
    public DateTime StartDateTimeUtc { get; set; } = DateTime.UtcNow.AddDays(-1);

    [JsonPropertyName("end-date-time-utc")]
    /// <summary>
    /// Gets or sets the end date time for the insights query.
    /// Defaults to the current time.
    /// </summary>
    public DateTime EndDateTimeUtc { get; set; } = DateTime.UtcNow;
}
