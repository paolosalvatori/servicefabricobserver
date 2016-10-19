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
    using Microsoft.ServiceFabric.Services.Remoting;

    #endregion

    public interface IRegistryService : IService
    {
        /// <summary>
        ///     Registers an entity as observable for a given topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task RegisterObservableAsync(string topic, EntityId entityId);

        /// <summary>
        ///     Unregisters an entity as observable for a given topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task UnregisterObservableAsync(string topic, EntityId entityId);

        /// <summary>
        ///     Used by an observable to send an heartbeat message to the registry.
        /// </summary>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task HearthbeatAsync(EntityId entityId);

        /// <summary>
        ///     Returns an enumerable containing the observables for a given topic.
        /// </summary>
        /// <param name="topic">The topic</param>
        /// <param name="filterExpression">Specifies a filter expression.</param>
        /// <returns>An enumerable containing the observables for the topic contained in the call argument.</returns>
        Task<IEnumerable<EntityId>> QueryObservablesAsync(string topic, string filterExpression);
    }
}