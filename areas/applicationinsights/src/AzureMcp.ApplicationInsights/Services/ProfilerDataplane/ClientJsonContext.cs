using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.ServiceProfiler.DataPlane.Contracts;

namespace ServiceProfiler.DataPlane.Client;

[JsonSerializable(typeof(BulkAppsPostBody))]
[JsonSerializable(typeof(IEnumerable<AggregatedInsightResult>))]
[JsonSerializable(typeof(IEnumerable<JsonNode>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class ClientJsonContext : JsonSerializerContext
{
}
