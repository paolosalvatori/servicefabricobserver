// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.ServiceFabric.Services.Remoting;

    public interface IEntityIdService : IService
    {
        /// <summary>
        /// Gets the EntityId.
        /// </summary>
        /// <returns>The service entity id.</returns>
        Task<EntityId> GetEntityIdAsync();
    }
}