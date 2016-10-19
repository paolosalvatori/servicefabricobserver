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

    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;

    #endregion

    public interface IServerObserverService : IEntityIdService
    {
        /// <summary>
        ///     Provides the observer with new data.
        ///     This method is invoked by an observable actor or service.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The current notification information.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <param name="observers">
        ///     A list of observers in the same cluster node. This field is optional.
        ///     When the list if not null or empty, the observer will forward the message to each observer which EntityId is in the
        ///     list.
        /// </param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task NotifyObserverAsync(string topic, Message message, EntityId entityId, IEnumerable<EntityId> observers);

        /// <summary>
        ///     Used by an observable to send an heartbeat message to an observer service.
        ///     This method is invoked by an observable actor or service.
        /// </summary>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task SendHeartbeatToObserverAsync(EntityId entityId);

        /// <summary>
        ///     Unregisters an observable actor or service.
        ///     This method is invoked by an observable actor or service.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <param name="observers">
        ///     A list of observers in the same cluster node. This field is optional.
        ///     When the list if not null or empty, the observer will forward the message to each observer which EntityId is in the
        ///     list.
        /// </param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task UnregisterObservableAsync(string topic, EntityId entityId, IEnumerable<EntityId> observers);
    }
}