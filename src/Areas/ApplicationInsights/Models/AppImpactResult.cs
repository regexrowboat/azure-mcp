using System.Text.Json.Serialization;

namespace AzureMcp.Areas.ApplicationInsights.Models
{
    public class AppImpactResult
    {
        [JsonPropertyName("cloud_RoleName")]
        public string CloudRoleName { get; set; } = string.Empty;

        [JsonPropertyName("impactedInstances")]
        public int ImpactedInstances { get; set; }

        [JsonPropertyName("impactedRequests")]
        public int ImpactedRequests { get; set; }

        [JsonPropertyName("totalRequests")]
        public int TotalRequests { get; set; }

        [JsonPropertyName("totalInstances")]
        public int TotalInstances { get; set; }

        [JsonPropertyName("impactedRequestPercent")]
        public double ImpactedRequestsPercentage { get; set; }

        [JsonPropertyName("impactedInstancePercent")]
        public double ImpactedInstancePercentage { get; set; }
    }
}
