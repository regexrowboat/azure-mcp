using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.ResourceManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ServiceProfiler.DataPlane.Client.CustomPolicies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProfiler.DataPlane.Client;

public sealed partial class DiagServiceClient : IDisposable
{
    private readonly ILogger _logger;
    private readonly DiagServiceClientOptions _options;
    private readonly ArmClient _armClient;
    private readonly DisposableHttpPipeline _pipeline;

    [ActivatorUtilitiesConstructor]
    public DiagServiceClient(
        [FromKeyedServices(typeof(DiagServiceClient))] TokenCredential tokenCredential,
        IOptions<DiagServiceClientOptions> options,
        ILogger<DiagServiceClient> logger)
        : this(tokenCredential, options?.Value ?? throw new ArgumentNullException(nameof(options)), logger)
    {
    }

    public DiagServiceClient(
        TokenCredential tokenCredential,
        DiagServiceClientOptions options,
        ILogger<DiagServiceClient>? logger = null)
    {
        _logger = logger ?? NullLogger<DiagServiceClient>.Instance;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _pipeline = BuildPipeline(options, tokenCredential);
        _armClient = new ArmClient(tokenCredential);
    }

    private ValueTask<T?> ReadAsAsync<T>(Stream stream, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (jsonTypeInfo is null)
        {
            throw new ArgumentNullException(nameof(jsonTypeInfo));
        }

        return JsonSerializer.DeserializeAsync<T>(stream, jsonTypeInfo, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Invoke GET on the given path on the data plane.
    /// </summary>
    /// <param name="path">The path and, optionally, query string.</param>
    /// <param name="queries">Optional queries to append to the path.</param>
    /// <param name="apiVersion">The API version to use.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="clientRequestId">An optional client request ID to include in the request headers.</param>
    /// <returns>A <see cref="ValueTask{Response}"/> representing the asynchronous operation.</returns>
    internal ValueTask<Response> GetAsync(string path,
        string apiVersion,
        CancellationToken cancellationToken,
        IDictionary<string, string>? queries = null,
        string? clientRequestId = null,
        IDictionary<string, IEnumerable<string>>? headers = null)
    {
        using Request request = CreateRequest(RequestMethod.Get, clientRequestId);
        return SendRequestAsync(path, queries, request, headers, apiVersion, cancellationToken);
    }

    /// <summary>
    /// Call the given path on the data plane, passing the tenant ID and object ID of the
    /// caller in headers. Posted with content.
    /// </summary>
    /// <param name="path">The path and, optionally, query string.</param>
    /// <param name="queries">Optional queries to append to the path.</param>
    /// <param name="clientRequestId">Optional client request ID.</param>
    /// <param name="httpContent">The content of the incoming request.</param>
    /// <param name="additionalHeaders">Additional headers to be added to the request</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    internal async ValueTask<Response> PostAsync(string path, IDictionary<string, string>? queries, string apiVersion, string? clientRequestId, HttpContent? httpContent, IDictionary<string, IEnumerable<string>>? additionalHeaders, CancellationToken cancellationToken)
    {
        using Request request = CreateRequest(RequestMethod.Post, clientRequestId);
        if (httpContent is not null)
        {
            request.Content = BinaryData.FromStream(await httpContent.ReadAsStreamAsync().ConfigureAwait(false));
            if (httpContent.Headers.ContentType != null)
            {
                request.Headers.Add(HttpHeader.Names.ContentType, httpContent.Headers.ContentType.ToString());
            }
        }

        return await SendRequestAsync(path, queries, request, additionalHeaders, apiVersion, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a request to the specified path and query with the given request and additional headers.
    /// </summary>
    /// <param name="path">The path and query.</param>
    /// <param name="queries">Optional queries to append to the path.</param>
    /// <param name="request">The request.</param>
    /// <param name="headers">Additional headers.</param>
    /// <param name="apiVersion">The api-version to use.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    private async ValueTask<Response> SendRequestAsync(string path, IDictionary<string, string>? queries, Request request, IDictionary<string, IEnumerable<string>>? headers, string apiVersion, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException($"'{nameof(path)}' cannot be null or empty.", nameof(path));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrEmpty(apiVersion))
        {
            throw new ArgumentException($"'{nameof(apiVersion)}' cannot be null or empty.", nameof(apiVersion));
        }

        request.Uri.AppendPath(path, escape: true);

        if (queries is not null && queries.Any())
        {
            foreach (var query in queries)
            {
                request.Uri.AppendQuery(query.Key, query.Value, escapeValue: true);
            }
        }

        request.Uri.AppendQuery("api-version", apiVersion);

        AppendHeaders(request, headers);

        // Send the request and return the response
        _logger.LogDebug("Sending request to {Uri}", request.Uri);
        return await _pipeline.SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static void AppendHeaders(Request request, IDictionary<string, IEnumerable<string>>? headers)
    {
        if (headers != null)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                foreach (string value in header.Value)
                {
                    request.Headers.Add(header.Key, value);
                }
            }
        }
    }

    private Request CreateRequest(RequestMethod method, string? clientRequestId)
    {
        Request request = _pipeline.CreateRequest();
        request.Method = method;
        if (!string.IsNullOrEmpty(clientRequestId))
        {
            request.ClientRequestId = clientRequestId;
        }

        return request;
    }

    public void Dispose()
    {
        _pipeline?.Dispose();
    }

    private DisposableHttpPipeline BuildPipeline(ClientOptions options, TokenCredential tokenCredential)
    {
        // Create a pipeline with necessary policies and handlers
        return HttpPipelineBuilder.Build(
                options,
                perCallPolicies:
                [
                    new UserAgentPolicy(_options.UserAgent),
                    new EndpointPolicy(_options.Endpoint),
                    new BearerTokenAuthenticationPolicy(tokenCredential, _options.Scope),
                ],
                perRetryPolicies:
                [
                ],
                transportOptions: new HttpPipelineTransportOptions
                {
                    IsClientRedirectEnabled = true
                },
                responseClassifier: null);
    }
}

