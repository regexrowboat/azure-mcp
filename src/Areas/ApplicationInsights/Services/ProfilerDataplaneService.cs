using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using AzureMcp.Areas.ApplicationInsights.Models;
using AzureMcp.Services.Azure;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Areas.ApplicationInsights.Services;

internal sealed class ProfilerDataplaneService : BaseAzureService, IProfilerDataplaneService, IDisposable
{
    private const string MonitorScope = "api://dataplane.diagnosticservices.azure.com/.default";
    private const string BaseUrl = "https://dataplane.diagnosticservices.azure.com/";

    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public ProfilerDataplaneService(ILogger<ProfilerDataplaneService> logger)
    {
        _httpClient = CreateHttpClient();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private HttpClient CreateHttpClient()
    {
        HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.BaseAddress = new Uri(BaseUrl);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<List<JsonNode>> GetInsightsAsync(IEnumerable<Guid> appIds, DateTime startDateTimeUtc, DateTime endDateTimeUtc, CancellationToken cancellationToken)
    {
        HttpClient dataplaneClient = await GetAuthenticatedHttpClientAsync().ConfigureAwait(false);
        BulkAppsPostBody bulkAppsPostBody = new()
        {
            Apps = appIds
        };

        JsonContent appsPostBody = JsonContent.Create(bulkAppsPostBody, ProfilerJsonContext.Default.BulkAppsPostBody, mediaType: MediaTypeHeaderValue.Parse("application/json"));
        HttpResponseMessage response = await dataplaneClient.PostAsync($"api/apps/bulk/insights/rollups?startTime={startDateTimeUtc:o}&endTime={endDateTimeUtc:o}&api-version=2025-01-07-preview", appsPostBody, cancellationToken).ConfigureAwait(false);

        List<JsonNode>? result = await JsonSerializer.DeserializeAsync(
            await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
            ProfilerJsonContext.Default.ListJsonNode,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result ?? new List<JsonNode>();
    }

    private async Task<HttpClient> GetAuthenticatedHttpClientAsync()
    {
        TokenCredential tokenCredential = await GetCredential(tenant: null).ConfigureAwait(false);
        AccessToken token = await tokenCredential.GetTokenAsync(
            new TokenRequestContext([MonitorScope]),
            CancellationToken.None).ConfigureAwait(false);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return _httpClient;
    }
}
