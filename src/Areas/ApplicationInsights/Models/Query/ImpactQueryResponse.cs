namespace AzureMcp.Areas.ApplicationInsights.Models.Query
{
    public class ImpactQueryResponse
    {
        public string? cloud_RoleName { get; set; }
        public int ImpactedInstances { get; set; } = 0;

        public int TotalInstances { get; set; } = 0;

        public int ImpactedRequests { get; set; } = 0;

        public int TotalRequests { get; set; } = 0;

        public double ImpactedRequestsPercent { get; set; } = 0.0;

        public double ImpactedInstancePercent { get; set; } = 0.0;
    }
}
