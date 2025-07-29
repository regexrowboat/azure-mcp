﻿using AzureMcp.Areas.ApplicationInsights.Models;

namespace AzureMcp.Areas.ApplicationInsights.Services
{
    public class DistributedTraceGraph(List<SpanSummary> spans)
    {
        private readonly List<SpanSummary> _spans = spans;

        public List<SpanSummary> FilterSpansById(string? spanId)
        {
            List<SpanSummary> filteredSpans = new List<SpanSummary>();
            if (spanId == null)
            {
                // find the root spans
                filteredSpans = _spans.Where(t => t.ParentSpan == null).ToList();
            }
            else
            {
                var targetSpan = _spans.FirstOrDefault(t => t.SpanId == spanId);

                if (targetSpan == null)
                {
                    // If the specified span wasn't found, return an empty result
                    filteredSpans = new List<SpanSummary>();
                }
                else
                {
                    // Filter to only include the specified span, its ancestors, and direct descendants
                    var currentSpan = targetSpan;
                    // ancestors
                    while (currentSpan.ParentSpan != null)
                    {
                        filteredSpans.Add(currentSpan.ParentSpan);
                        currentSpan = currentSpan.ParentSpan;
                    }
                    // target span
                    filteredSpans.Add(targetSpan);
                    // children
                    filteredSpans.AddRange(targetSpan.ChildSpans);
                }
            }
            return filteredSpans;
        }
    }
}
