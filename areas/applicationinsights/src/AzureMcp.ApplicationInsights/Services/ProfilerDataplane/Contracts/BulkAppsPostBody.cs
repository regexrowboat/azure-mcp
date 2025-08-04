// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.ServiceProfiler.DataPlane.Contracts
{
    /// <summary>
    /// A contract between the dataplane and the client to post for BulkApps.
    /// </summary>
    public class BulkAppsPostBody
    {
        /// <summary>
        /// List of apps.
        /// </summary>
        public IEnumerable<Guid> Apps { get; init; } = default!;
    }
}
