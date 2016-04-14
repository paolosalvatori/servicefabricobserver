// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;

    public interface IServerObserverService : IEntityIdService
    {
        /// <summary>
        /// Provides the observer with new data. 
        /// This method is invoked by an observable actor or service.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The current notification information.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <param name="observers">A list of observers in the same cluster node. This field is optional.
        /// When the list if not null or empty, the observer will forward the message to each observer which EntityId is in the list.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task NotifyObserverAsync(string topic, Message message, EntityId entityId, IEnumerable<EntityId> observers);

        /// <summary>
        /// Used by an observable to send an heartbeat message to an observer service. 
        /// This method is invoked by an observable actor or service.
        /// </summary>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task SendHeartbeatToObserverAsync(EntityId entityId);

        /// <summary>
        /// Unregisters an observable actor or service.
        /// This method is invoked by an observable actor or service.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <param name="observers">A list of observers in the same cluster node. This field is optional.
        /// When the list if not null or empty, the observer will forward the message to each observer which EntityId is in the list.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task UnregisterObservableAsync(string topic, EntityId entityId, IEnumerable<EntityId> observers);
    }
}