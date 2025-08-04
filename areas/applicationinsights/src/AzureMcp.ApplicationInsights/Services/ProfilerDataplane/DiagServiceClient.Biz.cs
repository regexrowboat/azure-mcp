using Azure;
using Azure.Core;
using Azure.ResourceManager.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceProfiler.DataPlane.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProfiler.DataPlane.Client;

public sealed partial class DiagServiceClient : IDiagServiceClient, IDisposable
{
    public async Task<IEnumerable<AggregatedInsightResult>> GetInsightsAsync(
        ResourceIdentifier resourceId,
        DateTime? startDateTimeUtc = null,
        DateTime? endDateTimeUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (resourceId is null)
        {
            throw new ArgumentNullException(nameof(resourceId));
        }

        Guid appId = ResolveApId(resourceId, cancellationToken);

        return await GetInsightsAsync(
            [appId],
            startDateTimeUtc,
            endDateTimeUtc,
            cancellationToken).ConfigureAwait(false);
    }

    public Task<IEnumerable<AggregatedInsightResult>> GetInsightsAsync(
        IEnumerable<Guid> appIds,
        DateTime? startDateTimeUtc = null,
        DateTime? endDateTimeUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (appIds is null)
        {
            throw new ArgumentNullException(nameof(appIds));
        }

        if (!appIds.Any())
        {
            throw new ArgumentException($"'{nameof(appIds)}' cannot be empty.", nameof(appIds));
        }

        return GetInsightsImpAsync(appIds, startDateTimeUtc, endDateTimeUtc, ClientJsonContext.Default.IEnumerableAggregatedInsightResult, cancellationToken);
    }

    public Task<IEnumerable<JsonNode>> GetRawInsightsAsync(
        ResourceIdentifier resourceId,
        DateTime? startDateTimeUtc = null,
        DateTime? endDateTimeUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (resourceId is null)
        {
            throw new ArgumentNullException(nameof(resourceId));
        }

        Guid appId = ResolveApId(resourceId, cancellationToken);

        return GetRawInsightsAsync(
            [appId],
            startDateTimeUtc,
            endDateTimeUtc,
            cancellationToken);
    }

    public Task<IEnumerable<JsonNode>> GetRawInsightsAsync(
        IEnumerable<Guid> appIds,
        DateTime? startDateTimeUtc = null,
        DateTime? endDateTimeUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (appIds is null)
        {
            throw new ArgumentNullException(nameof(appIds));
        }

        if (!appIds.Any())
        {
            throw new ArgumentException($"'{nameof(appIds)}' cannot be empty.", nameof(appIds));
        }

        return GetInsightsImpAsync(appIds, startDateTimeUtc, endDateTimeUtc, ClientJsonContext.Default.IEnumerableJsonNode, cancellationToken);
    }

    /// <summary>
    /// Downloads artifacts for a specific resource ID and artifact ID.
    /// </summary>
    /// <param name="resourceId"></param>
    /// <param name="artifactId"></param>
    /// <param name="destinationStream"></param>
    /// <param name="cancellationToken"></param>
    public async Task<bool> DownloadArtifactsAsync(ResourceIdentifier resourceId, Guid artifactId, Stream destinationStream, CancellationToken cancellationToken = default)
    {
        if (resourceId is null)
        {
            throw new ArgumentNullException(nameof(resourceId));
        }

        Guid appId = ResolveApId(resourceId, cancellationToken);
        return await DownloadArtifactsAsync(appId, artifactId, destinationStream, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Downloads artifacts for a specific app and artifact ID.
    /// </summary>
    /// <param name="appId">The app id of the application insights component.</param>
    /// <param name="artifactId">The artifact id.</param>
    /// <param name="destinationStream">The destination steam.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<bool> DownloadArtifactsAsync(Guid appId, Guid artifactId, Stream destinationStream, CancellationToken cancellationToken = default)
    {
        if (destinationStream is null)
        {
            throw new ArgumentNullException(nameof(destinationStream));
        }

        string path = $"/api/apps/{appId}/artifacts/{artifactId}";

        Response response = await GetAsync(path, apiVersion: "2024-03-06-preview", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            _logger.LogError("Failed to download artifact {ArtifactId} for app {AppId}. Status code: {StatusCode}", artifactId, appId, response.Status);
            return false;
        }

        using Stream contentStream = response.Content.ToStream();
        if (contentStream is null)
        {
            _logger.LogError("Response content stream is null for artifact {ArtifactId} for app {AppId}.", artifactId, appId);
            return false;
        }

        try
        {
            await contentStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully downloaded artifact {ArtifactId} for app {AppId}.", artifactId, appId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while downloading artifact {ArtifactId} for app {AppId}.", artifactId, appId);
            return false;
        }
    }

    private async Task<IEnumerable<T>> GetInsightsImpAsync<T>(IEnumerable<Guid> appIds, DateTime? startDateTimeUtc, DateTime? endDateTimeUtc, JsonTypeInfo<IEnumerable<T>> jsonTypeInfo, CancellationToken cancellationToken)
    {
        endDateTimeUtc ??= DateTime.UtcNow;
        startDateTimeUtc ??= endDateTimeUtc.Value.AddDays(-1);

        BulkAppsPostBody bulkAppsPostBody = new()
        {
            Apps = appIds
        };

        string path = $"api/apps/bulk/insights/rollups";
        Dictionary<string, string> queries = new()
        {
            ["startTime"] = startDateTimeUtc.Value.ToString("o"),
            ["endTime"] = endDateTimeUtc.Value.ToString("o"),
        };

        using JsonContent appsPostBody = JsonContent.Create(bulkAppsPostBody, ClientJsonContext.Default.BulkAppsPostBody, mediaType: MediaTypeHeaderValue.Parse("application/json"));
        Response response = await PostAsync(path, queries, apiVersion: "2025-01-07-preview", clientRequestId: null, appsPostBody, additionalHeaders: null, cancellationToken: cancellationToken).ConfigureAwait(false);

        IEnumerable<T>? result = await ReadAsAsync(
            response.Content.ToStream(),
            jsonTypeInfo,
            cancellationToken).ConfigureAwait(false);

        return result ?? [];
    }

    private Guid ResolveApId(ResourceIdentifier resourceId, CancellationToken cancellationToken)
    {
        ApplicationInsightsComponentResource applicationInsightsComponentResource = _armClient.GetApplicationInsightsComponentResource(resourceId);
        if (applicationInsightsComponentResource is null)
        {
            throw new ArgumentException($"Resource with ID '{resourceId}' is not an Application Insights component.", nameof(resourceId));
        }

        applicationInsightsComponentResource = applicationInsightsComponentResource.Get(cancellationToken);

        string appId = applicationInsightsComponentResource.Data.AppId;
        _logger.LogInformation("Resolving appId: {resourceId} => {appId}", resourceId, appId);
        return Guid.Parse(appId);
    }
}

