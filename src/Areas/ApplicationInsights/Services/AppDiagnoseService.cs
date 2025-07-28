using System.Data;
using System.Text;
using Azure;
using Azure.Core;
using Azure.Monitor.Query;
using AzureMcp.Areas.ApplicationInsights.Models;
using AzureMcp.Areas.ApplicationInsights.Models.Query;
using AzureMcp.Areas.ApplicationInsights.Options;
using AzureMcp.Commands.Monitor;
using AzureMcp.Options;
using AzureMcp.Services.Azure;
using AzureMcp.Services.Azure.Resource;

namespace AzureMcp.Areas.ApplicationInsights.Services
{
    public class AppDiagnoseService(IResourceResolverService resourceResolverService, IAppLogsQueryService appLogsQueryService) : BaseAzureService, IAppDiagnoseService
    {
        private readonly IResourceResolverService _resourceResolverService = resourceResolverService;
        private readonly IAppLogsQueryService _queryService = appLogsQueryService;

        public async Task<DistributedTraceResult> GetDistributedTrace(string subscription, string? resourceGroup, string? resourceName, string? resourceId, string traceId, string? spanId, DateTime startTime, DateTime endTime, string? tenant = null, RetryPolicyOptions? retryPolicy = null)
        {
            ResourceIdentifier resolvedResource = await _resourceResolverService.ResolveResourceIdAsync(subscription, resourceGroup, "microsoft.insights/components", resourceName ?? resourceId!, tenant, retryPolicy);

            var client = await _queryService.CreateClientAsync(resolvedResource, tenant, retryPolicy);

            QueryTimeRange queryTimeRange = new QueryTimeRange(startTime, endTime);

            var kql = $"""
            union requests, dependencies, exceptions, (availabilityResults | extend success=iff(success=='1', "True", "False"))
            | where operation_Id == "{traceId}"
            | project-away customMeasurements, _ResourceId, itemCount, client_Type, client_Model, client_OS, client_IP, client_City, client_StateOrProvince, client_CountryOrRegion, client_Browser, appId, appName, iKey, sdkVersion
            """;

            var response = await client.QueryResourceAsync<DistributedTraceQueryResponse>(resolvedResource, kql, queryTimeRange);

            DistributedTraceGraphBuilder graphBuilder = new DistributedTraceGraphBuilder(traceId);

            List<SpanSummary> spans = new List<SpanSummary>();

            if (response != null)
            {
                int rowId = 0;

                // Process each row in the query results
                foreach (var row in response)
                {
                    var spanDetails = DistributedTraceQueryResponse.CreateSpanDetails(rowId, row);
                    graphBuilder.AddSpan(spanDetails);
                    rowId++;
                }

                List<SpanSummary> allSpans = graphBuilder.Build();

                if (spanId == null)
                {
                    // find the root spans
                    spans = allSpans.Where(t => t.ParentSpan == null).ToList();
                }
                else 
                {
                    var targetSpan = allSpans.FirstOrDefault(t => t.SpanId == spanId);

                    if (targetSpan == null)
                    {
                        // If the specified span wasn't found, return an empty result
                        spans = new List<SpanSummary>();
                    }
                    else
                    {
                        // Filter to only include the specified span, its ancestors, and direct descendants
                        var currentSpan = targetSpan;
                        // ancestors
                        while (currentSpan.ParentSpan != null)
                        {
                            spans.Add(currentSpan.ParentSpan);
                            currentSpan = currentSpan.ParentSpan;
                        }
                        // target span
                        spans.Add(targetSpan);
                        // children
                        spans.AddRange(targetSpan.ChildSpans);
                    }
                }
            }

            return DistributedTraceResult.Create(traceId, spans);
        }

