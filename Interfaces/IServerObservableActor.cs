// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;

    public interface IServerObservableActor : IEntityIdActor
    {
        /// <summary>
        /// Registers an observer actor or service. 
        /// This method is invoked by an observer actor or service.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="filterExpressions">Specifies filter expressions.</param>
        /// <param name="entityId">The entity id of the observer.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task RegisterObserverAsync(string topic, IEnumerable<string> filterExpressions, EntityId entityId);

        /// <summary>
        /// Unregisters an observer actor or service. 
        /// This method is invoked by an observer actor or service.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id of the observer.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task UnregisterObserverAsync(string topic, EntityId entityId);
    }
}