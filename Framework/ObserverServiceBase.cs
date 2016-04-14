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
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    public abstract class ObserverServiceBase : StatefulService, IClientObserverService, IServerObserverService
    {
        #region Public Events

        public event Func<NotificationEventArgs<Message>, Task> NotificationMessageReceived;

        public event Func<SubscriptionEventArgs, Task> ObservableUnregistered;

        #endregion

        #region Protected Constructor

        protected ObserverServiceBase(StatefulServiceContext context)
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
                    IReliableDictionary<string, EntityId> entityIdDictionary =
                        this.StateManager.GetOrAddAsync<IReliableDictionary<string, EntityId>>(Constants.EntityIdDictionary).Result;
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

        #region IClientObserverService methods
        /// <summary>
        /// Registers an observer. This methods is invoked by an observer.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="filterExpressions">Specifies filter expressions.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// This method is called by a management service or actor.
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task RegisterObserverServiceAsync(string topic, IEnumerable<string> filterExpressions, EntityId entityId)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException($"The {nameof(topic)} parameter cannot be null.", nameof(topic));
            }
            if (entityId == null)
            {
                throw new ArgumentException($"The {nameof(entityId)} parameter cannot be null.", nameof(entityId));
            }
            IList<string> expressions = filterExpressions as IList<string> ?? filterExpressions.ToList();
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    EntityId id = await this.GetEntityIdAsync();
                    if (entityId.Kind == EntityKind.Actor)
                    {
                        IServerObservableActor actorProxy = ActorProxy.Create<IServerObservableActor>(entityId.ActorId, entityId.ServiceUri);
                        await actorProxy.RegisterObserverAsync(topic, expressions, id);
                    }
                    else
                    {
                        IServerObservableService serviceProxy = entityId.PartitionKey.HasValue
                            ? ServiceProxy.Create<IServerObservableService>(entityId.ServiceUri, new ServicePartitionKey(entityId.PartitionKey.Value))
                            : ServiceProxy.Create<IServerObservableService>(entityId.ServiceUri);
                        await serviceProxy.RegisterObserverAsync(topic, expressions, id);
                    }
                    IReliableDictionary<string, Dictionary<Uri, EntityId>> topicsDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<Uri, EntityId>>>(Constants.TopicDictionary);
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<Dictionary<Uri, EntityId>> result = await topicsDictionary.TryGetValueAsync(transaction, topic);
                        Dictionary<Uri, EntityId> observables = result.HasValue ? result.Value : new Dictionary<Uri, EntityId>();
                        if (!observables.ContainsKey(entityId.EntityUri))
                        {
                            observables.Add(entityId.EntityUri, entityId);
                        }
                        await topicsDictionary.AddOrUpdateAsync(transaction, topic, e => observables, (e, s) => observables);
                        await transaction.CommitAsync();
                    }
                    StringBuilder stringBuilder =
                        new StringBuilder(
                            $"Observer successfully registered.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Subscription]: Topic=[{topic}]");
                    int i = 1;
                    foreach (string expression in expressions.Where(expression => !string.IsNullOrWhiteSpace(expression)))
                    {
                        stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
                    }
                    ServiceEventSource.Current.Message(stringBuilder.ToString());
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
        /// Unregisters an observer. This methods is invoked by an observer.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// This method is called by a management service or actor.
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task UnregisterObserverServiceAsync(string topic, EntityId entityId)
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
                    IReliableDictionary<string, Dictionary<Uri, EntityId>> topicsDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<Uri, EntityId>>>(Constants.TopicDictionary);
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<Dictionary<Uri, EntityId>> result = await topicsDictionary.TryGetValueAsync(transaction, topic);
                        if (result.HasValue)
                        {
                            Dictionary<Uri, EntityId> observables = result.Value;
                            if (!observables.ContainsKey(entityId.EntityUri))
                            {
                                ServiceEventSource.Current.Message(
                                    $"Observer not registered to the specified observable.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Publication]: Topic=[{topic}]");
                            }
                            if (entityId.Kind == EntityKind.Actor)
                            {
                                IServerObservableActor actorProxy = ActorProxy.Create<IServerObservableActor>(entityId.ActorId, entityId.ServiceUri);
                                await actorProxy.UnregisterObserverAsync(topic, id);
                            }
                            else
                            {
                                IServerObservableService serviceProxy = entityId.PartitionKey.HasValue
                                    ? ServiceProxy.Create<IServerObservableService>(entityId.ServiceUri, new ServicePartitionKey(entityId.PartitionKey.Value))
                                    : ServiceProxy.Create<IServerObservableService>(entityId.ServiceUri);
                                await serviceProxy.UnregisterObserverAsync(topic, id);
                            }
                            observables.Remove(entityId.EntityUri);
                            if (observables.Any())
                            {
                                await topicsDictionary.AddOrUpdateAsync(transaction, topic, e => observables, (e, s) => observables);
                            }
                            else
                            {
                                await topicsDictionary.TryRemoveAsync(transaction, topic);
                            }
                        }
                        else
                        {
                            ServiceEventSource.Current.Message(
                                $"Observer not registered to the specified topic.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Publication]: Topic=[{topic}]");
                        }
                        await transaction.CommitAsync();
                    }
                    ServiceEventSource.Current.Message(
                        $"Observer successfully unregistered.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Subscription]: Topic=[{topic}]");
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
        /// Unregisters an observer service partition from all observables on all topics.
        /// </summary>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task ClearSubscriptionsAsync()
        {
            try
            {
                IReliableDictionary<string, Dictionary<Uri, EntityId>> topicsDictionary =
                    await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<Uri, EntityId>>>(Constants.TopicDictionary);
                using (ITransaction transaction = this.StateManager.CreateTransaction())
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    IAsyncEnumerable<KeyValuePair<string, Dictionary<Uri, EntityId>>> enumerable = await topicsDictionary.CreateEnumerableAsync(transaction);
                    using (IAsyncEnumerator<KeyValuePair<string, Dictionary<Uri, EntityId>>> enumerator = enumerable.GetAsyncEnumerator())
                    {
                        List<Task> taskList = new List<Task>();
                        while (await enumerator.MoveNextAsync(cancellationTokenSource.Token).ConfigureAwait(false))
                        {
                            string topic = enumerator.Current.Key;
                            Dictionary<Uri, EntityId> observables = enumerator.Current.Value;
                            taskList.AddRange(observables.Keys.Select(observableUri => this.UnregisterObserverServiceAsync(topic, observables[observableUri])));
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

        /// <summary>
        /// Reads the messages for the observer actor from its messagebox.
        /// </summary>
        /// <returns>The messages for the current observer actor.</returns>
        public virtual async Task<IEnumerable<Message>> ReadMessagesFromMessageBoxAsync()
        {
            EntityId id = await this.GetEntityIdAsync();
            IEnumerable<Message> messages = await ProcessingHelper.ReadMessagesFromMessageBoxAsync(id.EntityUri);
            if (messages == null)
            {
                return null;
            }
            StringBuilder stringBuilder = new StringBuilder($"Messages read from the MessageBox.\r\n[Observer]: {id}");
            int i = 1;
            foreach (Message message in messages)
            {
                stringBuilder.Append($"\r\nMessage[{i++}]=[{message.Body}]");
            }
            ServiceEventSource.Current.Message(stringBuilder.ToString());
            return messages;
        }

        /// <summary>
        /// Used by an observable to send an heartbeat message to an observer. 
        /// </summary>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public Task SendHeartbeatToObserverAsync(EntityId entityId)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IServerObserverService
        /// <summary>
        /// Provides the observer with new data. This method is invoked by an observable.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The current notification information.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <param name="observers">A list of observers in the same cluster node. This field is optional.
        /// When the list if not null or empty, the observer will forward the message to each observer which EntityId is in the list.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task NotifyObserverAsync(string topic, Message message, EntityId entityId, IEnumerable<EntityId> observers)
        {
            EntityId id = null;
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    id = await this.GetEntityIdAsync();
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
            try
            {
                ServiceEventSource.Current.Message(
                    $"Message Received.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Message]: Topic=[{topic}] Body=[{message?.Body ?? "NULL"}]");
                if (observers != null)
                {
                    IList<EntityId> observerList = observers as IList<EntityId> ?? observers.ToList();
                    if (observerList.Any())
                    {
                        StringBuilder builder = new StringBuilder($"Observer Proxy:\r\n[From]: {id}");
                        foreach (EntityId item in observerList)
                        {
                            builder.Append($"\r\n[To]: {item}");
                        }
                        ServiceEventSource.Current.Message(builder.ToString());
                        List<Task> taskList = new List<Task>();
                        taskList.AddRange(
                            observerList.Select(
                                observer => ProcessingHelper.NotifyObserverAsync(
                                    topic,
                                    message,
                                    observer,
                                    entityId,
                                    null)));
                        await Task.WhenAll(taskList.ToArray());
                    }
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
            if (this.NotificationMessageReceived == null)
            {
                return;
            }
            try
            {
                Delegate[] invocationList = this.NotificationMessageReceived.GetInvocationList();
                Task[] handlerTasks = new Task[invocationList.Length];
                NotificationEventArgs<Message> args = new NotificationEventArgs<Message>(topic, message, entityId);
                for (int i = 0; i < invocationList.Length; i++)
                {
                    handlerTasks[i] = ProcessingHelper.ExecuteEventHandlerAsync((Func<NotificationEventArgs<Message>, Task>) invocationList[i], args);
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
        /// Unregisters an observable. This method is invoked by an observable.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <param name="observers">A list of observers in the same cluster node. This field is optional.
        /// When the list if not null or empty, the observer will forward the message to each observer which EntityId is in the list.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task UnregisterObservableAsync(string topic, EntityId entityId, IEnumerable<EntityId> observers)
        {
            EntityId id = null;
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException($"The {nameof(topic)} parameter cannot be null.", nameof(topic));
            }
            if (entityId == null)
            {
                throw new ArgumentException($"The {nameof(entityId)} parameter cannot be null.", nameof(entityId));
            }
            bool unregistered = false;
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    id = await this.GetEntityIdAsync();
                    IReliableDictionary<string, Dictionary<Uri, EntityId>> topicsDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<Uri, EntityId>>>(Constants.TopicDictionary);
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<Dictionary<Uri, EntityId>> result = await topicsDictionary.TryGetValueAsync(transaction, topic);
                        if (result.HasValue)
                        {
                            Dictionary<Uri, EntityId> observables = result.Value;
                            if (observables.ContainsKey(entityId.EntityUri))
                            {
                                observables.Remove(entityId.EntityUri);
                                if (observables.Any())
                                {
                                    await topicsDictionary.AddOrUpdateAsync(transaction, topic, e => observables, (e, s) => observables);
                                }
                                else
                                {
                                    await topicsDictionary.TryRemoveAsync(transaction, topic);
                                }
                                unregistered = true;
                                ServiceEventSource.Current.Message(
                                    $"Observable successfully unregistered.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Publication]: Topic=[{topic}]");
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
            try
            {
                if (observers != null)
                {
                    IList<EntityId> observerList = observers as IList<EntityId> ?? observers.ToList();
                    if (observerList.Any())
                    {
                        StringBuilder builder = new StringBuilder($"Observer Proxy:\r\n[From]: {id}");
                        foreach (EntityId item in observerList)
                        {
                            builder.Append($"\r\n[To]: {item}");
                        }
                        ServiceEventSource.Current.Message(builder.ToString());
                        List<Task> taskList = new List<Task>();
                        taskList.AddRange(observerList.Select(observer => ProcessingHelper.UnregisterObservableAsync(topic, observer, entityId, null)));
                        await Task.WhenAll(taskList.ToArray());
                    }
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception e in ex.InnerExceptions)
                {
                    ActorEventSource.Current.Error(e);
                }
                throw;
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
            if (!unregistered || this.ObservableUnregistered == null)
            {
                return;
            }
            try
            {
                Delegate[] invocationList = this.ObservableUnregistered.GetInvocationList();
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
                    ActorEventSource.Current.Error(e);
                }
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
            }
        }

        #endregion
    }
}