        public async Task<AppListTraceResult> ListDistributedTraces(string subscription, string? resourceGroup, string? resourceName, string? resourceId, string? filters, string table, DateTime startTime, DateTime endTime, string? tenant = null, RetryPolicyOptions? retryPolicy = null)
        {
            ResourceIdentifier resolvedResource = await _resourceResolverService.ResolveResourceIdAsync(subscription, resourceGroup, "microsoft.insights/components", resourceName ?? resourceId!, tenant, retryPolicy);

            var client = await _queryService.CreateClientAsync(resolvedResource, tenant, retryPolicy);

            QueryTimeRange queryTimeRange = new QueryTimeRange(startTime, endTime);

            var query = BuildListTracesKQL(table, filters, startTime, endTime);

            var response = await client.QueryResourceAsync<ListTraceQueryResponse>(resolvedResource, query, queryTimeRange);

            if (response == null || response.Count == 0)
            {
                return new AppListTraceResult
                {
                    Table = table,
                    Rows = new List<AppListTraceEntry>(),
                };
            }

            List<AppListTraceEntry> rows = new List<AppListTraceEntry>();

            foreach (var row in response)
            {
                var entry = new AppListTraceEntry
                {
                    ProblemId = row.Data.problemId,
                    Target = row.Data.target,
                    TestLocation = row.Data.location,
                    TestName = row.Data.name,
                    Type = row.Data.type,
                    OperationName = row.Data.operation_Name,
                    ResultCode = row.Data.resultCode,
                    Traces = (JsonSerializer.Deserialize(row.Data.traces!, ApplicationInsightsJsonContext.Default.ListTraceIdEntry) ?? new List<TraceIdEntry>()).Distinct().ToList()
                };
                
                rows.Add(entry);
            }

            return new AppListTraceResult
            {
                Table = table,
                Rows = rows
            };
        }

        public async Task<AppCorrelateTimeResult[]> CorrelateTimeSeries(string subscription, string? resourceGroup, string? resourceName, string? resourceId, List<AppCorrelateDataSet> dataSets, DateTime startTime, DateTime endTime, string? tenant = null, RetryPolicyOptions? retryPolicy = null)
        {
            ResourceIdentifier resolvedResource = await _resourceResolverService.ResolveResourceIdAsync(subscription, resourceGroup, "microsoft.insights/components", resourceName ?? resourceId!, tenant, retryPolicy);

            var client = await _queryService.CreateClientAsync(resolvedResource, tenant, retryPolicy);

            // Convert the data sets into actual KQL queries...
            QueryTimeRange queryTimeRange = new QueryTimeRange(startTime, endTime);

            string interval = ComputeInterval(startTime, endTime);

            (string query, string description)[] queries = dataSets.Select(dataSet => BuildKQLQuery(dataSet, interval, startTime, endTime)).ToArray();

            var result = await Task.WhenAll(queries.Select(q => ExecuteQueryAsync(resolvedResource, client, queryTimeRange, q.query, q.description, interval)));

            return result;
        }

        public async Task<List<AppImpactResult>> GetImpact(string subscription, string? resourceGroup, string? resourceName, string? resourceId, string? filters, string table, DateTime startTime, DateTime endTime, string? tenant = null, RetryPolicyOptions? retryPolicy = null)
        {
            ResourceIdentifier resolvedResource = await _resourceResolverService.ResolveResourceIdAsync(subscription, resourceGroup, "microsoft.insights/components", resourceName ?? resourceId!, tenant, retryPolicy);

            var client = await _queryService.CreateClientAsync(resolvedResource, tenant, retryPolicy);

            QueryTimeRange queryTimeRange = new QueryTimeRange(startTime, endTime);

            var query = BuildImpactKQL(table, filters, startTime, endTime);

            var response = await client.QueryResourceAsync<ImpactQueryResponse>(resolvedResource, query, queryTimeRange);

            if (response == null || response.Count == 0)
            {
                return new List<AppImpactResult>();
            }

            List<AppImpactResult> results = new List<AppImpactResult>();
            foreach (var row in response)
            {
                var result = new AppImpactResult
                {
                    CloudRoleName = row.Data.cloud_RoleName ?? "Unknown",
                    ImpactedInstances = row.Data.ImpactedInstances,
                    TotalInstances = row.Data.TotalInstances,
                    ImpactedRequests = row.Data.ImpactedRequests,
                    TotalRequests = row.Data.TotalRequests,
                    ImpactedRequestsPercentage = row.Data.ImpactedRequestsPercent,
                    ImpactedInstancePercentage = row.Data.ImpactedInstancePercent
                };
                
                results.Add(result);
            }

            return results;
        }

