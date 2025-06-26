// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureMcp.Areas.Monitor.Options.App
{
    public interface IAppOptions
    {
        /// <summary>
        /// The resource name
        /// </summary>
        string? ResourceName { get; set; }

        /// <summary>
        /// The resource id of the Application Insights resource.
        /// </summary>
        string? ResourceId { get; set; }

        /// <summary>
        /// The intent for using the tool.
        /// </summary>
        string? Intent { get; set; }
    }
}
