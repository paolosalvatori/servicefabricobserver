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
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    [StatePersistence(StatePersistence.Persisted)]
    public abstract class ObserverActorBase :Actor, IObserverActor
    {
        #region Private Constants
        //************************************
        // States
        //************************************
        private const string EntityIdState = "entityId";
        #endregion

        #region IEntityIdActor methods

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
                    ConditionalValue<EntityId> entityIdState = await this.StateManager.TryGetStateAsync<EntityId>(EntityIdState);
                    return entityIdState.HasValue ? entityIdState.Value : null;
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
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
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
            throw new TimeoutException(Constants.RetryTimeoutExhausted);
        }

        #endregion

        #region Actor overridden methods

        /// <summary>
        /// Occurs when the Actor is activated.
        /// </summary>
        /// <returns>The asynchronous result of the operation.</returns>
        protected override async Task OnActivateAsync()
        {
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    EntityId entityId;
                    ConditionalValue<EntityId> entityIdState = await this.StateManager.TryGetStateAsync<EntityId>(EntityIdState);
                    if (!entityIdState.HasValue)
                    {
                        entityId = new EntityId(this.Id, this.ServiceUri);
                        await this.StateManager.TryAddStateAsync(EntityIdState, entityId);
                    }
                    else
                    {
                        entityId = entityIdState.Value;
                    }
                    ActorEventSource.Current.ActorMessage(this, $"{entityId} activated.");
                    return;
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
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
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
            throw new TimeoutException(Constants.RetryTimeoutExhausted);
        }

        #endregion

        #region Public Events

        public event Func<NotificationEventArgs<Message>, Task> NotificationMessageReceived;

        public event Func<SubscriptionEventArgs, Task> ObservableUnregistered;

        #endregion

        #region IObserverActor methods

        /// <summary>
        /// Registers an observer. This methods is invoked by an observer.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="filterExpressions">Specifies filter expressions.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// This method is called by a management service or actor.
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task RegisterObserverActorAsync(string topic, IEnumerable<string> filterExpressions, EntityId entityId)
        {
            EntityId id = await this.GetEntityIdAsync();
            if (id == null)
            {
                return;
            }
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
                    
                    ConditionalValue<Dictionary<Uri, EntityId>> topicState = await this.StateManager.TryGetStateAsync<Dictionary<Uri, EntityId>>(topic);
                    Dictionary<Uri, EntityId> observableDictionary = topicState.HasValue ?
                                                                     topicState.Value :
                                                                     new Dictionary<Uri, EntityId>();
                    if (observableDictionary.ContainsKey(entityId.EntityUri))
                    {
                        return;
                    }
                    observableDictionary.Add(entityId.EntityUri, entityId);
                    StringBuilder stringBuilder = new StringBuilder($"Observer successfully registered.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Subscription]: Topic=[{topic}]");
                    int i = 1;
                    foreach (string expression in expressions.Where(expression => !string.IsNullOrWhiteSpace(expression)))
                    {
                        stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
                    }
                    await this.StateManager.SetStateAsync(topic, observableDictionary);
                    ActorEventSource.Current.Message(stringBuilder.ToString());
                    return;
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
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
        public async Task UnregisterObserverActorAsync(string topic, EntityId entityId)
        {
            EntityId id = await this.GetEntityIdAsync();
            if (id == null)
            {
                return;
            }
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
                    ConditionalValue<Dictionary<Uri, EntityId>> topicState = await this.StateManager.TryGetStateAsync<Dictionary<Uri, EntityId>>(topic);
                    if (!topicState.HasValue)
                    {
                        ActorEventSource.Current.Message($"Observer not registered to the specified topic.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Publication]: Topic=[{topic}]");
                        return;
                    }
                    Dictionary<Uri, EntityId> observableDictionary = topicState.Value; 
                    if (!observableDictionary.ContainsKey(entityId.EntityUri))
                    {
                        ActorEventSource.Current.Message($"Observer not registered to the specified observable.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Publication]: Topic=[{topic}]");
                        return;
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
                    observableDictionary.Remove(entityId.EntityUri);
                    if (!observableDictionary.Any())
                    {
                        await this.StateManager.TryRemoveStateAsync(topic);
                    }
                    else
                    {
                        await this.StateManager.SetStateAsync(topic, observableDictionary);
                    }
                    ActorEventSource.Current.Message($"Observer successfully unregistered.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Subscription]: Topic=[{topic}]");
                    return;
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
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
                await Task.Delay(ConfigurationHelper.BackoffQueryDelay);
            }
            throw new TimeoutException(Constants.RetryTimeoutExhausted);
        }

        /// <summary>
        /// Unregisters an observer actor from all observables on all topics.
        /// </summary>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task ClearSubscriptionsAsync()
        {
            try
            {
                IEnumerable<string> stateNames = await this.StateManager.GetStateNamesAsync();
                IEnumerable<string> topics = stateNames.Where(state => string.Compare(state, EntityIdState, StringComparison.InvariantCultureIgnoreCase) != 0);
                List<Task> taskList = new List<Task>();
                foreach (string topic in topics)
                {
                    ConditionalValue<Dictionary<Uri,EntityId>> topicState = await this.StateManager.TryGetStateAsync<Dictionary<Uri, EntityId>>(topic);
                    if (!topicState.HasValue)
                    {
                        continue;
                    }
                    Dictionary<Uri, EntityId> observables = topicState.Value;
                    taskList.AddRange(observables.Keys.Select(observableUri => this.UnregisterObserverActorAsync(topic, observables[observableUri])));
                }
                await Task.WhenAll(taskList);
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

        /// <summary>
        /// Reads the messages for the observer actor from its messagebox.
        /// </summary>
        /// <returns>The messages for the current observer actor.</returns>
        public virtual async Task<IEnumerable<Message>> ReadMessagesFromMessageBoxAsync()
        {
            EntityId id = await this.GetEntityIdAsync();
            if (id == null)
            {
                return null;
            }
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
            ActorEventSource.Current.Message(stringBuilder.ToString());
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

        /// <summary>
        /// Provides the observer with new data. This method is invoked by an observable.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The current notification information.</param>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <param name="observers">A list of observers in the same cluster node. This field is optional.
        /// When the list if not null or empty, the observer will forward the message to each observer which EntityId is in the list.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public virtual async Task NotifyObserverAsync(string topic, Message message, EntityId entityId, IEnumerable<EntityId> observers)
        {
            try
            {
                EntityId id = await this.GetEntityIdAsync();
                if (id == null)
                {
                    return;
                }
                if (entityId == null)
                {
                    entityId = new EntityId {ActorId = ActorId.CreateRandom(), ServiceUri = this.ServiceUri};
                }
                ActorEventSource.Current.Message(
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
                        ActorEventSource.Current.Message(builder.ToString());
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
                    ActorEventSource.Current.Error(e);
                }
                throw;
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
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
                    ActorEventSource.Current.Error(e);
                }
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
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
            EntityId id = await this.GetEntityIdAsync();
            if (id == null)
            {
                return;
            }
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
                    ConditionalValue<Dictionary<Uri, EntityId>> topicState = await this.StateManager.TryGetStateAsync<Dictionary<Uri, EntityId>>(topic);
                    if (!topicState.HasValue)
                    {
                        ActorEventSource.Current.Message($"Observer not registered to the specified topic.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Publication]: Topic=[{topic}]");
                        return;
                    }
                    Dictionary<Uri, EntityId> observableDictionary = topicState.Value;
                    if (!observableDictionary.ContainsKey(entityId.EntityUri))
                    {
                        ActorEventSource.Current.Message($"Observer not registered to the specified observable.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Publication]: Topic=[{topic}]");
                        return;
                    }
                    observableDictionary.Remove(entityId.EntityUri);
                    if (!observableDictionary.Any())
                    {
                        await this.StateManager.TryRemoveStateAsync(topic);
                    }
                    else
                    {
                        await this.StateManager.SetStateAsync(topic, observableDictionary);
                    }
                    ActorEventSource.Current.Message($"Observable successfully unregistered.\r\n[Observable]: {entityId}\r\n[Observer]: {id}\r\n[Publication]: Topic=[{topic}]");
                    break;
                }
                catch (FabricTransientException ex)
                {
                    ActorEventSource.Current.Error(ex);
                    if (k == ConfigurationHelper.MaxQueryRetryCount)
                    {
                        throw new TimeoutException(Constants.RetryTimeoutExhausted);
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
                        ActorEventSource.Current.Message(builder.ToString());
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
            if (this.ObservableUnregistered == null)
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