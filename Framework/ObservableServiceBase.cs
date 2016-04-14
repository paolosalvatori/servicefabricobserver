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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Interfaces;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Newtonsoft.Json.Linq;

    public abstract class ObservableServiceBase : StatefulService, IClientObservableService, IServerObservableService
    {
        #region Public Events

        public event Func<SubscriptionEventArgs, Task> ObserverRegistered;

        public event Func<SubscriptionEventArgs, Task> ObserverUnregistered;

        #endregion

        #region Protected Constructor

        protected ObservableServiceBase(StatefulServiceContext context)
            : base(context)
        {
            ConfigurationHelper.Initialize(this.Context);
        }

        #endregion

            #region StatefulService overriden methods

            /// <summary>
            /// Optional override to create listeners (like tcp, http) for this service replica.
            /// </summary>
            /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    ServiceEventSource.Current.Message(Constants.ServiceReplicaListenersCreated);
                    return new[]
                    {
                        new ServiceReplicaListener(this.CreateServiceRemotingListener)
                    };
                }
                catch (FabricTransientException ex)
                {
                    ServiceEventSource.Current.Error(ex);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception e in ex.InnerExceptions)
                    {
                        ServiceEventSource.Current.Error(e);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.Error(ex);
                    throw;
                }
                Task.Delay(ConfigurationHelper.BackoffQueryDelay).Wait();
            }
            throw new TimeoutException(Constants.RetryTimeoutExhausted);
        }

        /// <summary>
        /// Run a background processing task on the partition's primary replica.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. Implementers should take special care to honor the cancellationToken promptly, as delay in doing so may impact service availability.</param>
        /// <returns>A task that represents the background processing operation.</returns>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    EntityId entityId;
                    switch (this.Partition.PartitionInfo.Kind)
                    {
                        case ServicePartitionKind.Singleton:
                            entityId = new EntityId(this.Context.ServiceName);
                            break;
                        case ServicePartitionKind.Int64Range:
                            Int64RangePartitionInformation in64Range = (Int64RangePartitionInformation)this.Partition.PartitionInfo;
                            entityId = new EntityId(in64Range.LowKey, this.Context.ServiceName);
                            break;
                        default:
                            throw new Exception(Constants.NamedPartitionsNotSupported);
                    }
                    IReliableDictionary<string, EntityId> entityIdDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, EntityId>>(Constants.EntityIdDictionary);
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        await entityIdDictionary.AddOrUpdateAsync(transaction, Constants.EntityIdKey, entityId, (k, v) => v);
                        await transaction.CommitAsync();
                    }
                    ServiceEventSource.Current.Message($"{entityId} activated.");
                    break;
                }
                catch (FabricTransientException ex)
                {
                    ServiceEventSource.Current.Error(ex);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception e in ex.InnerExceptions)
                    {
                        ServiceEventSource.Current.Error(e);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.Error(ex);
                    throw;
                }
            }
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        #endregion

        #region IEntityIdService methods
        /// <summary>
        /// Gets the EntityId.
        /// </summary>
        /// <returns>The actor entity id.</returns>
        /// <summary>
        /// Gets the EntityId.
        /// </summary>
        /// <returns>The actor entity id.</returns>
        public async Task<EntityId> GetEntityIdAsync()
        {
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    EntityId entityId = null;
                    IReliableDictionary<string, EntityId> entityIdDictionary = this.StateManager.GetOrAddAsync<IReliableDictionary<string, EntityId>>(Constants.EntityIdDictionary).Result;
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<EntityId> result = await entityIdDictionary.TryGetValueAsync(transaction, Constants.EntityIdKey);
                        if (result.HasValue)
                        {
                            entityId = result.Value;
                        }
                        transaction.CommitAsync().Wait();
                    }
                    return entityId;
                }
                catch (FabricTransientException ex)
                {
                    ServiceEventSource.Current.Error(ex);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception e in ex.InnerExceptions)
                    {
                        ServiceEventSource.Current.Error(e);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.Error(ex);
                    throw;
                }
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
            throw new TimeoutException(Constants.RetryTimeoutExhausted);
        }
        #endregion

        #region IServerObservableService methods

        /// <summary>
        /// Registers an observer. This methods is invoked by an observer.
        /// </summary>
        /// <param name="filterExpressions">Specifies filter expressions.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id of the observer.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task RegisterObserverAsync(string topic, IEnumerable<string> filterExpressions, EntityId entityId)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException($"The {nameof(topic)} parameter cannot be null.", nameof(topic));
            }
            if (entityId == null)
            {
                throw new ArgumentException($"The {nameof(entityId)} parameter cannot be null.", nameof(entityId));
            }
            IList<string> enumerable = filterExpressions as IList<string> ?? filterExpressions.ToList();
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    EntityId id = await this.GetEntityIdAsync();
                    IReliableDictionary<string, Dictionary<Uri, ObserverInfo>> topicsDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<Uri, ObserverInfo>>>(Constants.TopicDictionary);
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<Dictionary<Uri, ObserverInfo>> result = await topicsDictionary.TryGetValueAsync(transaction, topic);
                        if (!result.HasValue)
                        {
                            throw new ArgumentException($"{id} is not an observable for Topic=[{topic}]");
                        }
                        await transaction.CommitAsync();
                    }
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<Dictionary<Uri, ObserverInfo>> result = await topicsDictionary.TryGetValueAsync(transaction, topic);
                        Dictionary<Uri, ObserverInfo> observers = result.HasValue ? result.Value : new Dictionary<Uri, ObserverInfo>();
                        if (!observers.ContainsKey(entityId.EntityUri))
                        {
                            observers.Add(entityId.EntityUri, new ObserverInfo(enumerable, entityId));
                            StringBuilder stringBuilder =
                                new StringBuilder(
                                    $"Observer successfully registered.\r\n[Observable]: {id}\r\n[Observer]: {entityId}\r\n[Subscription]: Topic=[{topic}]");
                            int i = 1;
                            foreach (string expression in enumerable.Where(expression => !string.IsNullOrWhiteSpace(expression)))
                            {
                                stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
                            }
                            ServiceEventSource.Current.Message(stringBuilder.ToString());
                        }
                        else
                        {
                            StringBuilder stringBuilder =
                                new StringBuilder(
                                    $"Observer already registered.\r\n[Observable]: {id}\r\n[Observer]: {entityId}\r\n[Subscription]: Topic=[{topic}]");
                            int i = 1;
                            foreach (string expression in enumerable.Where(expression => !string.IsNullOrWhiteSpace(expression)))
                            {
                                stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
                            }
                            ServiceEventSource.Current.Message(stringBuilder.ToString());
                        }
                        await topicsDictionary.AddOrUpdateAsync(transaction, topic, e => observers, (e, s) => observers);
                        await transaction.CommitAsync();
                    }
                    break;
                }
                catch (FabricTransientException ex)
                {
                    ServiceEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw new TimeoutException(Constants.RetryTimeoutExhausted);
                    }
                }
                catch (AggregateException ex)
                {
                    foreach (Exception e in ex.InnerExceptions)
                    {
                        ServiceEventSource.Current.Error(e);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.Error(ex);
                    throw;
                }
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
            if (this.ObserverRegistered == null)
            {
                return;
            }
            try
            {
                Delegate[] invocationList = this.ObserverRegistered.GetInvocationList();
                Task[] handlerTasks = new Task[invocationList.Length];
                SubscriptionEventArgs args = new SubscriptionEventArgs(topic, enumerable, entityId);
                for (int i = 0; i < invocationList.Length; i++)
                {
                    handlerTasks[i] = ProcessingHelper.ExecuteEventHandlerAsync((Func<SubscriptionEventArgs, Task>) invocationList[i], args);
                }
                await Task.WhenAll(handlerTasks);
            }
            catch (AggregateException ex)
            {
                foreach (Exception e in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(e);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
            }
        }

        /// <summary>
        /// Unregisters an observer. This methods is invoked by an observer.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id of the observer.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task UnregisterObserverAsync(string topic, EntityId entityId)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException($"The {nameof(topic)} parameter cannot be null.", nameof(topic));
            }
            if (entityId == null)
            {
                throw new ArgumentException($"The {nameof(entityId)} parameter cannot be null.", nameof(entityId));
            }
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    EntityId id = await this.GetEntityIdAsync();
                    IReliableDictionary<string, Dictionary<Uri, ObserverInfo>> topicsDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<Uri, ObserverInfo>>>(Constants.TopicDictionary);
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<Dictionary<Uri, ObserverInfo>> result = await topicsDictionary.TryGetValueAsync(transaction, topic);
                        if (!result.HasValue)
                        {
                            throw new ArgumentException($"{id} is not an observable for Topic=[{topic}]");
                        }
                        await transaction.CommitAsync();
                    }

                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<Dictionary<Uri, ObserverInfo>> result = await topicsDictionary.TryGetValueAsync(transaction, topic);
                        if (result.HasValue)
                        {
                            Dictionary<Uri, ObserverInfo> observers = result.Value;
                            if (observers.ContainsKey(entityId.EntityUri))
                            {
                                observers.Remove(entityId.EntityUri);
                                ServiceEventSource.Current.Message(
                                    $"Observer successfully unregistered.\r\n[Observable]: {id}\r\n[Observer]: {entityId}\r\n[Subscription]: Topic=[{topic}]");
                                await topicsDictionary.AddOrUpdateAsync(transaction, topic, e => observers, (e, s) => observers);
                            }
                        }
                        await transaction.CommitAsync();
                    }
                    break;
                }
                catch (FabricTransientException ex)
                {
                    ServiceEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw;
                    }
                }
                catch (AggregateException ex)
                {
                    foreach (Exception e in ex.InnerExceptions)
                    {
                        ServiceEventSource.Current.Error(e);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.Error(ex);
                    throw;
                }
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
            if (this.ObserverUnregistered == null)
            {
                return;
            }
            try
            {
                Delegate[] invocationList = this.ObserverUnregistered.GetInvocationList();
                Task[] handlerTasks = new Task[invocationList.Length];
                SubscriptionEventArgs args = new SubscriptionEventArgs(topic, entityId);
                for (int i = 0; i < invocationList.Length; i++)
                {
                    handlerTasks[i] = ProcessingHelper.ExecuteEventHandlerAsync((Func<SubscriptionEventArgs, Task>) invocationList[i], args);
                }
                await Task.WhenAll(handlerTasks);
            }
            catch (AggregateException ex)
            {
                foreach (Exception e in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(e);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
            }
        }

        /// <summary>
        /// Registers an entity as observable for a given topic. 
        /// This method is called by a management service or actor.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task RegisterObservableServiceAsync(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException($"The {nameof(topic)} parameter cannot be null.", nameof(topic));
            }
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    EntityId id = await this.GetEntityIdAsync();
                    IRegistryService registryService =
                        ServiceProxy.Create<IRegistryService>(ConfigurationHelper.RegistryServiceUri,
                                                              new ServicePartitionKey(PartitionResolver.Resolve(topic, ConfigurationHelper.RegistryServicePartitionCount)));
                    await registryService.RegisterObservableAsync(topic, id);
                    ServiceEventSource.Current.Message($"Observable successfully registered.\r\n[Observable]: {id}\r\n[Publication]: Topic=[{topic}].");
                    IReliableDictionary<string, Dictionary<Uri, ObserverInfo>> topicsDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<Uri, ObserverInfo>>>(Constants.TopicDictionary);
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        await topicsDictionary.AddOrUpdateAsync(transaction, topic, s => new Dictionary<Uri, ObserverInfo>(), (s, b) => b);
                        await transaction.CommitAsync();
                    }
                    return;
                }
                catch (FabricTransientException ex)
                {
                    ServiceEventSource.Current.Error(ex);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception e in ex.InnerExceptions)
                    {
                        ServiceEventSource.Current.Error(e);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.Error(ex);
                    throw;
                }
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
            throw new TimeoutException(Constants.RetryTimeoutExhausted);
        }

        /// <summary>
        /// Clear all observers for all topics.
        /// </summary>
        /// <param name="useObserverAsProxy">Observable uses one observer for each cluster node as a proxy when true, 
        /// it directly sends the message to all observers otherwise.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task ClearObserversAndPublicationsAsync(bool useObserverAsProxy)
        {
            try
            {
                IReliableDictionary<string, Dictionary<Uri, ObserverInfo>> topicsDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<Uri, ObserverInfo>>>(Constants.TopicDictionary);
                using (ITransaction transaction = this.StateManager.CreateTransaction())
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    IAsyncEnumerable<KeyValuePair<string, Dictionary<Uri, ObserverInfo>>> enumerable = await topicsDictionary.CreateEnumerableAsync(transaction);
                    using (IAsyncEnumerator<KeyValuePair<string, Dictionary<Uri, ObserverInfo>>> enumerator = enumerable.GetAsyncEnumerator())
                    {
                        List<Task> taskList = new List<Task>();
                        while (await enumerator.MoveNextAsync(cancellationTokenSource.Token).ConfigureAwait(false))
                        {
                            taskList.Add(this.UnregisterObservableServiceAsync(enumerator.Current.Key, useObserverAsProxy));
                        }
                        await Task.WhenAll(taskList);
                        await transaction.CommitAsync();
                    }
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception e in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(e);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
            }
        }
        #endregion

        #region IClientObservableService methods
        /// <summary>
        /// Unregisters an entity as observable for a given topic. 
        /// This method is called by a management service or actor.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="useObserverAsProxy">Observable uses one observer for each cluster node as a proxy when true, 
        /// it directly sends the message to all observers otherwise.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task UnregisterObservableServiceAsync(string topic, bool useObserverAsProxy)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException($"The {nameof(topic)} parameter cannot be null.", nameof(topic));
            }
            try
            {
                EntityId id = await this.GetEntityIdAsync();
                if (id != null)
                {
                    for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
                    {
                        try
                        {
                            IRegistryService registryService =
                                ServiceProxy.Create<IRegistryService>(ConfigurationHelper.RegistryServiceUri,
                                                                      new ServicePartitionKey(PartitionResolver.Resolve(topic, ConfigurationHelper.RegistryServicePartitionCount)));
                            await registryService.UnregisterObservableAsync(topic, id);
                            break;
                        }
                        catch (FabricTransientException ex)
                        {
                            ServiceEventSource.Current.Error(ex);
                            if (k == ConfigurationHelper.MaxQueryRetryCount)
                            {
                                throw;
                            }
                        }
                        catch (AggregateException ex)
                        {
                            foreach (Exception innerException in ex.InnerExceptions)
                            {
                                ServiceEventSource.Current.Error(innerException);
                            }
                            if (k == ConfigurationHelper.MaxQueryRetryCount)
                            {
                                throw;
                            }
                        }
                        catch (Exception ex)
                        {
                            ServiceEventSource.Current.Error(ex);
                            if (k == ConfigurationHelper.MaxQueryRetryCount)
                            {
                                throw;
                            }
                        }
                        await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
                    }
                    IReliableDictionary<string, Dictionary<Uri, ObserverInfo>> topicsDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<Uri, ObserverInfo>>>(Constants.TopicDictionary);
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        List<Task> taskList = new List<Task>();
                        ConditionalValue<Dictionary<Uri, ObserverInfo>> result = await topicsDictionary.TryGetValueAsync(transaction, topic);
                        if (result.HasValue)
                        {
                            Dictionary<Uri, ObserverInfo> observers = result.Value;
                            if (useObserverAsProxy)
                            {
                                // observers are grouped by NodeName
                                taskList.AddRange(
                                    observers.
                                        Select(kvp => kvp.Value.EntityId).
                                        GroupBy(e => e.NodeName).
                                        Select(groupingByNodeName => ProcessingHelper.GetObserverProxyAndList(groupingByNodeName, true)).
                                        Select(tuple => ProcessingHelper.UnregisterObservableAsync(topic, tuple.Item1, id, tuple.Item2)));
                            }
                            else
                            {
                                taskList.AddRange(
                                    observers.Select(observer => ProcessingHelper.UnregisterObservableAsync(topic, observer.Value.EntityId, id, null)));
                            }
                            await Task.WhenAll(taskList.ToArray());
                            await transaction.CommitAsync();
                        }
                    }
                }
                ServiceEventSource.Current.Message($"Observable successfully unregistered.\r\n[Observable]: {id}\r\n[Publication]: Topic=[{topic}].");
            }
            catch (AggregateException ex)
            {
                foreach (Exception e in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(e);
                }
                throw;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Sends data to observers for a given topic. 
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The current notification information.</param>
        /// <param name="useObserverAsProxy">Observable uses one observer for each cluster node as a proxy when true, 
        /// it directly sends the message to all observers otherwise.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task NotifyObserversAsync(string topic, Message message, bool useObserverAsProxy)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException($"The {nameof(topic)} parameter cannot be null.", nameof(topic));
            }
            if (string.IsNullOrWhiteSpace(message?.Body))
            {
                throw new ArgumentException($"The {nameof(message)} parameter cannot be null.", nameof(message));
            }
            try
            {
                EntityId id = await this.GetEntityIdAsync();
                IReliableDictionary<string, Dictionary<Uri, ObserverInfo>> topicsDictionary =
                    await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<Uri, ObserverInfo>>>(Constants.TopicDictionary);
                using (ITransaction transaction = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<Dictionary<Uri, ObserverInfo>> result = await topicsDictionary.TryGetValueAsync(transaction, topic);
                    if (!result.HasValue)
                    {
                        throw new ArgumentException($"{id} is not an observable for Topic=[{topic}]");
                    }
                    await transaction.CommitAsync();
                }
                List<Task> taskList = new List<Task>();
                using (ITransaction transaction = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<Dictionary<Uri, ObserverInfo>> result = await topicsDictionary.TryGetValueAsync(transaction, topic);
                    if (result.HasValue)
                    {
                        Dictionary<Uri, ObserverInfo> observers = result.Value;
                        if (useObserverAsProxy)
                        {
                            if (JsonSerializerHelper.IsJson(message.Body))
                            {
                                // Create the list of observers: an observer is added to the list only at least of of its 
                                // filter predicates is satisified by the message.
                                JObject jObject = JsonSerializerHelper.Deserialize(message.Body);
                                IEnumerable<EntityId> observerEnumerable = (from subscriptionInfo in observers.
                                    Where(kvp => kvp.Value.Predicates.Any()).
                                    Select(observer => observer.Value)
                                    let ok = subscriptionInfo.Predicates.Any(predicate => predicate(jObject))
                                    where ok
                                    select subscriptionInfo.EntityId);

                                // observers are grouped by NodeName
                                taskList.AddRange(
                                    observerEnumerable.
                                        GroupBy(e => e.NodeName).
                                        Select(groupingByNodeName => ProcessingHelper.GetObserverProxyAndList(groupingByNodeName, true)).
                                        Select(tuple => ProcessingHelper.NotifyObserverAsync(topic, message, tuple.Item1, id, tuple.Item2)));
                            }
                            else
                            {
                                // observers are grouped by NodeName
                                taskList.AddRange(
                                    observers.
                                        Select(kvp => kvp.Value.EntityId).
                                        GroupBy(e => e.NodeName).
                                        Select(groupingByNodeName => ProcessingHelper.GetObserverProxyAndList(groupingByNodeName, true)).
                                        Select(tuple => ProcessingHelper.NotifyObserverAsync(topic, message, tuple.Item1, id, tuple.Item2)));
                            }
                        }
                        else
                        {
                            if (JsonSerializerHelper.IsJson(message.Body))
                            {
                                JObject jObject = JsonSerializerHelper.Deserialize(message.Body);
                                taskList.AddRange(
                                    (from subscriptionInfo in
                                        observers.Where(kvp => kvp.Value.Predicates.Any()).
                                            Select(observer => observer.Value)
                                        let ok = subscriptionInfo.Predicates.Any(predicate => predicate(jObject))
                                        where ok
                                        select subscriptionInfo.EntityId).
                                        Select(entityId => ProcessingHelper.NotifyObserverAsync(topic, message, entityId, id, null)));
                            }
                            else
                            {
                                taskList.AddRange(
                                    observers.Select(
                                        observer => ProcessingHelper.NotifyObserverAsync(topic, message, observer.Value.EntityId, id, null)));
                            }
                        }
                    }
                    await Task.WhenAll(taskList.ToArray());
                    await transaction.CommitAsync();
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception e in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(e);
                }
                throw;
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