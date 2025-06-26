// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AzureMcp.Areas.Monitor.Models;
using AzureMcp.Models.Option;

namespace AzureMcp.Areas.Monitor.Options;

public static class MonitorOptionDefinitions
{
    public static class App
    {
        public const string ResourceNameName = "resource-name";
        public const string ResourceGroupName = "resource-group";
        public const string ResourceIdName = "resource-id";
        public const string SymptomName = "symptom";
        public const string StartTimeName = "start-time";
        public const string EndTimeName = "end-time";
        public const string CorrelationDataSetName = "data-sets";
        public const string SpanIdName = "span-id";
        public const string TraceIdName = "trace-id";
        public const string TableName = "table";
        public const string FiltersName = "filters";
        public const string ItemIdName = "item-id";
        public const string ItemTypeName = "item-type";
        public const string IntentName = "intent";

        public static readonly Option<string> Intent = new(
            $"--{IntentName}",
            "Describe what information you're trying to get by using this tool."
        )
        {
            IsRequired = true
        };

        public static readonly Option<string> ResourceName = new(
            $"--{ResourceNameName}",
            "The name of the Application Insights resource."
        )
        {
            IsRequired = false
        };

        public static readonly Option<string> ResourceGroup = new(
            $"--{ResourceGroupName}",
            "The name of the Azure resource group containing the Application Insights resource."
        )
        {
            IsRequired = false
        };

        public static readonly Option<string> ResourceId = new(
            $"--{ResourceIdName}",
            "The resource ID of the Application Insights resource."
        )
        {
            IsRequired = false
        };

        public static readonly Option<string> Symptom = new(
            $"--{SymptomName}",
            "The user-reported description of the problem occurring. Include as much detail as possible, including relevant details provided by the user such as result codes, operations, and time information."
        )
        {
            IsRequired = false
        };

        public static readonly Option<string> StartTime = new(
            $"--{StartTimeName}",
            () => DateTime.UtcNow.AddHours(-24).ToString("o"),
            "The start time of the investigation in ISO format (e.g., 2023-01-01T00:00:00Z). Defaults to 24 hours ago."
        )
        {
            IsRequired = false
        };

        public static readonly Option<string> EndTime = new(
            $"--{EndTimeName}",
            () => DateTime.UtcNow.ToString("o"),
            "The end time of the investigation in ISO format (e.g., 2023-01-01T00:00:00Z). Defaults to now."
        )
        {
            IsRequired = false
        };

        public static readonly Option<string> SpanId = new Option<string>(
            $"--{SpanIdName}",
            "The specific span ID in the trace to analyze.")
        {
            IsRequired = false
        };

        public static readonly Option<string> ItemId = new Option<string>(
            $"--{ItemIdName}",
            "The specific ItemId in the distributed trace to retrieve details of.")
        {
            IsRequired = true
        };

        public static readonly Option<string> ItemType = new Option<string>(
            $"--{ItemTypeName}",
            "The specific ItemType of the item in the distributed trace to retrieve details of. (e.g. 'exception')")
        {
            IsRequired = true
        };

        public static readonly Option<string> TraceId = new Option<string>(
            $"--{TraceIdName}",
            "The specific trace ID to analyze.")
        {
            IsRequired = true
        };

        public static readonly Option<string> Table = new Option<string>(
            $"--{TableName}",
            "The table to list traces for. Valid values are 'requests', 'dependencies', 'availabilityResults', 'exceptions'.")
        {
            IsRequired = true
        };

        public static readonly Option<string> Filters = new Option<string>(
            $"--{FiltersName}",
            "The filters to apply to the trace results. A comma-separated list of 'dimension=value'. Dimension names should be valid Application Insights column names. (e.g. \"success='false',resultCode='500'\")")
        {
            IsRequired = false
        };

