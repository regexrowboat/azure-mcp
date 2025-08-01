// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.AI.Projects;
using AzureMcp.Core.Options;
using AzureMcp.Foundry.Models;

namespace AzureMcp.Foundry.Services;

public interface IFoundryService
{
    Task<List<ModelInformation>> ListModels(
        bool searchForFreePlayground = false,
        string publisherName = "",
        string licenseName = "",
        string modelName = "",
        int maxPages = 3,
        RetryPolicyOptions? retryPolicy = null
    );

    Task<List<Deployment>> ListDeployments(
        string endpoint,
        string? tenantId = null,
        RetryPolicyOptions? retryPolicy = null
    );

    Task<ModelDeploymentResult> DeployModel(string deploymentName,
        string modelName,
        string modelFormat,
        string azureAiServicesName,
        string resourceGroup,
        string subscriptionId,
        string? modelVersion = null,
        string? modelSource = null,
        string? skuName = null,
        int? skuCapacity = null,
        string? scaleType = null,
        int? scaleCapacity = null,
        RetryPolicyOptions? retryPolicy = null
    );
}
