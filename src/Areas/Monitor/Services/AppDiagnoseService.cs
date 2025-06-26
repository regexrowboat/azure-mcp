using System.Data;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using Azure;
using Azure.Core;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using AzureMcp.Areas.Monitor.Models;
using AzureMcp.Areas.Monitor.Options.App;
using AzureMcp.Commands.Monitor;
using AzureMcp.Options;
using AzureMcp.Services.Azure;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace AzureMcp.Areas.Monitor.Services
{
    public class AppDiagnoseService(ILogger<AppDiagnoseService> logger, IResourceResolverService resourceResolverService) : BaseAzureService, IAppDiagnoseService
    {
        private readonly ILogger<AppDiagnoseService> _logger = logger;
        private readonly IResourceResolverService _resourceResolverService = resourceResolverService;
        private static readonly StandardFields _standardFields = new StandardFields();

        public async Task<string?> SummarizeWithSampling<T>(IMcpServer? mcpServer, string intent, T data, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
        {
            if (mcpServer?.ClientCapabilities?.Sampling == null)
            {
                return null;
            }

            var samplingRequest = new CreateMessageRequestParams
            {
                Messages = [
                new SamplingMessage
                {
                    Role = Role.Assistant,
                    Content = new TextContentBlock {
                        Text = $"""
                            You are a "sub-agent" in a telemetry investigation workflow for Application Insights. Your job is to extract relevant details that help the "main agent" (caller) perform a root cause analysis. The "main agent" will express its intent to you.

                            Summarize the following data and extract the key information based on the "main agent" intent:
                            {intent}

                            Rules:
                            1. Always describe the evidence you based your conclusions on.
                            2. Always extract any IDs and time ranges the main agent would need to investigate further or drill into details.

                            {JsonSerializer.Serialize(data, typeInfo)}
                            """
                    }
                }
            ],
            };

            var samplingResponse = await mcpServer.SampleAsync(samplingRequest, cancellationToken);
            return (samplingResponse.Content as TextContentBlock)?.Text?.Trim();
        }

        public async Task<DistributedTraceResult> GetDistributedTrace(string subscription, string? resourceGroup, string? resourceName, string? resourceId, string traceId, string? spanId, DateTime startTime, DateTime endTime, string? tenant = null, RetryPolicyOptions? retryPolicy = null)
        {
            ResourceIdentifier resolvedResource = await _resourceResolverService.ResolveResourceIdAsync(subscription, resourceGroup, "microsoft.insights/components", resourceName ?? resourceId!, tenant, retryPolicy);

            var credential = await GetCredential(tenant);
            var options = AddDefaultPolicies(new LogsQueryClientOptions());

            if (retryPolicy != null)
            {
                options.Retry.Delay = TimeSpan.FromSeconds(retryPolicy.DelaySeconds);
                options.Retry.MaxDelay = TimeSpan.FromSeconds(retryPolicy.MaxDelaySeconds);
                options.Retry.MaxRetries = retryPolicy.MaxRetries;
                options.Retry.Mode = retryPolicy.Mode;
                options.Retry.NetworkTimeout = TimeSpan.FromSeconds(retryPolicy.NetworkTimeoutSeconds);
            }

            var client = new LogsQueryClient(credential, options);

            QueryTimeRange queryTimeRange = new QueryTimeRange(startTime, endTime);

            var kql = $"""
            union requests, dependencies, exceptions, (availabilityResults | extend success=iff(success=='1', "True", "False"))
            | where operation_Id == "{traceId}"
            | project-away customMeasurements, _ResourceId, itemCount, client_Type, client_Model, client_OS, client_IP, client_City, client_StateOrProvince, client_CountryOrRegion, client_Browser, appId, appName, iKey, sdkVersion
            """;

            var response = await client.QueryResourceAsync(resolvedResource, kql, queryTimeRange);

            List<SpanSummary> spans = new List<SpanSummary>();

            if (response != null)
            {
                // Dictionary to keep track of spans by ID and parent-child relationships
                var parents = new Dictionary<int, List<string>>();
                var spanMap = new Dictionary<string, SpanSummary>();
                SpanSummary[] allSpans = new SpanSummary[response.Value.Table.Rows.Count];

                Dictionary<string, int> columnMap = new();

                foreach (var column in response.Value.Table.Columns)
                {
                    // Map column names to their index for easy access
                    columnMap[column.Name] = columnMap.Count;
                }

                int rowId = 0;

                // Process each row in the query results
                foreach (var row in response.Value.Table.Rows)
                {
                    string? currentSpanId = GetColumnIfExists(row, columnMap, _standardFields.Id);
                    string? parentSpanId = GetColumnIfExists(row, columnMap, _standardFields.OperationParentId);

                    var successString = GetColumnIfExists(row, columnMap, _standardFields.Success);

                    var spanDetails = new SpanSummary
                    {
                        RowId = rowId,
                        SpanId = currentSpanId,
                        ParentId = parentSpanId,
                        ItemId = GetColumnIfExists(row, columnMap, _standardFields.ItemId),
                        ResponseCode = GetColumnIfExists(row, columnMap, _standardFields.ResultCode),
                        ItemType = GetColumnIfExists(row, columnMap, _standardFields.ItemType),
                        IsSuccessful = !string.IsNullOrEmpty(successString) ? Boolean.Parse(successString!) : null,
                        ChildSpans = new List<SpanSummary>(),
                        Name = GetColumnIfExists(row, columnMap, _standardFields.Name),
                        Duration = row[columnMap[_standardFields.Duration]] is double duration ? TimeSpan.FromMilliseconds(duration) : null
                    };

                    DateTime end = row[columnMap[_standardFields.Timestamp]] is DateTime timestamp ? timestamp : DateTime.TryParse(row[columnMap[_standardFields.Timestamp]].ToString(), out DateTime r) ? r : default;

                    string? target = GetColumnIfExists(row, columnMap, _standardFields.Target);
                    string? problemId = GetColumnIfExists(row, columnMap, _standardFields.ProblemId);
                    string? operationName = GetColumnIfExists(row, columnMap, _standardFields.OperationName);
                    string? name = GetColumnIfExists(row, columnMap, _standardFields.Name);

                    spanDetails.Name = target ?? problemId ?? operationName ?? name;

                    spanDetails.StartTime = end.Subtract(spanDetails.Duration ?? TimeSpan.Zero);
                    spanDetails.EndTime = end;

                    if (parentSpanId != null && currentSpanId != parentSpanId && parentSpanId != traceId)
                    {
                        if (parents.TryGetValue(rowId, out List<string>? existingParents))
                        {
                            // If we already have parents for this span, add the new parent
                            existingParents.Add(parentSpanId);
                        }
                        else
                        {
                            // Otherwise, create a new entry for this span with its parent
                            parents[rowId] = new List<string> { parentSpanId };
                        }
                    }

                    if (currentSpanId != null)
                    {
                        spanMap[currentSpanId] = spanDetails;
                    }

                    allSpans[rowId] = spanDetails;

                    rowId++;
                }

                foreach (var span in allSpans)
                {
                    // Find the parent span if it exists
                    if (span.ParentId != null && spanMap.TryGetValue(span.ParentId, out var parentSpan))
                    {
                        // Add this span to its parent's child spans
                        parentSpan.ChildSpans.Add(span);
                    }
                }

                if (spanId == null)
                {
                    // find the root spans
                    var rootSpans = allSpans.Where(t =>
                        !parents.TryGetValue(t.RowId, out List<string>? ids) ||
                        (ids?.All(id => !spanMap.TryGetValue(id, out SpanSummary? span)) ?? true) // Check if any parent actually exists
                    ).ToList();

                    spans = rootSpans;
                }
                // Filter to only include the specified span, its ancestors, and direct descendants
                else if (spanMap.TryGetValue(spanId, out var targetSpan))
                {
                    // Find all ancestor spans (parents up to the root)
                    var ancestorSpans = FindAncestorSpans(parents, spanMap, targetSpan);

                    // The direct descendants are already in the ChildSpans property after building the hierarchy
                    var directDescendants = targetSpan.ChildSpans;

                    // The result should include the target span, its ancestors, and direct descendants
                    var filteredSpans = new List<SpanSummary> { targetSpan };
                    filteredSpans.AddRange(ancestorSpans);
                    filteredSpans.AddRange(directDescendants);

                    // Set the filtered spans to the result
                    spans = filteredSpans;
                }
                else
                {
                    // If the specified span wasn't found, return an empty result
                    spans = new List<SpanSummary>();
                }
            }

            return FormatDistributedTrace(traceId, spans);
        }

        public async Task<SpanDetails[]> GetSpan(string subscription, string? resourceGroup, string? resourceName, string? resourceId, string itemId, string itemType, DateTime startTime, DateTime endTime, string? tenant = null, RetryPolicyOptions? retryPolicy = null)
        {
            ResourceIdentifier resolvedResource = await _resourceResolverService.ResolveResourceIdAsync(subscription, resourceGroup, "microsoft.insights/components", resourceName ?? resourceId!, tenant, retryPolicy);

            var credential = await GetCredential(tenant);
            var options = AddDefaultPolicies(new LogsQueryClientOptions());

            if (retryPolicy != null)
            {
                options.Retry.Delay = TimeSpan.FromSeconds(retryPolicy.DelaySeconds);
                options.Retry.MaxDelay = TimeSpan.FromSeconds(retryPolicy.MaxDelaySeconds);
                options.Retry.MaxRetries = retryPolicy.MaxRetries;
                options.Retry.Mode = retryPolicy.Mode;
                options.Retry.NetworkTimeout = TimeSpan.FromSeconds(retryPolicy.NetworkTimeoutSeconds);
            }

            var client = new LogsQueryClient(credential, options);

            QueryTimeRange queryTimeRange = new QueryTimeRange(startTime, endTime);

            var kql = $"""
            union requests, dependencies, exceptions, (availabilityResults | extend success=iff(success=='1', "True", "False"))
            | where itemId == "{itemId}" and itemType contains "{itemType}"
            | project-away customMeasurements, _ResourceId, itemCount, client_Type, client_Model, client_OS, client_IP, client_City, client_StateOrProvince, client_CountryOrRegion, client_Browser, appId, appName, iKey, sdkVersion
            | limit 3
            """;

            var response = await client.QueryResourceAsync(resolvedResource, kql, queryTimeRange);

            SpanDetails[] allSpans = new SpanDetails[response.Value.Table.Rows.Count];

            if (response != null)
            {
                // Dictionary to keep track of spans by ID and parent-child relationships
                var parents = new Dictionary<int, List<string>>();
                var spanMap = new Dictionary<string, SpanDetails>();
                
                Dictionary<string, int> columnMap = new();

                foreach (var column in response.Value.Table.Columns)
                {
                    // Map column names to their index for easy access
                    columnMap[column.Name] = columnMap.Count;
                }

                int rowId = 0;

                // Process each row in the query results
                foreach (var row in response.Value.Table.Rows)
                {
                    string? currentSpanId = GetColumnIfExists(row, columnMap, _standardFields.Id);
                    string? parentSpanId = GetColumnIfExists(row, columnMap, _standardFields.OperationParentId);

                    var successString = GetColumnIfExists(row, columnMap, _standardFields.Success);

                    var spanDetails = new SpanDetails
                    {
                        RowId = rowId,
                        SpanId = currentSpanId,
                        ParentId = parentSpanId,
                        ItemId = GetColumnIfExists(row, columnMap, _standardFields.ItemId),
                        OperationName = GetColumnIfExists(row, columnMap, _standardFields.OperationName),
                        ResponseCode = GetColumnIfExists(row, columnMap, _standardFields.ResultCode),
                        ItemType = GetColumnIfExists(row, columnMap, _standardFields.ItemType),
                        IsSuccessful = !string.IsNullOrEmpty(successString) ? Boolean.Parse(successString!) : null,
                        Properties = new List<KeyValuePair<string, string>>(),
                        StartTime = row[columnMap[_standardFields.Timestamp]] is DateTime timestamp ? timestamp : DateTime.TryParse(row[columnMap[_standardFields.Timestamp]].ToString(), out DateTime r) ? r : default,
                        Duration = row[columnMap[_standardFields.Duration]] is double duration ? TimeSpan.FromMilliseconds(duration) : null
                    };

                    spanDetails.EndTime = spanDetails.StartTime.Add(spanDetails.Duration ?? TimeSpan.Zero);

                    // Add all other properties as key-value pairs
                    foreach (var column in columnMap)
                    {
                        string columnName = column.Key;
                        string? value = row[column.Value]?.ToString();

                        if (!IsStandardField(columnName) && !string.IsNullOrEmpty(value))
                        {
                            spanDetails.Properties.Add(new KeyValuePair<string, string>(
                                columnName,
                                value!
                            ));
                        }
                    }

                    allSpans[rowId] = spanDetails;

                    rowId++;
                }
            }
            return allSpans;
        }

        public async Task<AppListTraceResult> ListDistributedTraces(string subscription, string? resourceGroup, string? resourceName, string? resourceId, string? filters, string table, DateTime startTime, DateTime endTime, string? tenant = null, RetryPolicyOptions? retryPolicy = null)
        {
            ResourceIdentifier resolvedResource = await _resourceResolverService.ResolveResourceIdAsync(subscription, resourceGroup, "microsoft.insights/components", resourceName ?? resourceId!, tenant, retryPolicy);

            var credential = await GetCredential(tenant);
            var options = AddDefaultPolicies(new LogsQueryClientOptions());

            if (retryPolicy != null)
            {
                options.Retry.Delay = TimeSpan.FromSeconds(retryPolicy.DelaySeconds);
                options.Retry.MaxDelay = TimeSpan.FromSeconds(retryPolicy.MaxDelaySeconds);
                options.Retry.MaxRetries = retryPolicy.MaxRetries;
                options.Retry.Mode = retryPolicy.Mode;
                options.Retry.NetworkTimeout = TimeSpan.FromSeconds(retryPolicy.NetworkTimeoutSeconds);
            }

            var client = new LogsQueryClient(credential, options);

            QueryTimeRange queryTimeRange = new QueryTimeRange(startTime, endTime);

            var query = BuildListTracesKQL(table, filters, startTime, endTime);

            var response = await client.QueryResourceAsync(resolvedResource, query, queryTimeRange);

            if (response == null || response.Value.Table.Rows.Count == 0)
            {
                return new AppListTraceResult
                {
                    Table = table,
                    Rows = new List<AppListTraceEntry>(),
                };
            }

            List<AppListTraceEntry> rows = new List<AppListTraceEntry>();
            Dictionary<string, int> columnMap = new Dictionary<string, int>();
            for (int i = 0; i < response.Value.Table.Columns.Count; i++)
            {
                columnMap[response.Value.Table.Columns[i].Name] = i;
            }

            foreach (var row in response.Value.Table.Rows)
            {
                var entry = new AppListTraceEntry
                {
                    ProblemId = GetColumnIfExists(row, columnMap, "problemId"),
                    Target = GetColumnIfExists(row, columnMap, "target"),
                    TestLocation = GetColumnIfExists(row, columnMap, "location"),
                    TestName = GetColumnIfExists(row, columnMap, "name"),
                    Type = GetColumnIfExists(row, columnMap, "type"),
                    OperationName = GetColumnIfExists(row, columnMap, "operation_Name"),
                    ResultCode = GetColumnIfExists(row, columnMap, "resultCode"),
                    Traces = (JsonSerializer.Deserialize<List<TraceIdEntry>>(row[columnMap["traces"]].ToString()!, MonitorJsonContext.Default.ListTraceIdEntry) ?? new List<TraceIdEntry>()).Distinct().ToList()
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

            var credential = await GetCredential(tenant);
            var options = AddDefaultPolicies(new LogsQueryClientOptions());

            if (retryPolicy != null)
            {
                options.Retry.Delay = TimeSpan.FromSeconds(retryPolicy.DelaySeconds);
                options.Retry.MaxDelay = TimeSpan.FromSeconds(retryPolicy.MaxDelaySeconds);
                options.Retry.MaxRetries = retryPolicy.MaxRetries;
                options.Retry.Mode = retryPolicy.Mode;
                options.Retry.NetworkTimeout = TimeSpan.FromSeconds(retryPolicy.NetworkTimeoutSeconds);
            }
            var client = new LogsQueryClient(credential, options);

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

            var credential = await GetCredential(tenant);
            var options = AddDefaultPolicies(new LogsQueryClientOptions());

            if (retryPolicy != null)
            {
                options.Retry.Delay = TimeSpan.FromSeconds(retryPolicy.DelaySeconds);
                options.Retry.MaxDelay = TimeSpan.FromSeconds(retryPolicy.MaxDelaySeconds);
                options.Retry.MaxRetries = retryPolicy.MaxRetries;
                options.Retry.Mode = retryPolicy.Mode;
                options.Retry.NetworkTimeout = TimeSpan.FromSeconds(retryPolicy.NetworkTimeoutSeconds);
            }

            var client = new LogsQueryClient(credential, options);

            QueryTimeRange queryTimeRange = new QueryTimeRange(startTime, endTime);

            var query = BuildImpactKQL(table, filters, startTime, endTime);

            var response = await client.QueryResourceAsync(resolvedResource, query, queryTimeRange);

            if (response == null || response.Value.Table.Rows.Count == 0)
            {
                return new List<AppImpactResult>();
            }

            List<AppImpactResult> results = new List<AppImpactResult>();
            Dictionary<string, int> columnMap = new Dictionary<string, int>();
            for (int i = 0; i < response.Value.Table.Columns.Count; i++)
            {
                columnMap[response.Value.Table.Columns[i].Name] = i;
            }
            foreach (var row in response.Value.Table.Rows)
            {
                var result = new AppImpactResult
                {
                    CloudRoleName = GetColumnIfExists(row, columnMap, "cloud_RoleName") ?? "Unknown",
                    ImpactedInstances = int.Parse(GetColumnIfExists(row, columnMap, "ImpactedInstances") ?? "0"),
                    TotalInstances = int.Parse(GetColumnIfExists(row, columnMap, "TotalInstances") ?? "0"),
                    ImpactedRequests = int.Parse(GetColumnIfExists(row, columnMap, "ImpactedRequests") ?? "0"),
                    TotalRequests = int.Parse(GetColumnIfExists(row, columnMap, "TotalRequests") ?? "0"),
                    ImpactedRequestsPercentage = double.Parse(GetColumnIfExists(row, columnMap, "ImpactedRequestsPercent") ?? "0.0"),
                    ImpactedInstancePercentage = double.Parse(GetColumnIfExists(row, columnMap, "ImpactedInstancePercent") ?? "0.0")
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

        private static string? GetColumnIfExists(LogsTableRow? row, Dictionary<string, int> columnMap, string columnName)
        {
            string? result = columnMap.TryGetValue(columnName, out int index) ? row?[index]?.ToString() : null;
            return string.IsNullOrEmpty(result) ? null : result;
        }

        private static DistributedTraceResult FormatDistributedTrace(string traceId, List<SpanSummary> spans)
        {
            if (spans.Count == 0)
            {
                return new DistributedTraceResult
                {
                    Description = "No spans available for the selected trace and span. Try a different traceId, spanId, or time range filter",
                    TraceDetails = null,
                    StartTime = null,
                    TraceId = traceId
                };
            }

            string description = $"This represents a distributed trace. Parent/child relationships are represented by indentation. Columns: ItemId, ItemType, Name, Success, ResultCode, StartToEnd";

            DateTime startTime = spans.Min(s => s.StartTime);

            spans = spans.OrderBy(s => s.StartTime).ToList();

            StringBuilder results = new StringBuilder();

            foreach (var span in spans)
            {
                AddSpan("", results, span, startTime);
            }

            return new DistributedTraceResult
            {
                Description = description,
                TraceDetails = results.ToString(),
                StartTime = startTime,
                TraceId = traceId
            };
        }

        private static void AddSpan(string indent, StringBuilder results, SpanSummary span, DateTime startTime)
        {
            string success = span.ItemType == "exception" ? "⚠️" : span.IsSuccessful.HasValue ? (span.IsSuccessful.Value ? "✅" : "❌") : "❓";
            double start = (span.StartTime - startTime).TotalMilliseconds;
            double end = (span.EndTime - startTime).TotalMilliseconds;
            string duration = $"{Math.Round(start, 2)}->{Math.Round(end, 2)}";
            results.AppendLine($"{indent}{span.ItemId}, {span.ItemType}, {span.Name}, {success}, {span.ResponseCode}, {duration}");

            var childSpans = span.ChildSpans.OrderBy(span => span.StartTime).ToList();

            foreach (var child in childSpans)
            {
                AddSpan(indent + "    ", results, child, startTime);
            }
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
                availabilityResults{(table == "availabilityResults" ? string.Join("\n", kqlFilters) : "")}
                | where timestamp >= start and timestamp <= end
                | extend success=iff(success == '1', "True", "False")
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

        private static bool IsStandardField(string columnName)
        {
            return _standardFields.Values.Contains(columnName, StringComparer.OrdinalIgnoreCase);
        }

        private class StandardFields
        {
            public IEnumerable<string> Values => StandardFieldValues;

            public static List<string> StandardFieldValues = new List<string>();

            public string Id = AddValue("id");
            public string OperationParentId = AddValue("operation_ParentId");
            public string OperationName = AddValue("operation_Name");
            public string ResultCode = AddValue("resultCode");
            public string ItemType = AddValue("itemType");
            public string Success = AddValue("success");
            public string Timestamp = AddValue("timestamp");
            public string Duration = AddValue("duration");
            public string OperationId = AddValue("operation_Id");
            public string ItemId = AddValue("itemId");
            public string ProblemId = AddValue("problemId");
            public string Type = AddValue("type");
            public string Name = AddValue("name");
            public string Target = AddValue("target");

            public StandardFields()
            {

            }

            private static string AddValue(string name)
            {
                StandardFieldValues.Add(name);
                return name;
            }
        }

        private static async Task<AppCorrelateTimeResult> ExecuteQueryAsync(ResourceIdentifier resourceId, LogsQueryClient client, QueryTimeRange timeRange, string query, string description, string interval)
        {
            try
            {
                List<AppCorrelateTimeSeries> timeSeries = new List<AppCorrelateTimeSeries>();
                var response = await client.QueryResourceAsync(
                    resourceId,
                    query,
                    timeRange);

                int valueIndex = -1;
                int splitIndex = -1;
                int i = 0;
                foreach (var column in response.Value.Table.Columns)
                {
                    if (column.Name == "Value")
                    {
                        valueIndex = i;
                    }
                    else if (column.Name == "split")
                    {
                        splitIndex = i;
                    }
                    i++;
                }

                foreach(var row in response.Value.Table.Rows)
                {
                    if (valueIndex == -1 || splitIndex == -1)
                    {
                        throw new Exception("Query result does not contain expected columns 'Value' and 'split'.");
                    }
                    string split = row[splitIndex]?.ToString() ?? "Unknown";
                    double[]? value = JsonSerializer.Deserialize(row[valueIndex].ToString()!, MonitorJsonContext.Default.DoubleArray);
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
