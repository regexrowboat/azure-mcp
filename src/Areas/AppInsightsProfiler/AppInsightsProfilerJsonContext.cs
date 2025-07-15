using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AzureMcp.Commands.AppInsightsProfiler;

[JsonSerializable(typeof(Areas.AppInsightsProfiler.Models.BulkAppsPostBody))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(List<JsonNode>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class AppInsightsProfilerJsonContext : JsonSerializerContext
{
}
