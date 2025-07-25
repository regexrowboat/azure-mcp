// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Areas.ApplicationInsights.Options;

namespace AzureMcp.Areas.ApplicationInsights.Models
{
    public class AppListTraceResult
    {
        [JsonPropertyName("table")]
        public string Table { get; set; } = string.Empty;

        [JsonPropertyName("rows")]
        public List<AppListTraceEntry> Rows { get; set; } = new();
    }
}
