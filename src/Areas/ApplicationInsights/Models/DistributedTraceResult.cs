using System.Text.Json.Serialization;
using Azure.Monitor.Query.Models;

namespace AzureMcp.Areas.ApplicationInsights.Models
{
    public class DistributedTraceResult
    {
        [JsonPropertyName("traceId")]
        public string TraceId { get; set; } = string.Empty;

        [JsonPropertyName("startTime")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public string? TraceDetails { get; set; }

        [JsonPropertyName("relevantSpans")]
        public List<SpanDetails> RelevantSpans { get; set; } = new List<SpanDetails>();
    }
}
