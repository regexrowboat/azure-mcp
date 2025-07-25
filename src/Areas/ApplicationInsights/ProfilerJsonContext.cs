using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AzureMcp.Areas.ApplicationInsights.Models;

namespace AzureMcp.Areas.ApplicationInsights;

[JsonSerializable(typeof(BulkAppsPostBody))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(List<JsonNode>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class ProfilerJsonContext : JsonSerializerContext
{
}
