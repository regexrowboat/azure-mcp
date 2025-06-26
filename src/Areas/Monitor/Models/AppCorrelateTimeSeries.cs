using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Models
{
    public class AppCorrelateTimeSeries
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("timeSeries")]
        [JsonConverter(typeof(RoundedDoubleArrayConverter))]
        public double[] Data { get; set; } = Array.Empty<double>();
    }
}
