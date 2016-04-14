// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverService
{
    using System;
    using System.Fabric;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class TestObservableObserverService : ObservableObserverServiceBase
    {
        #region Public Constructor

        /// <summary>
        /// Initializes a new instance of the TestObservableObserverService class.
        /// </summary>
        public TestObservableObserverService(StatefulServiceContext context)
            : base(context)
        {
            try
            {
                // Create Handlers for Observer Events
                this.NotificationMessageReceived += this.TestObservableObserverService_NotificationMessageReceived;
                this.ObservableUnregistered += this.TestObservableObserverService_ObservableUnregistered;

                // Create Handlers for Observable Events
                this.ObserverRegistered += this.TestObservableObserverService_ObserverRegistered;
                this.ObserverUnregistered += this.TestObservableObserverService_ObserverUnregistered;
                ServiceEventSource.Current.Message("TestObservableObserverService instance created.");
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
                throw;
            }
        }

        #endregion

        #region Observable Event Handlers

        private async Task TestObservableObserverService_ObserverRegistered(SubscriptionEventArgs args)
        {
            try
            {
                EntityId id = await this.GetEntityIdAsync();
                StringBuilder stringBuilder =
                    new StringBuilder(
                        $"Observer successfully registered.\r\n[Observable]: {id}\r\n[Observer]: {args.EntityId}\r\n[Subscription]: Topic=[{args.Topic}]");
                int i = 1;
                foreach (string expression in args.FilterExpressions.Where(expression => !string.IsNullOrWhiteSpace(expression)))
                {
                    stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
                }
                ServiceEventSource.Current.Message(stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
                throw;
            }
        }

        private async Task TestObservableObserverService_ObserverUnregistered(SubscriptionEventArgs args)
        {
            try
            {
                EntityId id = await this.GetEntityIdAsync();
                ServiceEventSource.Current.Message(
                    $"Observer successfully unregistered.\r\n[Observable]: {id}\r\n[Observer]: {args.EntityId}\r\n[Subscription]: Topic=[{args.Topic}]");
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
                throw;
            }
        }

        #endregion

        #region Observer Event Handlers

        private async Task TestObservableObserverService_NotificationMessageReceived(NotificationEventArgs<Message> args)
        {
            try
            {
                EntityId id = await this.GetEntityIdAsync();
                ServiceEventSource.Current.Message(
                    $"Message Received.\r\n[Observable]: {args.EntityId}\r\n[Observer]: {id}\r\n[Message]: Topic=[{args.Topic}] Body=[{args.Message?.Body ?? "NULL"}]");
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
                throw;
            }
        }

        private async Task TestObservableObserverService_ObservableUnregistered(SubscriptionEventArgs args)
        {
            try
            {
                EntityId id = await this.GetEntityIdAsync();
                ServiceEventSource.Current.Message($"Observable successfully unregistered.\r\n[Observable]: {args.EntityId}\r\n[Observer]: {id}");
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
                throw;
            }
        }

        #endregion
    }
}