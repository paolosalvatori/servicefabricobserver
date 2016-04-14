// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.ServiceFabric.Services.Remoting;

    public interface IMessageBoxService : IService
    {
        /// <summary>
        /// Read messages stored for an observer.
        /// </summary>
        /// <param name="uri">Observer uri.</param>
        /// <returns>An enumerable containing messages for the observer.</returns>
        Task<IEnumerable<Message>> ReadMessagesAsync(Uri uri);

        /// <summary>
        /// Write messages for an observer.
        /// </summary>
        /// <param name="uri">Observer uri.</param>
        /// <param name="messages">A collection of messages.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task WriteMessagesAsync(Uri uri, IEnumerable<Message> messages);
    }
}