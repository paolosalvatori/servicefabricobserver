// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.GatewayService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.AzureCat.Samples.ObserverPattern.Interfaces;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    public class MessageBoxServiceController : ApiController
    {
        #region Private Static Methods

        private static IMessageBoxService GetServiceProxy(long? partitionKey, Uri serviceUri)
        {
            if (serviceUri == null)
            {
                throw new ArgumentException($"Parameter {nameof(serviceUri)} is null or invalid.", nameof(serviceUri));
            }
            return partitionKey.HasValue ?
                ServiceProxy.Create<IMessageBoxService>(serviceUri, new ServicePartitionKey(partitionKey.Value)) :
                ServiceProxy.Create<IMessageBoxService>(serviceUri);
        }

        #endregion

        #region Private Constants

        //************************************
        // Parameters
        //************************************

        #endregion

        #region Public Methods

        [HttpGet]
        [Route("api/messagebox/service")]
        public string Test()
        {
            return "TEST REGISTRY SERVICE CONTROLLER";
        }

        /// <summary>
        /// Read messages stored for an observer.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/messagebox/service/read")]
        public async Task<IEnumerable<Message>> ReadMessagesAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (request.ObserverEntityId == null)
                {
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));
                }

                // Gets service proxy
                IMessageBoxService proxy = GetServiceProxy(PartitionResolver.Resolve(request.ObserverEntityId.EntityUri.AbsoluteUri,
                                                                                     OwinCommunicationListener.MessageBoxServicePartitionCount),
                                                           OwinCommunicationListener.MessageBoxServiceUri);
                if (proxy == null)
                {
                    throw new ApplicationException("The ServiceProxy cannot be null.");
                }

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Reading messages from the MessageBox...\r\n[Observer]: {request.ObserverEntityId}");
                return await proxy.ReadMessagesAsync(request.ObserverEntityId.EntityUri);
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                {
                    throw;
                }
                foreach (Exception exception in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Message(exception.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Write messages for an observer.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/messagebox/service/write")]
        public async Task WriteMessagesAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (request.ObserverEntityId == null ||
                    request.Messages == null ||
                    !request.Messages.Any())
                {
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));
                }

                // Gets service proxy
                IMessageBoxService proxy = GetServiceProxy(PartitionResolver.Resolve(request.ObserverEntityId.EntityUri.AbsoluteUri,
                                                                                     OwinCommunicationListener.MessageBoxServicePartitionCount),
                                                           OwinCommunicationListener.MessageBoxServiceUri);
                if (proxy == null)
                {
                    throw new ApplicationException("The ServiceProxy cannot be null.");
                }

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Writing messages to the MessageBox...\r\n[Observer]: {request.ObserverEntityId}");
                await proxy.WriteMessagesAsync(request.ObserverEntityId.EntityUri, request.Messages);
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                {
                    throw;
                }
                foreach (Exception exception in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Message(exception.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
                throw;
            }
        }
        #endregion
    }
}