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

// ReSharper disable once CheckNamespace
namespace Microsoft.AzureCat.Samples.ObserverPattern.GatewayService
{
    #region Using Directives

    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.AzureCat.Samples.ObserverPattern.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;

    #endregion

    public class ObserverActorController : ApiController
    {
        #region Private Static Methods

        private static IClientObserverActor GetActorProxy(ActorId actorId, Uri serviceUri)
        {
            if (actorId == null)
                throw new ArgumentException($"Parameter {nameof(actorId)} is null or invalid.", nameof(actorId));
            if (serviceUri == null)
                throw new ArgumentException($"Parameter {nameof(serviceUri)} is null or invalid.", nameof(serviceUri));
            return ActorProxy.Create<IClientObserverActor>(actorId, serviceUri);
        }

        #endregion

        #region Private Constants

        //************************************
        // Parameters
        //************************************

        #endregion

        #region Public Methods

        [HttpGet]
        [Route("api/observer/actor")]
        public string Test()
        {
            return "TEST OBSERVER ACTOR CONTROLLER";
        }

        /// <summary>
        ///     Registers an observer actor.
        ///     This method is called by a client component.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observer/actor/register")]
        public async Task RegisterObserverActorAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(request?.Topic) ||
                    (request.ObserverEntityId?.ServiceUri == null) ||
                    (request.ObserverEntityId.ActorId == null) ||
                    (request.ObservableEntityId == null))
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));

                // Gets actor proxy
                IClientObserverActor proxy = GetActorProxy(
                    new ActorId(request.ObserverEntityId.ActorId),
                    request.ObserverEntityId.ServiceUri);
                if (proxy == null)
                    throw new ApplicationException("The ActorProxy cannot be null.");

                // Invokes actor using proxy
                ServiceEventSource.Current.Message(
                    $"Registering observer...\r\n[Observable]: {request.ObservableEntityId}\r\n[Observer]: {request.ObserverEntityId}\r\n[Publication]: Topic=[{request.Topic}]");
                await proxy.RegisterObserverActorAsync(request.Topic, request.FilterExpressions, new EntityId(request.ObservableEntityId));
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                    return;
                foreach (Exception exception in ex.InnerExceptions)
                    ServiceEventSource.Current.Message(exception.Message);
                throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
                throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
        }

        /// <summary>
        ///     Unregisters an observer actor.
        ///     This method is called by a client component.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observer/actor/unregister")]
        public async Task UnregisterObserverActorAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(request?.Topic) ||
                    (request.ObserverEntityId?.ServiceUri == null) ||
                    (request.ObserverEntityId.ActorId == null) ||
                    (request.ObservableEntityId == null))
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));

                // Gets actor proxy
                IClientObserverActor proxy = GetActorProxy(
                    new ActorId(request.ObserverEntityId.ActorId),
                    request.ObserverEntityId.ServiceUri);
                if (proxy == null)
                    throw new ApplicationException("The ActorProxy cannot be null.");

                // Invokes actor using proxy
                ServiceEventSource.Current.Message(
                    $"Unregistering observer...\r\n[Observable]: {request.ObservableEntityId}\r\n[Observer]: {request.ObserverEntityId}\r\n[Publication]: Topic=[{request.Topic}]");
                await proxy.UnregisterObserverActorAsync(request.Topic, new EntityId(request.ObservableEntityId));
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                    throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
                foreach (Exception exception in ex.InnerExceptions)
                    ServiceEventSource.Current.Message(exception.Message);
                throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
                throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
        }

        /// <summary>
        ///     Unregisters an observer actor from all observables on all topics.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observer/actor/clear")]
        public async Task ClearSubscriptionsAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if ((request.ObserverEntityId?.ServiceUri == null) ||
                    (request.ObserverEntityId.ActorId == null))
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));

                // Gets actor proxy
                IClientObserverActor proxy = GetActorProxy(
                    new ActorId(request.ObserverEntityId.ActorId),
                    request.ObserverEntityId.ServiceUri);
                if (proxy == null)
                    throw new ApplicationException("The ActorProxy cannot be null.");

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Clearing observer subscriptions...\r\n[Observer]: {request.ObserverEntityId}");
                await proxy.ClearSubscriptionsAsync();
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                    throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
                foreach (Exception exception in ex.InnerExceptions)
                    ServiceEventSource.Current.Message(exception.Message);
                throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
                throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
        }

        /// <summary>
        ///     Reads the messages for the observer actor from its messagebox.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observer/actor/messages")]
        public async Task<IEnumerable<Message>> ReadMessagesFromMessageBoxAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if ((request.ObserverEntityId?.ServiceUri == null) ||
                    (request.ObserverEntityId.ActorId == null))
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));

                // Gets actor proxy
                IClientObserverActor proxy = GetActorProxy(
                    new ActorId(request.ObserverEntityId.ActorId),
                    request.ObserverEntityId.ServiceUri);
                if (proxy == null)
                    throw new ApplicationException("The ActorProxy cannot be null.");

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Reading messages from the MessageBox...\r\n[Observer]: {request.ObserverEntityId}");
                return await proxy.ReadMessagesFromMessageBoxAsync();
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                    throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
                foreach (Exception exception in ex.InnerExceptions)
                    ServiceEventSource.Current.Message(exception.Message);
                throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
                throw new HttpResponseException(this.Request.CreateErrorResponse(HttpStatusCode.NotFound, ex.Message));
            }
        }

        #endregion
    }
}