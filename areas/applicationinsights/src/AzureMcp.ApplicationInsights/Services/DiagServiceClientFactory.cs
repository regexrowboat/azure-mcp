using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using ServiceProfiler.DataPlane.Client;

namespace AzureMcp.ApplicationInsights.Services;

internal class DiagServiceClientFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DiagServiceClientFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public DiagServiceClient CreateClient(TokenCredential tokenCredential)
    {
        DiagServiceClientOptions options = DiagServiceClientOptions.Create(DiagServiceClientOptions.Production, userAgent: "AzureMCP");
        return ActivatorUtilities.CreateInstance<DiagServiceClient>(
            _serviceProvider,
            options,
            tokenCredential);
    }
}
