using System;
using Azure.Core;
using Azure.Core.Pipeline;

namespace ServiceProfiler.DataPlane.Client.CustomPolicies;

internal class UserAgentPolicy : HttpPipelineSynchronousPolicy
{
    private readonly string _userAgent;

    public UserAgentPolicy(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            throw new ArgumentException($"'{nameof(userAgent)}' cannot be null or empty.", nameof(userAgent));
        }

        _userAgent = userAgent;
    }

    public override void OnSendingRequest(HttpMessage message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        message.Request.Headers.SetValue("User-Agent", _userAgent);
        base.OnSendingRequest(message);
    }
}