        public static readonly Option<AppCorrelateDataSetParseResult> DataSets = new(
            $"--{CorrelationDataSetName}",
            (ArgumentResult result) =>
            {
                var tokens = result.Tokens.Select(t => t.Value)
                    .Select(s => s.Split("table:", StringSplitOptions.RemoveEmptyEntries).Select(t => "table:" + t)).SelectMany(t => t)
                .Select(t => t.Split(';', StringSplitOptions.RemoveEmptyEntries)).SelectMany(t => t).ToList();
                List<AppCorrelateDataSet> toReturn = new();
                string? table = null;
                string? filters = null;
                string? splitBy = null;
                string? aggregation = null;
                foreach (string currentToken in tokens)
                {
                    if (currentToken == null)
                    {
                        continue;
                    }
                    var splitPart = currentToken.Split(':', 2);

                    if (splitPart.Length != 2)
                    {
                        return new AppCorrelateDataSetParseResult()
                        {
                            IsValid = false,
                            ErrorMessage = $"Invalid format for the {CorrelationDataSetName} option. Each data set should be formatted as table:\"TableName\";filters:\"Dimension=Value,...\";splitBy:\"DimensionName\";aggregation:\"AggregationMethod\"."
                        };
                    }

                    var key = splitPart[0].Trim().ToLowerInvariant();
                    var value = splitPart[1].Trim();
                    if (key == "table")
                    {
                        if (table != null)
                        {
                            // add a new entry
                            var dataSet = new AppCorrelateDataSet
                            {
                                Table = table!,
                                Filters = filters ?? string.Empty,
                                SplitBy = splitBy ?? string.Empty,
                                Aggregation = aggregation ?? "Count"
                            };
                            toReturn.Add(dataSet);
                        }
                        table = value;
                        filters = null;
                        splitBy = null;
                        aggregation = null;
                    }

                    switch (key)
                    {
                        case "filters":
                            filters = value;
                            break;
                        case "splitby":
                            splitBy = value;
                            break;
                        case "aggregation":
                            aggregation = value;
                            break;
                    }
                }

                if (table != null)
                {
                    // add a new entry
                    var dataSet = new AppCorrelateDataSet
                    {
                        Table = table!,
                        Filters = filters ?? string.Empty,
                        SplitBy = splitBy ?? string.Empty,
                        Aggregation = aggregation ?? "Count"
                    };
                    toReturn.Add(dataSet);
                }

                return new AppCorrelateDataSetParseResult
                {
                    DataSets = toReturn,
                    IsValid = true
                };
            },
            isDefault: false,
            "The data sets to include in the correlation analysis. This is a list of one or more strings formatted as follows (each string represents a data set to compare): table:\"The name of the table to perform correlation analysis on. Should be a valid Application Insights table name\";filters:\"A comma-separated list of 'dimension=value'. Dimension names should be valid Application Insights column names\";splitBy:\"A single dimension to split by, or null (if data set should not be split). This should be a valid Application Insights column name.\";aggregation:\"The aggregation method to use. Default is 'Count'. Valid values are 'Count', 'Average' and '95thPercentile'.\""
        )
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.OneOrMore
        };
    }
    public const string TableNameName = "table-name";
    public const string TableTypeName = "table-type";
    public const string QueryTextName = "query";
    public const string HoursName = "hours";
    public const string LimitName = "limit";
    public const string EntityName = "entity";
    public const string HealthModelName = "model-name";

    public static readonly Option<string> TableType = new(
        $"--{TableTypeName}",
        () => "CustomLog",
        "The type of table to query. Options: 'CustomLog', 'AzureMetrics', etc."
    )
    {
        IsRequired = true
    };

    public static readonly Option<string> TableName = new(
        $"--{TableNameName}",
        "The name of the table to query. This is the specific table within the workspace."
    )
    {
        IsRequired = true
    };

    public static readonly Option<string> Query = new(
        $"--{QueryTextName}",
        "The KQL query to execute against the Log Analytics workspace. You can use predefined queries by name:\n" +
        "- 'recent': Shows most recent logs ordered by TimeGenerated\n" +
        "- 'errors': Shows error-level logs ordered by TimeGenerated\n" +
        "Otherwise, provide a custom KQL query."
    )
    {
        IsRequired = true
    };

    public static readonly Option<int> Hours = new(
        $"--{HoursName}",
        () => 24,
        "The number of hours to query back from now."
    )
    {
        IsRequired = true
    };

    public static readonly Option<int> Limit = new(
        $"--{LimitName}",
        () => 20,
        "The maximum number of results to return."
    )
    {
        IsRequired = true
    };

    public static class Metrics
    {
        // Metrics related options
        public const string ResourceIdName = "resource-id";
        public const string ResourceTypeName = "resource-type";
        public const string ResourceNameName = "resource-name";
        public const string MetricNamespaceName = "metric-namespace";
        public const string MetricNamesName = "metric-names";
        public const string StartTimeName = "start-time";
        public const string EndTimeName = "end-time";
        public const string IntervalName = "interval";
        public const string AggregationName = "aggregation";
        public const string FilterName = "filter";
        public const string SearchStringName = "search-string";

        public const string EntityName = "entity";
        public const string HealthModelName = "model-name";
        public const string MaxBucketsName = "max-buckets";

        // Metrics options
        public static readonly Option<string> MetricNamespaceOptional = new(
            $"--{MetricNamespaceName}",
            "The metric namespace to query. Obtain this value from the azmcp-monitor-metrics-definitions command."
        )
        {
            IsRequired = false
        };

        public static readonly Option<string> MetricNamespace = new(
            $"--{MetricNamespaceName}",
            "The metric namespace to query. Obtain this value from the azmcp-monitor-metrics-definitions command."
        )
        {
            IsRequired = true
        };

        public static readonly Option<string> MetricNames = new(
            $"--{MetricNamesName}",
            "The names of metrics to query (comma-separated)."
        )
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };

        public static readonly Option<string> StartTime = new(
            $"--{StartTimeName}",
            () => DateTime.UtcNow.AddHours(-24).ToString("o"),
            "The start time for the query in ISO format (e.g., 2023-01-01T00:00:00Z). Defaults to 24 hours ago."
        );

        public static readonly Option<string> EndTime = new(
            $"--{EndTimeName}",
            () => DateTime.UtcNow.ToString("o"),
            "The end time for the query in ISO format (e.g., 2023-01-01T00:00:00Z). Defaults to now."
        );

        public static readonly Option<string> Interval = new(
            $"--{IntervalName}",
            "The time interval for data points (e.g., PT1H for 1 hour, PT5M for 5 minutes)."
        );

        public static readonly Option<string> Aggregation = new(
            $"--{AggregationName}",
            "The aggregation type to use (Average, Maximum, Minimum, Total, Count)."
        );

        public static readonly Option<string> Filter = new(
            $"--{FilterName}",
            "OData filter to apply to the metrics query."
        );

        public static readonly Option<string> SearchString = new(
            $"--{SearchStringName}",
            "A string to filter the metric definitions by. Helpful for reducing the number of records returned. Performs case-insensitive matching on metric name and description fields."
        )
        {
            IsRequired = false
        };

        public static readonly Option<int> DefinitionsLimit = new(
            $"--limit",
            () => 10,
            "The maximum number of metric definitions to return. Defaults to 10."
        )
        {
            IsRequired = false
        };

        public static readonly Option<int> NamespacesLimit = new(
            $"--limit",
            () => 10,
            "The maximum number of metric namespaces to return. Defaults to 10."
        )
        {
            IsRequired = false
        };

        public static readonly Option<int> MaxBuckets = new(
            $"--{MaxBucketsName}",
            () => 50,
            "The maximum number of time buckets to return. Defaults to 50."
        )
        {
            IsRequired = false
        };

        public static readonly Option<string> OptionalResourceGroup = new(
            $"--{OptionDefinitions.Common.ResourceGroupName}",
            "The name of the Azure resource group. This is a logical container for Azure resources."
        )
        {
            IsRequired = false
        };

        public static readonly Option<string> ResourceType = new(
            $"--{ResourceTypeName}",
            "The Azure resource type (e.g., 'Microsoft.Storage/storageAccounts', 'Microsoft.Compute/virtualMachines'). If not specified, will attempt to infer from resource name."
        )
        {
            IsRequired = false
        };

        public static readonly Option<string> ResourceName = new(
            $"--{ResourceNameName}",
            "The name of the Azure resource to query metrics for."
        )
        {
            IsRequired = true
        };
    }

    public static class Health
    {
        public static readonly Option<string> Entity = new(
            $"--{EntityName}",
            "The entity to get health for."
        )
        {
            IsRequired = true
        };

        public static readonly Option<string> HealthModel = new(
            $"--{HealthModelName}",
            "The name of the health model for which to get the health."
        )
        {
            IsRequired = true
        };
    }
}
