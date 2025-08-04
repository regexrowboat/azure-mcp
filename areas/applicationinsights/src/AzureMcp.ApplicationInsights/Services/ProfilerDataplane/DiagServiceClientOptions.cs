using System;
using Azure.Core;

namespace ServiceProfiler.DataPlane.Client;

/// <summary>
/// Options for configuring the Diagnostic Services Data Plane Client.
/// </summary>
public class DiagServiceClientOptions : ClientOptions
{
    public DiagServiceClientOptions() { }

    public static DiagServiceClientOptions Create(DiagServiceClientOptions defaults, string userAgent)
    {
        if (defaults is null)
        {
            throw new ArgumentNullException(nameof(defaults));
        }

        if (string.IsNullOrEmpty(userAgent))
        {
            throw new ArgumentException($"'{nameof(userAgent)}' cannot be null or empty.", nameof(userAgent));
        }

        return new DiagServiceClientOptions
        {
            Endpoint = defaults.Endpoint,
            Scope = defaults.Scope,
            UserAgent = userAgent
        };
    }

    public static readonly DiagServiceClientOptions Production = new()
    {
        Endpoint = new Uri("https://dataplane.diagnosticservices.azure.com/"),
        Scope = "api://dataplane.diagnosticservices.azure.com/.default"
    };

    public static readonly DiagServiceClientOptions Test = new()
    {
        Endpoint = new Uri("https://dataplane.test.diagnosticservices.azure.com/"),
        Scope = "api://dataplane.diagnosticservices-test.azure.com/.default"
    };

    /// <summary>
    /// Gets or sets the user agent for the client. This is used to identify the client in telemetry and logs.
    /// </summary>
    public string UserAgent { get; set; } = null!;

    /// <summary>
    /// The endpoint for the Diagnostic Services Data Plane.
    /// </summary>
    public Uri Endpoint { get; set; } = null!;

    /// <summary>
    /// The scope for the Diagnostic Services Data Plane API.
    /// </summary>
    public string Scope { get; set; } = null!;
}
