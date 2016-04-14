// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.ServiceFabric.Services.Remoting;

    public interface IClientObservableService : IEntityIdService
    {
        /// <summary>
        /// Registers a service as observable for a given topic. 
        /// This method is called by a client component.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task RegisterObservableServiceAsync(string topic);

        /// <summary>
        /// Unregisters a service as observable for a given topic. 
        /// This method is called by a client component.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="useObserverAsProxy">Observable uses one observer for each cluster node as a proxy when true, 
        /// it directly sends the message to all observers otherwise.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task UnregisterObservableServiceAsync(string topic, bool useObserverAsProxy);

        /// <summary>
        /// Clear all observers and publications.
        /// </summary>
        /// <param name="useObserverAsProxy">Observable uses one observer for each cluster node as a proxy when true, 
        /// it directly sends the message to all observers otherwise.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task ClearObserversAndPublicationsAsync(bool useObserverAsProxy);

        /// <summary>
        /// Sends data to observers for a given topic. 
        /// This method is called by a client component.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The current notification information.</param>
        /// <param name="useObserverAsProxy">Observable uses one observer for each cluster node as a proxy when true, 
        /// it directly sends the message to all observers otherwise.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task NotifyObserversAsync(string topic, Message message, bool useObserverAsProxy);
    }
}