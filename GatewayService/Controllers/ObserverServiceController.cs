// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.GatewayService
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.AzureCat.Samples.ObserverPattern.Interfaces;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    public class ObserverServiceController : ApiController
    {
        #region Private Static Methods

        private static IClientObserverService GetServiceProxy(long? partitionKey, Uri serviceUri)
        {
            if (serviceUri == null)
            {
                throw new ArgumentException($"Parameter {nameof(serviceUri)} is null or invalid.", nameof(serviceUri));
            }
            return partitionKey.HasValue ?
                ServiceProxy.Create<IClientObserverService>(serviceUri, new ServicePartitionKey(partitionKey.Value)) :
                ServiceProxy.Create<IClientObserverService>(serviceUri);
        }

        #endregion

        #region Private Constants

        //************************************
        // Parameters
        //************************************

        #endregion

        #region Public Methods

        [HttpGet]
        [Route("api/observer/service")]
        public string Test()
        {
            return "TEST OBSERVER SERVICE CONTROLLER";
        }

        /// <summary>
        /// Registers an observer service. 
        /// This method is called by a client component.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observer/service/register")]
        public async Task RegisterObserverServiceAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(request?.Topic) ||
                    request.ObserverEntityId?.ServiceUri == null ||
                    request.ObservableEntityId == null)
                {
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));
                }

                // Gets service proxy
                IClientObserverService proxy = GetServiceProxy(request.ObserverEntityId.PartitionKey,
                                                               request.ObserverEntityId.ServiceUri);
                if (proxy == null)
                {
                    throw new ApplicationException("The ServiceProxy cannot be null.");
                }

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Registering observer...\r\n[Observable]: {request.ObservableEntityId}\r\n[Observer]: {request.ObserverEntityId}\r\n[Publication]: Topic=[{request.Topic}]");
                await proxy.RegisterObserverServiceAsync(request.Topic, request.FilterExpressions, new EntityId(request.ObservableEntityId));
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
        /// Unregisters an observer service.
        /// This method is called by a client component.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observer/service/unregister")]
        public async Task UnregisterObserverServiceAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(request?.Topic) ||
                    request.ObserverEntityId?.ServiceUri == null ||
                    request.ObservableEntityId == null)
                {
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));
                }

                // Gets service proxy
                IClientObserverService proxy = GetServiceProxy(request.ObserverEntityId.PartitionKey,
                                                               request.ObserverEntityId.ServiceUri);
                if (proxy == null)
                {
                    throw new ApplicationException("The ServiceProxy cannot be null.");
                }

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Unregistering observer...\r\n[Observable]: {request.ObservableEntityId}\r\n[Observer]: {request.ObserverEntityId}\r\n[Publication]: Topic=[{request.Topic}]");
                await proxy.UnregisterObserverServiceAsync(request.Topic, new EntityId(request.ObservableEntityId));
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
        /// Unregisters an observer service partition from all observables on all topics.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observer/service/clear")]
        public async Task ClearSubscriptionsAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (request.ObserverEntityId?.ServiceUri == null)
                {
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));
                }

                // Gets service proxy
                IClientObserverService proxy = GetServiceProxy(request.ObserverEntityId.PartitionKey,
                                                               request.ObserverEntityId.ServiceUri);
                if (proxy == null)
                {
                    throw new ApplicationException("The ServiceProxy cannot be null.");
                }

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Clearing observer subscriptions...\r\n[Observer]: {request.ObserverEntityId}");
                await proxy.ClearSubscriptionsAsync();
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
                    throw;
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Reads the messages for the observer actor from its messagebox.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observer/service/messages")]
        public async Task<IEnumerable<Message>> ReadMessagesFromMessageBoxAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (request.ObserverEntityId?.ServiceUri == null)
                {
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));
                }

                // Gets service proxy
                IClientObserverService proxy = GetServiceProxy(request.ObserverEntityId.PartitionKey,
                                                               request.ObserverEntityId.ServiceUri);
                if (proxy == null)
                {
                    throw new ApplicationException("The ServiceProxy cannot be null.");
                }

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Reading messages from the MessageBox...\r\n[Observer]: {request.ObserverEntityId}");
                return await proxy.ReadMessagesFromMessageBoxAsync();
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