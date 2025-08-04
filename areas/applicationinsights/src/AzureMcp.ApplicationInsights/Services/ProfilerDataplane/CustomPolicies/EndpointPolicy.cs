using System;
using Azure.Core;
using Azure.Core.Pipeline;

namespace ServiceProfiler.DataPlane.Client.CustomPolicies;

/// <summary>
/// A policy that sets the endpoint for the request.
/// This policy ensures that the request URI has the correct host set to the specified endpoint.
/// </summary>
internal class EndpointPolicy : HttpPipelineSynchronousPolicy
{
    private readonly Uri _endpoint;
    public EndpointPolicy(Uri endpoint)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    public override void OnSendingRequest(HttpMessage message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        RequestUriBuilder uriBuilder = message.Request.Uri;
        if (uriBuilder.Host is null)
        {
            uriBuilder.Reset(new Uri(_endpoint, uriBuilder.PathAndQuery));
        }

        base.OnSendingRequest(message);
    }
}
