// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Options.App
{
    public class AppGetSpanOptions : BaseAppOptions
    {
        /// <summary>
        /// The start time of the investigation in ISO format (optional).
        /// </summary>
        [JsonPropertyName(MonitorOptionDefinitions.App.StartTimeName)]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The end time of the investigation in ISO format (optional).
        /// </summary>
        [JsonPropertyName(MonitorOptionDefinitions.App.EndTimeName)]
        public DateTime EndTime { get; set; }

        [JsonPropertyName(MonitorOptionDefinitions.App.ItemIdName)]
        public string? ItemId { get; set; }

        [JsonPropertyName(MonitorOptionDefinitions.App.ItemTypeName)]
        public string? ItemType { get; set; }
    }
}
