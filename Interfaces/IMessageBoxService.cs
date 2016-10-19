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

    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.ServiceFabric.Services.Remoting;

    #endregion

    public interface IMessageBoxService : IService
    {
        /// <summary>
        ///     Read messages stored for an observer.
        /// </summary>
        /// <param name="uri">Observer uri.</param>
        /// <returns>An enumerable containing messages for the observer.</returns>
        Task<IEnumerable<Message>> ReadMessagesAsync(Uri uri);

        /// <summary>
        ///     Write messages for an observer.
        /// </summary>
        /// <param name="uri">Observer uri.</param>
        /// <param name="messages">A collection of messages.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task WriteMessagesAsync(Uri uri, IEnumerable<Message> messages);
    }
}