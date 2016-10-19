#region Copyright

// //=======================================================================================
// // Microsoft Azure Customer Advisory Team  
// //
// // This sample is supplemental to the technical guidance published on the community
// // blog at http://blogs.msdn.com/b/paolos/. 
// // 
// // Author: Paolo Salvatori
// //=======================================================================================
// // Copyright © 2016 Microsoft Corporation. All rights reserved.
// // 
// // THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// // EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// // MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
// //=======================================================================================

#endregion

namespace Microsoft.AzureCat.Samples.ObserverPattern.Interfaces
{
    #region Using Directives

    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;

    #endregion

    public interface IClientObservableService : IEntityIdService
    {
        /// <summary>
        ///     Registers a service as observable for a given topic.
        ///     This method is called by a client component.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task RegisterObservableServiceAsync(string topic);

        /// <summary>
        ///     Unregisters a service as observable for a given topic.
        ///     This method is called by a client component.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="useObserverAsProxy">
        ///     Observable uses one observer for each cluster node as a proxy when true,
        ///     it directly sends the message to all observers otherwise.
        /// </param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task UnregisterObservableServiceAsync(string topic, bool useObserverAsProxy);

        /// <summary>
        ///     Clear all observers and publications.
        /// </summary>
        /// <param name="useObserverAsProxy">
        ///     Observable uses one observer for each cluster node as a proxy when true,
        ///     it directly sends the message to all observers otherwise.
        /// </param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task ClearObserversAndPublicationsAsync(bool useObserverAsProxy);

        /// <summary>
        ///     Sends data to observers for a given topic.
        ///     This method is called by a client component.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The current notification information.</param>
        /// <param name="useObserverAsProxy">
        ///     Observable uses one observer for each cluster node as a proxy when true,
        ///     it directly sends the message to all observers otherwise.
        /// </param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task NotifyObserversAsync(string topic, Message message, bool useObserverAsProxy);
    }
}