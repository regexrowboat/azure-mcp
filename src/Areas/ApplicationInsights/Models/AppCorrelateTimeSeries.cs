using System.Text.Json.Serialization;
using AzureMcp.Areas.Monitor.Models;

namespace AzureMcp.Areas.ApplicationInsights.Models
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
