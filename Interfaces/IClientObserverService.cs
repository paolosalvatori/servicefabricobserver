// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;

    public interface IClientObserverService : IEntityIdService
    {
        /// <summary>
        /// Registers an observer service. 
        /// This method is called by a client component.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="filterExpressions">Specifies a collection of filter expressions.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task RegisterObserverServiceAsync(string topic, IEnumerable<string> filterExpressions, EntityId entityId);

        /// <summary>
        /// Unregisters an observer service.
        /// This method is called by a client component.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task UnregisterObserverServiceAsync(string topic, EntityId entityId);

        /// <summary>
        /// Unregisters an observer service partition from all observables on all topics.
        /// </summary>
        /// <returns>The asynchronous result of the operation.</returns>
        Task ClearSubscriptionsAsync();

        /// <summary>
        /// Reads the messages for the observer actor from its messagebox.
        /// </summary>
        /// <returns>The messages for the current observer actor.</returns>
        Task<IEnumerable<Message>> ReadMessagesFromMessageBoxAsync();
    }
}