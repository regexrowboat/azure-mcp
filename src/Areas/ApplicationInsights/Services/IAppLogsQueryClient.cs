using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Monitor.Query;
using AzureMcp.Areas.ApplicationInsights.Models;

namespace AzureMcp.Areas.ApplicationInsights.Services
{
    public interface IAppLogsQueryClient
    {
        Task<IReadOnlyList<AppLogsQueryRow<T>>> QueryResourceAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(ResourceIdentifier resourceId, string kql, QueryTimeRange timeRange) where T : new();
    }
}
