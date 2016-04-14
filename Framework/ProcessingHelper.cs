// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Interfaces;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    public static class ProcessingHelper
    {
        #region Public Static Methods

        public static async Task UnregisterObservableAsync(string topic, EntityId observerEntityId, EntityId observableEntityId, List<EntityId> observerList)
        {
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    if (observerEntityId.Kind == EntityKind.Actor)
                    {
                        IServerObserverActor actorProxy = ActorProxy.Create<IServerObserverActor>(observerEntityId.ActorId, observerEntityId.ServiceUri);
                        await actorProxy.UnregisterObservableAsync(topic, observableEntityId, observerList);
                    }
                    else
                    {
                        IServerObserverService serviceProxy = observerEntityId.PartitionKey.HasValue
                            ? ServiceProxy.Create<IServerObserverService>(observerEntityId.ServiceUri, new ServicePartitionKey(observerEntityId.PartitionKey.Value))
                            : ServiceProxy.Create<IServerObserverService>(observerEntityId.ServiceUri);
                        await serviceProxy.UnregisterObservableAsync(topic, observableEntityId, observerList);
                    }
                    return;
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                catch (AggregateException ex)
                {
                    foreach (Exception innerException in ex.InnerExceptions)
                    {
                        ActorEventSource.Current.Error(innerException);
                    }
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
        }

        public static async Task NotifyObserverAsync(
            string topic, Message message, EntityId observerEntityId, EntityId observableEntityId, List<EntityId> observerList)
        {
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    if (observerEntityId.Kind == EntityKind.Actor)
                    {
                        IServerObserverActor actorProxy = ActorProxy.Create<IServerObserverActor>(observerEntityId.ActorId, observerEntityId.ServiceUri);
                        await actorProxy.NotifyObserverAsync(topic, message, observableEntityId, observerList);
                    }
                    else
                    {
                        IServerObserverService serviceProxy = observerEntityId.PartitionKey.HasValue
                            ? ServiceProxy.Create<IServerObserverService>(observerEntityId.ServiceUri, new ServicePartitionKey(observerEntityId.PartitionKey.Value))
                            : ServiceProxy.Create<IServerObserverService>(observerEntityId.ServiceUri);
                        await serviceProxy.NotifyObserverAsync(topic, message, observableEntityId, observerList);
                    }
                    return;
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception innerException in ex.InnerExceptions)
                    {
                        ActorEventSource.Current.Error(innerException);
                    }
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.Error(ex);
                }
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
            WriteMessageToMessageBoxAsync(observerEntityId.EntityUri, message).Wait();
            if (observerList != null && observerList.Any())
            {
                Tuple<EntityId, List<EntityId>> tuple = GetObserverProxyAndList(observerList, true);
                await NotifyObserverAsync(topic, message, tuple.Item1, observableEntityId, tuple.Item2);
            }
        }

        public static async Task WriteMessageToMessageBoxAsync(Uri uri, Message message)
        {
            if (uri == null)
            {
                throw new ArgumentException($"The {nameof(uri)} parameter cannot be null.", nameof(uri));
            }
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    IMessageBoxService messageBoxService =
                        ServiceProxy.Create<IMessageBoxService>(ConfigurationHelper.MessageBoxServiceUri, 
                                                                new ServicePartitionKey(PartitionResolver.Resolve(uri.AbsoluteUri, ConfigurationHelper.MessageBoxServicePartitionCount)));
                    await messageBoxService.WriteMessagesAsync(uri, new[] {message});
                    return;
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                catch (AggregateException ex)
                {
                    foreach (Exception innerException in ex.InnerExceptions)
                    {
                        ActorEventSource.Current.Error(innerException);
                    }
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
        }

        public static async Task<IEnumerable<Message>> ReadMessagesFromMessageBoxAsync(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentException($"The {nameof(uri)} parameter cannot be null.", nameof(uri));
            }
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    IMessageBoxService messageBoxService =
                        ServiceProxy.Create<IMessageBoxService>(ConfigurationHelper.MessageBoxServiceUri,
                                                                new ServicePartitionKey(PartitionResolver.Resolve(uri.AbsoluteUri, ConfigurationHelper.MessageBoxServicePartitionCount)));
                    return await messageBoxService.ReadMessagesAsync(uri);
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception innerException in ex.InnerExceptions)
                    {
                        ActorEventSource.Current.Error(innerException);
                    }
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.Error(ex);
                }
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
            throw new TimeoutException(Constants.RetryTimeoutExhausted);
        }

        public static async Task ExecuteEventHandlerAsync(Func<SubscriptionEventArgs, Task> func, SubscriptionEventArgs eventArgs)
        {
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    await func(eventArgs);
                    return;
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                catch (AggregateException ex)
                {
                    foreach (Exception innerException in ex.InnerExceptions)
                    {
                        ActorEventSource.Current.Error(innerException);
                    }
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
        }

        public static async Task ExecuteEventHandlerAsync(Func<NotificationEventArgs<Message>, Task> func, NotificationEventArgs<Message> eventArgs)
        {
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    await func(eventArgs);
                    return;
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                catch (AggregateException ex)
                {
                    foreach (Exception innerException in ex.InnerExceptions)
                    {
                        ActorEventSource.Current.Error(innerException);
                    }
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
        }

        public static Tuple<EntityId, List<EntityId>> GetObserverProxyAndList(List<EntityId> list, bool selectRandomObserverAsProxy)

        {
            if (list == null || !list.Any())
            {
                return null;
            }
            if (selectRandomObserverAsProxy)
            {
                // Create random object
                Random random = new Random();

                // Get a random element as proxy
                int index = random.Next(0, list.Count);
                EntityId proxyEntityId = list[index];

                // Remove the EntityId of the proxy observer from the list
                list.RemoveAt(index);

                return new Tuple<EntityId, List<EntityId>>(proxyEntityId, list);
            }
            else
            {
                // The first observer in the list is used as a proxy to send the message 
                // to the remaining observers located on the same node.
                EntityId proxyEntityId = list.First();

                // Remove the EntityId of the proxy observer from the list
                list.RemoveAt(0);

                return new Tuple<EntityId, List<EntityId>>(proxyEntityId, list);
            }
        }

        public static Tuple<EntityId, List<EntityId>> GetObserverProxyAndList(IGrouping<string, EntityId> grouping, bool selectRandomObserverAsProxy)

        {
            if (grouping == null || !grouping.Any())
            {
                return null;
            }
            if (selectRandomObserverAsProxy)
            {
                // Create random object
                Random random = new Random();

                // Get the list of EntityId for the observers located on the same node
                List<EntityId> observerList = grouping.ToList();

                // Get a random element as proxy
                int index = random.Next(0, observerList.Count);
                EntityId proxyEntityId = observerList[index];

                // Remove the EntityId of the proxy observer from the list
                observerList.RemoveAt(index);

                return new Tuple<EntityId, List<EntityId>>(proxyEntityId, observerList);
            }
            else
            {
                // The first observer in the list is used as a proxy to send the message 
                // to the remaining observers located on the same node.
                EntityId proxyEntityId = grouping.First();

                // Get the list of EntityId for the observers located on the same node
                List<EntityId> observerList = grouping.ToList();

                // Remove the EntityId of the proxy observer from the list
                observerList.RemoveAt(0);

                return new Tuple<EntityId, List<EntityId>>(proxyEntityId, observerList);
            }
        }

        #endregion
    }
}