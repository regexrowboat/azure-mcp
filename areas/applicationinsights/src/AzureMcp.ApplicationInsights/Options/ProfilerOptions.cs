using System.Text.Json.Serialization;
using AzureMcp.Core.Options;

namespace AzureMcp.ApplicationInsights.Options;

public class ProfilerOptions : SubscriptionOptions, IAppOptions
{
    [JsonPropertyName(ProfilerOptionDefinitions.StartDateTimeUtcName)]
    /// <summary>
    /// Gets or sets the start date time for the insights query.
    /// Defaults to 24 hours ago.
    /// </summary>
    public DateTime StartDateTimeUtc { get; set; } = DateTime.UtcNow.AddDays(-1);

    [JsonPropertyName(ProfilerOptionDefinitions.EndDateTimeUtcName)]
    /// <summary>
    /// Gets or sets the end date time for the insights query.
    /// Defaults to the current time.
    /// </summary>
    public DateTime EndDateTimeUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the maximum number of insights to return.
    /// </summary>
    [JsonPropertyName(ProfilerOptionDefinitions.MaxInsightsName)]
    public int MaxInsights { get; set; } = 50;

    [JsonPropertyName(ApplicationInsightsOptionDefinitions.ResourceNameName)]
    public string? ResourceName { get; set; }

    [JsonPropertyName(ApplicationInsightsOptionDefinitions.ResourceIdName)]
    public string? ResourceId { get; set; }
}
