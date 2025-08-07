// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ServiceProfiler.DataPlane.Contracts;

/// <summary>
/// Represents a PerfLens insight aggregate/roll-up .This is a contract between the data plane
/// and the Portal UX.
/// </summary>
public record class AggregatedInsightResult
{
    /// <summary>
    /// MD5 hash of the aggregated properties. (IssueCategory, Function, ParentFunction, AppId, RoleName, IssueId)
    /// Joined with a underscore.
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Number of insights returned with the record.
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// The application ID
    /// </summary>
    public Guid AppId { get; set; }

    /// <summary>
    /// The registry ID correlating to an issue in the issue registry.
    /// </summary>
    public string IssueId { get; set; } = null!;

    /// <summary>
    /// The largest threshold value in the result list.
    /// </summary>
    public double Criteria { get; set; }

    /// <summary>
    /// A string to describe where the issue is.
    /// </summary>
    public string IssueCategory { get; set; } = null!;

    /// <summary>
    /// A relation descriptor for criteria to value. See the substitute below for an example.
    /// </summary>
    public string Relation { get; set; } = "<";

    /// <summary>
    /// Value of the performance metric.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// If the blob is derived from, or closely associated with another blob, then the value is the URI to the original artifact. For example, this is used for the output of a transcoder, or the result of running analysis on an ingested artifact.
    /// Blobs without Provenance may be deleted by the clean-up service. Blobs with Provenance may be deleted when the primary artifact is deleted by the clean-up service.
    /// </summary>
    public string? Provenance { get; set; }

    /// <summary>
    /// The CorrelationId. Used to track artifacts across services.
    /// One is created if not provided.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Source that produced the artifact.
    /// (e.g. "Agent", "PerfLens", "Transcoder", "Aggregator")
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// The role name, if any.
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// A list of all containers that have insights in this result.
    /// </summary>
    public IEnumerable<string> Containers { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// The most recent timestamp from this result.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The call stack of the issue.
    /// </summary>
    public IEnumerable<string>? Context { get; set; }

    /// <summary>
    /// The symbol that is the bottleneck of the issue.
    /// </summary>
    public string? Symbol { get; set; }

    /// <summary>
    /// The parent symbol of the issue.
    /// </summary>
    public string? ParentSymbol { get; set; }

    /// <summary>
    /// Processed method used in rationale
    /// </summary>
    public string? Function { get; set; }

    /// <summary>
    /// Processed component used in rationale
    /// </summary>
    public string? ParentFunction { get; set; }

    /// <summary>
    /// Total number of trace occurrences across all insights in this result.
    /// </summary>
    public long TraceOccurrences { get; set; }

    /// <summary>
    /// Whether the issue is automatically fixable.
    /// </summary>
    public bool IsFixable { get; set; } = false;

    /// <summary>
    /// Additional data that analyzers may provide.
    /// </summary>
    public IDictionary<string, string>? Payload { get; set; }
}
