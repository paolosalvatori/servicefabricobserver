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
    using System.Linq;
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

    public class ObservableActorController : ApiController
    {
        #region Private Static Methods

        private static IClientObservableActor GetActorProxy(ActorId actorId, Uri serviceUri)
        {
            if (actorId == null)
                throw new ArgumentException($"Parameter {nameof(actorId)} is null or invalid.", nameof(actorId));
            if (serviceUri == null)
                throw new ArgumentException($"Parameter {nameof(serviceUri)} is null or invalid.", nameof(serviceUri));
            return ActorProxy.Create<IClientObservableActor>(actorId, serviceUri);
        }

        #endregion

        #region Private Constants

        //************************************
        // Parameters
        //************************************

        #endregion

        #region Public Methods

        [HttpGet]
        [Route("api/observable/actor")]
        public string Test()
        {
            return "TEST OBSERVABLE ACTOR CONTROLLER";
        }

        /// <summary>
        ///     Registers an actor as observable for a given topic.
        ///     This method is called by a client component.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observable/actor/register")]
        public async Task RegisterObservableActorAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(request?.Topic) ||
                    (request.ObservableEntityId?.ServiceUri == null) ||
                    (request.ObservableEntityId.ActorId == null))
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));

                // Gets actor proxy
                IClientObservableActor proxy = GetActorProxy(
                    new ActorId(request.ObservableEntityId.ActorId),
                    request.ObservableEntityId.ServiceUri);
                if (proxy == null)
                    throw new ApplicationException("The ActorProxy cannot be null.");

                // Invokes actor using proxy
                ServiceEventSource.Current.Message(
                    $"Registering observable...\r\n[Observable]: {request.ObservableEntityId}\r\n[Publication]: Topic=[{request.Topic}]");
                await proxy.RegisterObservableActorAsync(request.Topic);
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
        ///     Unregisters an actor as observable for a given topic.
        ///     This method is called by a client component.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observable/actor/unregister")]
        public async Task UnregisterObservableActorAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(request?.Topic) ||
                    (request.ObservableEntityId?.ServiceUri == null) ||
                    (request.ObservableEntityId.ActorId == null))
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));

                // Gets actor proxy
                IClientObservableActor proxy = GetActorProxy(
                    new ActorId(request.ObservableEntityId.ActorId),
                    request.ObservableEntityId.ServiceUri);
                if (proxy == null)
                    throw new ApplicationException("The ActorProxy cannot be null.");

                // Invokes actor using proxy
                ServiceEventSource.Current.Message(
                    $"Unregistering observable...\r\n[Observable]: {request.ObservableEntityId}\r\n[Publication]: Topic=[{request.Topic}]");
                await proxy.UnregisterObservableActorAsync(request.Topic, request.UseObserverAsProxy);
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
        ///     Clear all observers and publications.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observable/actor/clear")]
        public async Task ClearObserversAndPublicationsAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if ((request.ObservableEntityId?.ServiceUri == null) ||
                    (request.ObservableEntityId.ActorId == null))
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));

                // Gets actor proxy
                IClientObservableActor proxy = GetActorProxy(
                    new ActorId(request.ObservableEntityId.ActorId),
                    request.ObservableEntityId.ServiceUri);
                if (proxy == null)
                    throw new ApplicationException("The ActorProxy cannot be null.");

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Clearing observers and publications...\r\n[Observable]: {request.ObservableEntityId}");
                await proxy.ClearObserversAndPublicationsAsync(request.UseObserverAsProxy);
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
        ///     Sends data to observers for a given topic.
        ///     This method is called by a client component.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        [HttpPost]
        [Route("api/observable/actor/notify")]
        public async Task NotifyObserversAsync(GatewayRequest request)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(request?.Topic) ||
                    (request.Messages == null) ||
                    !request.Messages.Any() ||
                    (request.ObservableEntityId?.ServiceUri == null) ||
                    (request.ObservableEntityId.ActorId == null))
                    throw new ArgumentException($"Parameter {nameof(request)} is null or invalid.", nameof(request));

                // Gets actor proxy
                IClientObservableActor proxy = GetActorProxy(
                    new ActorId(request.ObservableEntityId.ActorId),
                    request.ObservableEntityId.ServiceUri);
                if (proxy == null)
                    throw new ApplicationException("The ActorProxy cannot be null.");

                // Invokes actor using proxy
                foreach (Message message in request.Messages.Where(message => message != null))
                {
                    ServiceEventSource.Current.Message(
                        $"Notifying observers...\r\n[Observable]: {request.ObservableEntityId}\r\n[Message]: Topic=[{request.Topic}] Body=[{message.Body ?? "NULL"}]");
                    await proxy.NotifyObserversAsync(request.Topic, message, request.UseObserverAsProxy);
                }
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