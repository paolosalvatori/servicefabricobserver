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

    public class RegistryServiceController : ApiController
    {
        #region Private Static Methods

        private static IRegistryService GetServiceProxy(long? partitionKey, Uri serviceUri)
        {
            if (serviceUri == null)
            {
                throw new ArgumentException($"Parameter {nameof(serviceUri)} is null or invalid.", nameof(serviceUri));
            }
            return partitionKey.HasValue ?
                ServiceProxy.Create<IRegistryService>(serviceUri, new ServicePartitionKey(partitionKey.Value)) :
                ServiceProxy.Create<IRegistryService>(serviceUri);
        }

        #endregion

        #region Private Constants

        //************************************
        // Parameters
        //************************************

        #endregion

        #region Public Methods

        [HttpGet]
        [Route("api/registry/service")]
        public string Test()
        {
            return "TEST REGISTRY SERVICE CONTROLLER";
        }

        /// <summary>
        /// Registers an entity as observable for a given topic.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/registry/service/register")]
        public async Task RegisterObservableAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(request?.Topic) ||
                    request.ObservableEntityId == null)
                {
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));
                }

                // Gets service proxy
                IRegistryService proxy = GetServiceProxy(PartitionResolver.Resolve(request.Topic, OwinCommunicationListener.RegistryServicePartitionCount),
                                                         OwinCommunicationListener.RegistryServiceUri);
                if (proxy == null)
                {
                    throw new ApplicationException("The ServiceProxy cannot be null.");
                }

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Registering observable...\r\n[Observable]: {request.ObservableEntityId}\r\n[Observer]: {request.ObserverEntityId}\r\n[Publication]: Topic=[{request.Topic}]");
                await proxy.RegisterObservableAsync(request.Topic, new EntityId(request.ObservableEntityId));
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
        /// Unregisters an entity as observable for a given topic.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/registry/service/unregister")]
        public async Task UnregisterRegistryServiceAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(request?.Topic) ||
                    request.ObservableEntityId == null)
                {
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));
                }

                // Gets service proxy
                IRegistryService proxy = GetServiceProxy(PartitionResolver.Resolve(request.Topic, OwinCommunicationListener.RegistryServicePartitionCount),
                                                         OwinCommunicationListener.RegistryServiceUri);
                if (proxy == null)
                {
                    throw new ApplicationException("The ServiceProxy cannot be null.");
                }

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Unregistering observable...\r\n[Observable]: {request.ObservableEntityId}\r\n[Observer]: {request.ObserverEntityId}\r\n[Publication]: Topic=[{request.Topic}]");
                await proxy.UnregisterObservableAsync(request.Topic, new EntityId(request.ObservableEntityId));
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
        /// Returns an enumerable containing the observables for a given topic.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/registry/service/get")]
        public async Task<IEnumerable<ShortEntityId>> QueryObservablesAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(request?.Topic))
                {
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));
                }

                // Gets service proxy
                IRegistryService proxy = GetServiceProxy(PartitionResolver.Resolve(request.Topic, OwinCommunicationListener.RegistryServicePartitionCount),
                                                         OwinCommunicationListener.RegistryServiceUri);
                if (proxy == null)
                {
                    throw new ApplicationException("The ServiceProxy cannot be null.");
                }

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Retrieving observables...\r\n[Publication]: Topic=[{request.Topic}]");
                IEnumerable<EntityId> entityIds = await proxy.QueryObservablesAsync(request.Topic, 
                                                                                             request.FilterExpressions != null && request.FilterExpressions.Any() ? 
                                                                                             request.FilterExpressions.First() : 
                                                                                             null);
                if (entityIds == null || !entityIds.Any())
                {
                    return null;
                }
                return entityIds.Select(e => new ShortEntityId(e));
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