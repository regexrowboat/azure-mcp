using System.Text.Json.Serialization.Metadata;
using AzureMcp.Areas.Monitor.Models;
using AzureMcp.Options;

namespace AzureMcp.Areas.Monitor.Services
{
    public interface IAppDiagnoseService
    {
        Task<AppCorrelateTimeResult[]> CorrelateTimeSeries(string subscription,
            string? resourceGroup,
            string? resourceName,
            string? resourceId,
            List<AppCorrelateDataSet> dataSets,
            DateTime startTime,
            DateTime endTime,
            string? tenant = null,
            RetryPolicyOptions? retryPolicy = null);

        Task<DistributedTraceResult> GetDistributedTrace(
            string subscription,
            string? resourceGroup,
            string? resourceName,
            string? resourceId,
            string traceId,
            string? spanId,
           DateTime startTime,
           DateTime endTime,
           string? tenant = null,
           RetryPolicyOptions? retryPolicy = null);

        Task<SpanDetails[]> GetSpan(
            string subscription,
            string? resourceGroup,
            string? resourceName,
            string? resourceId,
            string itemId,
            string ItemType,
           DateTime startTime,
           DateTime endTime,
           string? tenant = null,
           RetryPolicyOptions? retryPolicy = null);

        Task<AppListTraceResult> ListDistributedTraces(
            string subscription,
            string? resourceGroup,
            string? resourceName,
            string? resourceId,
            string? filters,
            string table,
           DateTime startTime,
           DateTime endTime,
           string? tenant = null,
           RetryPolicyOptions? retryPolicy = null);

        Task<List<AppImpactResult>> GetImpact(
            string subscription,
            string? resourceGroup,
            string? resourceName,
            string? resourceId,
            string? filters,
            string table,
           DateTime startTime,
           DateTime endTime,
           string? tenant = null,
           RetryPolicyOptions? retryPolicy = null);

        Task<string?> SummarizeWithSampling<T>(IMcpServer? mcpServer, string intent, T data, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken);
    }
}