        private static string BuildImpactKQL(string table, string? filters, DateTime startTime, DateTime endTime)
        {
            return $"""
                let start = datetime({startTime:O});
                let end = datetime({endTime:O});
                let total={table}
                | where timestamp > start and timestamp < end
                | summarize TotalInstances=dcount(cloud_RoleInstance), TotalRequests=sum(itemCount) by cloud_RoleName;
                {table}
                | where timestamp > start and timestamp < end
                {(filters != null ? string.Join("\n", GetKqlFilters(filters)) : "")}
                | summarize ImpactedInstances=dcount(cloud_RoleInstance), ImpactedRequests=sum(itemCount) by cloud_RoleName
                | join kind=rightouter (total) on cloud_RoleName
                | extend ImpactedInstances = iff(isempty(ImpactedInstances), 0, ImpactedInstances)
                | extend ImpactedRequests = iff(isempty(ImpactedRequests), 0, ImpactedRequests)
                | project
                    cloud_RoleName=cloud_RoleName1,
                    ImpactedInstances,
                    TotalInstances,
                    TotalRequests,
                    ImpactedRequests
                | extend
                    ImpactedRequestsPercent = round((todouble(ImpactedRequests) / TotalRequests) * 100, 3),
                    ImpactedInstancePercent = round((todouble(ImpactedInstances) / TotalInstances) * 100, 3)
                    | order by ImpactedRequestsPercent desc
                """;
        }

        private static string BuildListTracesKQL(string table, string? filters, DateTime startTime, DateTime endTime)
        {
            List<string> kqlFilters = (filters == null ? Array.Empty<string>() : GetKqlFilters(filters)).ToList();

            KeyValuePair<string, string>[]? percentileFilters = filters?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .Select(t => {
                    var split = t.Split('=');
                    return new KeyValuePair<string, string>(split[0].Trim().Trim('\'').Trim('\"'), split[1].Trim().Trim('\'').Trim('\"'));
                    })
                .Where(kvp => string.Equals(kvp.Key, "duration") && kvp.Value.EndsWith('p'))
                .Distinct()
                .ToArray();
            List<string> percentileFunctions = new();
            if (percentileFilters != null)
            {
                foreach (var filter in percentileFilters)
                {
                    if (!double.TryParse(filter.Value.Trim('p'), out double percentileValue) || percentileValue < 0 || percentileValue > 100)
                    {
                        throw new ArgumentException($"Invalid percentile value '{filter.Value}' for filter '{filter.Key}'. Must be a number between 0 and 100.");
                    }
                    percentileFunctions.Add($"""
                        let percentile{percentileValue} = toscalar({table} {string.Join("\n", kqlFilters)}
                        | where timestamp >= start and timestamp <= end
                        | summarize percentile(duration, {percentileValue}));
                        """);
                    kqlFilters.Add($"| where duration > percentile{percentileValue}");
                }
            }

            string requestsQuery = $"""
                requests{(table == "requests" ? string.Join("\n", kqlFilters) : "")}
                | where timestamp >= start and timestamp <= end
                | project operation_Name, resultCode, operation_Id, itemType{(table == "requests" ? ", id, itemCount" : "")}
                """;

            string dependenciesQuery = $"""
                dependencies{(table == "dependencies" ? string.Join("\n", kqlFilters) : "")}
                | where type != "InProc"
                | where timestamp >= start and timestamp <= end
                | project target, type, resultCode, operation_Id, itemType{(table == "dependencies" ? ", id, itemCount" : "")}
                """;

            string exceptionsQuery = $"""
                exceptions{(table == "exceptions" ? string.Join("\n", kqlFilters) : "")}
                | where timestamp >= start and timestamp <= end
                | project problemId, type, operation_Id, itemType{(table == "exceptions" ? ", itemCount" : "")}
                """;

            string availabilityResultsQuery = $"""
                availabilityResults
                | where timestamp >= start and timestamp <= end
                | extend success=iff(success == '1', "True", "False"){(table == "availabilityResults" ? string.Join("\n", kqlFilters) : "")}
                | project name, location, operation_Id, itemType{(table == "availabilityResults" ? ", id, itemCount" : "")}
                """;

            string mainTableQuery;
            string[] remainingQueries;
            string keyDimensions;
            switch (table)
            {
                case "requests":
                    mainTableQuery = requestsQuery;
                    remainingQueries = new[] { dependenciesQuery, exceptionsQuery, availabilityResultsQuery };
                    keyDimensions = "operation_Name, resultCode";
                    break;
                case "dependencies":
                    mainTableQuery = dependenciesQuery;
                    remainingQueries = new[] { requestsQuery, exceptionsQuery, availabilityResultsQuery };
                    keyDimensions = "target, type, resultCode";
                    break;
                case "exceptions":
                    mainTableQuery = exceptionsQuery;
                    remainingQueries = new[] { requestsQuery, dependenciesQuery, availabilityResultsQuery };
                    keyDimensions = "problemId, type";
                    break;
                case "availabilityResults":
                    mainTableQuery = availabilityResultsQuery;
                    remainingQueries = new[] { requestsQuery, dependenciesQuery, exceptionsQuery };
                    keyDimensions = "name, location";
                    break;
                default:
                    throw new InvalidOperationException("Invalid table specified. Valid values are 'requests', 'dependencies', 'exceptions', or 'availabilityResults'.");
            }

            return $$"""
                let start = datetime({{startTime:O}});
                let end = datetime({{endTime:O}});
                {{string.Join("\n", percentileFunctions)}}
                let min_length_8 = (s: string) {let len = strlen(s);case(len == 1, strcat(s, s, s, s, s, s, s, s),    len == 2 or len == 3, strcat(s, s, s, s),    len == 4 or len == 5 or len == 6 or len == 7, strcat(s, s),    s)};
                let ai_hash = (s: string) {
                    abs(toint(__hash_djb2(min_length_8(s))))
                };
                {{mainTableQuery}}
                | join kind=leftouter ({{remainingQueries[0]}}) on operation_Id
                | join kind=leftouter ({{remainingQueries[1]}}) on operation_Id
                | join kind=leftouter ({{remainingQueries[2]}}) on operation_Id
                | summarize sum(itemCount), arg_min(ai_hash(operation_Id), operation_Id, column_ifexists("id", '')) by itemType, operation_Name, resultCode, problemId, target, type, resultCode1, name, location
                | summarize traces=make_list(bag_pack('traceId', operation_Id, 'spanId', column_ifexists("id", '')), 3), sum(sum_itemCount) by itemType, {{keyDimensions}}
                | top 10 by sum_sum_itemCount desc
                """;
        }

