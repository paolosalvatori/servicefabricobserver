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

namespace Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverActor
{
    #region Using Directives

    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverActor.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    #endregion

    [ActorService(Name = "TestObservableObserverActor")]
    internal class TestObservableObserverActor : ObservableObserverActorBase, ITestObservableObserverActor
    {
        #region Public Constructor

        /// <summary>
        ///     Initializes a new instance of the TestObservableObserverActor class.
        /// </summary>
        public TestObservableObserverActor(ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
            // Create Handlers for Observer Events
            this.NotificationMessageReceived += this.TestObservableObserverActor_NotificationMessageReceived;
            this.ObservableUnregistered += this.TestObservableObserverActor_ObservableUnregistered;

            // Create Handlers for Observable Events
            this.ObserverRegistered += this.TestObservableObserverActor_ObserverRegistered;
            this.ObserverUnregistered += this.TestObservableObserverActor_ObserverUnregistered;

            ActorEventSource.Current.Message("TestObservableObserverActor actor created.");
        }

        #endregion

        #region ITestObservableObserverActor methods

        public Task ExecuteCommandAsync(string command)
        {
            try
            {
                ActorEventSource.Current.Message($"Command Received.\r\n[Command]: [{command ?? "NULL"}]");
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
            return Task.FromResult(true);
        }

        #endregion

        #region Observable Event Handlers

        private async Task TestObservableObserverActor_ObserverRegistered(SubscriptionEventArgs args)
        {
            try
            {
                EntityId entityId = await this.GetEntityIdAsync();
                StringBuilder stringBuilder =
                    new StringBuilder(
                        $"Observer successfully registered.\r\n[Observable]: {entityId}\r\n[Observer]: {args.EntityId}\r\n[Subscription]: Topic=[{args.Topic}]");
                int i = 1;
                foreach (string expression in args.FilterExpressions.Where(expression => !string.IsNullOrWhiteSpace(expression)))
                    stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
                ActorEventSource.Current.Message(stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
        }

        private async Task TestObservableObserverActor_ObserverUnregistered(SubscriptionEventArgs args)
        {
            try
            {
                EntityId entityId = await this.GetEntityIdAsync();
                ActorEventSource.Current.Message(
                    $"Observer successfully unregistered.\r\n[Observable]: {entityId}\r\n[Observer]: {args.EntityId}\r\n[Subscription]: Topic=[{args.Topic}]");
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
        }

        #endregion

        #region Observer Event Handlers

        private async Task TestObservableObserverActor_NotificationMessageReceived(NotificationEventArgs<Message> args)
        {
            try
            {
                EntityId entityId = await this.GetEntityIdAsync();
                ActorEventSource.Current.Message(
                    $"Message Received.\r\n[Observable]: {args.EntityId}\r\n[Observer]: {entityId}\r\n[Message]: Topic=[{args.Topic}] Body=[{args.Message?.Body ?? "NULL"}]");
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
        }

        private async Task TestObservableObserverActor_ObservableUnregistered(SubscriptionEventArgs args)
        {
            try
            {
                EntityId entityId = await this.GetEntityIdAsync();
                ActorEventSource.Current.Message($"Observable successfully unregistered.\r\n[Observable]: {args.EntityId}\r\n[Observer]: {entityId}");
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
        }

        #endregion
    }
}