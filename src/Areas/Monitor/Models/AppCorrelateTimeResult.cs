﻿using System.Text.Json.Serialization;

namespace AzureMcp.Areas.Monitor.Models
{
    public class AppCorrelateTimeResult
    {
        [JsonPropertyName("start")]
        public DateTime Start { get; set; }

        [JsonPropertyName("end")]
        public DateTime End { get; set; }

        [JsonPropertyName("interval")]
        public string Interval { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("timeSeries")]
        public List<AppCorrelateTimeSeries> TimeSeries { get; set; } = new();
    }
}