        // Helper method to find all ancestor spans (parents, grandparents, etc.)
        private static List<SpanSummary> FindAncestorSpans(Dictionary<int, List<string>> parents, Dictionary<string, SpanSummary> spanMap, SpanSummary span)
        {
            var ancestors = new List<SpanSummary>();
            int currentRowId = span.RowId;

            while (parents.TryGetValue(currentRowId, out List<string>? parentIds))
            {
                // choose only 1 parent
                if (parentIds.Count == 0)
                    break;
                foreach (var parentId in parentIds)
                {
                    if (spanMap.TryGetValue(parentId, out var parentSpan))
                    {
                        ancestors.Add(parentSpan);
                        currentRowId = parentSpan.RowId;
                        break;
                    }
                }
                // no valid spans found
                break;
            }
            return ancestors;
        }

        private static async Task<AppCorrelateTimeResult> ExecuteQueryAsync(ResourceIdentifier resourceId, IAppLogsQueryClient client, QueryTimeRange timeRange, string query, string description, string interval)
        {
            try
            {
                List<AppCorrelateTimeSeries> timeSeries = new List<AppCorrelateTimeSeries>();
                var response = await client.QueryResourceAsync<TimeSeriesCorrelationResponse>(
                    resourceId,
                    query,
                    timeRange);

                foreach(var row in response)
                {
                    string split = row.Data.split ?? "Unknown";
                    double[]? value = JsonSerializer.Deserialize(row.Data.Value?.ToString()!, MonitorJsonContext.Default.DoubleArray);
                    timeSeries.Add(new AppCorrelateTimeSeries
                    {
                        Data = value ?? Array.Empty<double>(),
                        Label = split
                    });
                }

                return new AppCorrelateTimeResult
                {
                    TimeSeries = timeSeries,
                    Description = description,
                    Start = timeRange.Start?.UtcDateTime ?? DateTime.MinValue,
                    End = timeRange.End?.UtcDateTime ?? DateTime.MinValue,
                    Interval = interval
                };
            }
            catch (Exception ex)
            {
                string errorMessage = ex switch
                {
                    RequestFailedException rfe => $"Azure request failed: {rfe.Status} - {rfe.Message} - {query}",
                    TimeoutException => "The query timed out. Try simplifying your query or reducing the time range.",
                    _ => $"Error querying resource logs: {ex.Message}"
                };
                throw new Exception(errorMessage, ex);
            }
        }

