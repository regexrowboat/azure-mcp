// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using AzureMcp.Areas.Monitor.Options.App;

namespace AzureMcp.Areas.Monitor.Models
{
    public class AppListTraceResult
    {
        [JsonPropertyName("table")]
        public string Table { get; set; } = string.Empty;

        [JsonPropertyName("rows")]
        public List<AppListTraceEntry> Rows { get; set; } = new();
    }
}
