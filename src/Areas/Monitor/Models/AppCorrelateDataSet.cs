using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Models
{
    public record AppCorrelateDataSet
    {
        /// <summary>
        /// The name of the dataset
        /// </summary>
        [JsonPropertyName("table")]
        public string Table { get; init; } = string.Empty;
        /// <summary>
        /// The list of filters applied to this dataset
        /// </summary>
        [JsonPropertyName("filters")]
        public string Filters { get; init; } = string.Empty;
        /// <summary>
        /// The split by dimension for this dataset
        /// </summary>
        [JsonPropertyName("splitBy")]
        public string SplitBy { get; init; } = string.Empty;

        /// <summary>
        /// The aggregation function to apply to the dataset. Valid values are 'Count', 'Average' and '95thPercentile'.
        /// </summary>
        [JsonPropertyName("aggregation")]
        public string Aggregation { get; init; } = "Count";
    }
}
