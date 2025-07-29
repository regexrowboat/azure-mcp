using System.Text.Json.Serialization;
using AzureMcp.Areas.Monitor.Options;

namespace AzureMcp.Areas.ApplicationInsights.Options
{
    public class AppListTraceOptions : BaseAppOptions
    {
        /// <summary>
        /// The start time of the investigation in ISO format (optional).
        /// </summary>
        [JsonPropertyName(ApplicationInsightsOptionDefinitions.StartTimeName)]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The end time of the investigation in ISO format (optional).
        /// </summary>
        [JsonPropertyName(ApplicationInsightsOptionDefinitions.EndTimeName)]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// The table to list traces on
        /// </summary>
        [JsonPropertyName(ApplicationInsightsOptionDefinitions.TableName)]
        public string? Table { get; set; }

        /// <summary>
        /// Filters for the traces
        /// </summary>
        [JsonPropertyName(ApplicationInsightsOptionDefinitions.FiltersName)]
        public string[] Filters { get; set; } = Array.Empty<string>();
    }
}
