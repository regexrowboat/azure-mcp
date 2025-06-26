using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Models
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
    }
}
