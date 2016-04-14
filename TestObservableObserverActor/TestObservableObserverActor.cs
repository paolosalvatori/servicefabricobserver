// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverActor
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using global::Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using global::Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverActor.Interfaces;
    using global::Microsoft.ServiceFabric.Actors.Runtime;

    [ActorService(Name = "TestObservableObserverActor")]
    internal class TestObservableObserverActor : ObservableObserverActorBase, ITestObservableObserverActor
    {
        #region Public Constructor

        /// <summary>
        /// Initializes a new instance of the TestObservableObserverActor class.
        /// </summary>
        public TestObservableObserverActor()
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
                StringBuilder stringBuilder = new StringBuilder($"Observer successfully registered.\r\n[Observable]: {entityId}\r\n[Observer]: {args.EntityId}\r\n[Subscription]: Topic=[{args.Topic}]");
                int i = 1;
                foreach (string expression in args.FilterExpressions.Where(expression => !string.IsNullOrWhiteSpace(expression)))
                {
                    stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
                }
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
                ActorEventSource.Current.Message($"Observer successfully unregistered.\r\n[Observable]: {entityId}\r\n[Observer]: {args.EntityId}\r\n[Subscription]: Topic=[{args.Topic}]");
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
                ActorEventSource.Current.Message($"Message Received.\r\n[Observable]: {args.EntityId}\r\n[Observer]: {entityId}\r\n[Message]: Topic=[{args.Topic}] Body=[{args.Message?.Body ?? "NULL"}]");
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