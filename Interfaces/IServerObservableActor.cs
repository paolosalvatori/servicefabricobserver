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

    public interface IServerObservableActor : IEntityIdActor
    {
        /// <summary>
        ///     Registers an observer actor or service.
        ///     This method is invoked by an observer actor or service.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="filterExpressions">Specifies filter expressions.</param>
        /// <param name="entityId">The entity id of the observer.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task RegisterObserverAsync(string topic, IEnumerable<string> filterExpressions, EntityId entityId);

        /// <summary>
        ///     Unregisters an observer actor or service.
        ///     This method is invoked by an observer actor or service.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id of the observer.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task UnregisterObserverAsync(string topic, EntityId entityId);
    }
}