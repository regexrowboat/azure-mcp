using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Models
{
    public class AppFocusResult
    {
        [JsonPropertyName("table")]
        public string Table { get; set; } = string.Empty;

        [JsonPropertyName("filters")]
        public List<KeyValuePair<string, string>> Filters { get; set; } = new();

        [JsonPropertyName("aggregations")]
        public List<string> Aggregations = new();

        [JsonPropertyName("groupBy")]
        public List<string> GroupBy = new();
    }
}