        private static (string query, string description) BuildKQLQuery(AppCorrelateDataSet dataSet, string interval, DateTime start, DateTime end)
        {
            // TODO: Validate the table list and return a good error message if it's not right.

            string kqlAggregation;
            string aggregationName;
            switch (dataSet.Aggregation.ToLowerInvariant())
            {
                case "count":
                    aggregationName = "Count";
                    kqlAggregation = "sum(itemCount)";
                    break;
                case "average":
                    aggregationName = "Average duration";
                    kqlAggregation = "avg(duration)";
                    break;
                case "95thpercentile":
                    aggregationName = "95th Percentile duration";
                    kqlAggregation = "percentile(duration, 95)";
                    break;
                default:
                    throw new ArgumentException($"Unsupported aggregation type: {dataSet.Aggregation}. Valid values are 'Count', 'Average' and '95thPercentile'");
            }

            var filters = GetKqlFilters(dataSet.Filters);

            if (string.IsNullOrEmpty(dataSet.SplitBy))
            {
                return (query: $"""
                    let start = datetime({start:O});
                    let end = datetime({end:O});
                    let interval = {interval};
                    {dataSet.Table}{string.Join("\n", filters)}
                    | extend split = 'Overall {dataSet.Table} {aggregationName}'
                    | make-series Value={kqlAggregation} default=0 on timestamp from start to end step interval by split
                    | project Value, split
                    | extend startTime=start, endTime = end
                    | project startTime, endTime, split, Value
                    """, description: $"{aggregationName} of {dataSet.Table} {(!string.IsNullOrEmpty(dataSet.Filters) ? $"where {dataSet.Filters}" : "")}");
            }

            return (query: $"""
                let start = datetime({start:O});
                let end = datetime({end:O});
                let interval = {interval};
                let top10 = {dataSet.Table}{string.Join("\n", filters)}
                | where timestamp > start and timestamp < end
                | summarize sum(itemCount) by {dataSet.SplitBy}
                | top 10 by sum_itemCount desc
                | project split={dataSet.SplitBy};
                {dataSet.Table}{string.Join("\n", filters)}
                | extend split={dataSet.SplitBy}
                | where split in (top10)
                | make-series Value={kqlAggregation} default=0 on timestamp from start to end step interval by split
                | project Value, split=strcat('{dataSet.SplitBy}=', split)
                | union ({dataSet.Table}{string.Join("\n", filters)}
                    | extend split = 'Overall {dataSet.Table} {aggregationName}'
                    | make-series Value={kqlAggregation} default=0 on timestamp from start to end step interval by split
                    | project Value, split)
                | extend startTime=start, endTime = end
                | project startTime, endTime, split, Value
                """, description: $"{aggregationName} of {dataSet.Table} {(dataSet.Filters != null ? $"where {dataSet.Filters}" : "")} {(!string.IsNullOrEmpty(dataSet.SplitBy) ? $"split by {dataSet.SplitBy}" : "")}");
        }

        private static string[] GetKqlFilters(string filters)
        {
            return filters
                .Trim()
                .Split(',')
                .Select(f => f.Trim())
                .Where(f => !string.IsNullOrEmpty(f))
                .Select(f =>
                {
                    // Split the filter into key-value pairs, e.g. "key=value" ->
                    var split = f.Split('=');
                    if (split.Length != 2)
                    {
                        throw new ArgumentException($"Invalid filter format: '{f}'. Expected format is 'key=value'.");
                    }
                    return new KeyValuePair<string, string>(split[0].Trim().Trim('\'').Trim('\"'), split[1].Trim().Trim('\'').Trim('\"'));
                })
                .Where(kvp => !string.Equals(kvp.Key, "duration", StringComparison.OrdinalIgnoreCase) && !kvp.Value.EndsWith('p'))
                .Select(kvp => $"| where {kvp.Key} contains \"{kvp.Value}\"")
                .ToArray();
        }

        private static string ComputeInterval(DateTime start, DateTime end)
        {
            // Compute the interval based on the time range.
            // Try to keep a maximum of 60 buckets in the result.
            TimeSpan duration = end - start;
            if (duration.TotalMinutes <= 60)
            {
                return "2m"; // 2 minute interval for short ranges
            }
            else if (duration.TotalHours <= 4)
            {
                return "10m"; // 5 minute interval for short ranges
            }
            else if (duration.TotalHours <= 12)
            {
                return "30m"; // 15 minute interval for medium ranges
            }
            else if (duration.TotalHours <= 24)
            {
                return "1h"; // 30 minute interval for medium ranges
            }
            else if (duration.TotalDays <= 3)
            {
                return "2h"; // 1 hour interval for longer ranges
            }
            else if (duration.TotalDays <= 7)
            {
                return "6h"; // 3 hour interval for longer ranges
            }
            else if (duration.TotalDays <= 14)
            {
                return "12h"; // 6 hour interval for longer ranges
            }
            else if (duration.TotalDays <= 30)
            {
                return "1d"; // 12 hour interval for longer ranges
            }
            else
            {
                return "2d"; // 1d interval for longer ranges
            }
        }
    }
}
