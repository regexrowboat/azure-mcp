using System.Text.Json.Serialization;

namespace AzureMcp.Areas.ApplicationInsights.Models
{
    public class SpanDetails
    {
        [JsonPropertyName("itemId")]
        public string? ItemId { get; set; }

        [JsonPropertyName("properties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<KeyValuePair<string, string>> Properties { get; set; } = new List<KeyValuePair<string, string>>();
    }
}
