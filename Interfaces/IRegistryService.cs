// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.ServiceFabric.Services.Remoting;

    public interface IRegistryService : IService
    {
        /// <summary>
        /// Registers an entity as observable for a given topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task RegisterObservableAsync(string topic, EntityId entityId);

        /// <summary>
        /// Unregisters an entity as observable for a given topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task UnregisterObservableAsync(string topic, EntityId entityId);

        /// <summary>
        /// Used by an observable to send an heartbeat message to the registry.
        /// </summary>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task HearthbeatAsync(EntityId entityId);

        /// <summary>
        /// Returns an enumerable containing the observables for a given topic.
        /// </summary>
        /// <param name="topic">The topic</param>
        /// <param name="filterExpression">Specifies a filter expression.</param>
        /// <returns>An enumerable containing the observables for the topic contained in the call argument.</returns>
        Task<IEnumerable<EntityId>> QueryObservablesAsync(string topic, string filterExpression);
    }
}