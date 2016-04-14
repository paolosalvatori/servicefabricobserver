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
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Newtonsoft.Json.Linq;

    [StatePersistence(StatePersistence.Persisted)]
    public abstract class ObservableActorBase : Actor, IObservableActor
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

        public event Func<SubscriptionEventArgs, Task> ObserverRegistered;

        public event Func<SubscriptionEventArgs, Task> ObserverUnregistered;

        #endregion

        #region IObservableActor methods

        /// <summary>
        /// Registers an observer. This methods is invoked by an observer.
        /// </summary>
        /// <param name="filterExpressions">Specifies filter expressions.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id of the observer.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task RegisterObserverAsync(string topic, IEnumerable<string> filterExpressions, EntityId entityId)
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
            ConditionalValue<Dictionary<Uri, ObserverInfo>> topicState = await this.StateManager.TryGetStateAsync<Dictionary<Uri, ObserverInfo>>(topic);
            if (!topicState.HasValue)
            {
                throw new ArgumentException($"{id} is not an observable for Topic=[{topic}]");
            }
            Dictionary<Uri, ObserverInfo> observerDictionary = topicState.Value;
            if (entityId == null)
            {
                throw new ArgumentException($"The {nameof(entityId)} parameter cannot be null.", nameof(entityId));
            }
            IList<string> enumerable = filterExpressions as IList<string> ?? filterExpressions.ToList();
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    if (!observerDictionary.ContainsKey(entityId.EntityUri))
                    {
                        observerDictionary.Add(entityId.EntityUri, new ObserverInfo(enumerable, entityId));
                        StringBuilder stringBuilder = new StringBuilder($"Observer successfully registered.\r\n[Observable]: {id}\r\n[Observer]: {entityId}\r\n[Subscription]: Topic=[{topic}]");
                        int i = 1;
                        foreach (string expression in enumerable.Where(expression => !string.IsNullOrWhiteSpace(expression)))
                        {
                            stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
                        }
                        await this.StateManager.SetStateAsync(topic, observerDictionary);
                        ActorEventSource.Current.Message(stringBuilder.ToString());
                    }
                    else
                    {
                        StringBuilder stringBuilder = new StringBuilder($"Observer already registered.\r\n[Observable]: {id}\r\n[Observer]: {entityId}\r\n[Subscription]: Topic=[{topic}]");
                        int i = 1;
                        foreach (string expression in enumerable.Where(expression => !string.IsNullOrWhiteSpace(expression)))
                        {
                            stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
                        }
                        ActorEventSource.Current.Message(stringBuilder.ToString());
                    }
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
                    ActorEventSource.Current.Error(e);
                }
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
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
                    ConditionalValue<Dictionary<Uri, ObserverInfo>> topicState = await this.StateManager.TryGetStateAsync<Dictionary<Uri, ObserverInfo>>(topic);
                    if (!topicState.HasValue)
                    {
                        return;
                    }
                    Dictionary<Uri, ObserverInfo> observerDictionary = topicState.Value;
                    if (!observerDictionary.ContainsKey(entityId.EntityUri))
                    {
                        return;
                    }
                    observerDictionary.Remove(entityId.EntityUri);
                    await this.StateManager.SetStateAsync(topic, observerDictionary);
                    ActorEventSource.Current.Message($"Observer successfully unregistered.\r\n[Observable]: {id}\r\n[Observer]: {entityId}\r\n[Subscription]: Topic=[{topic}]");
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
                    ActorEventSource.Current.Error(e);
                }
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
            }
        }

        /// <summary>
        /// Registers an entity as observable for a given topic. 
        /// This method is called by a management service or actor.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task RegisterObservableActorAsync(string topic)
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
            ConditionalValue<Dictionary<Uri, ObserverInfo>> topicState = await this.StateManager.TryGetStateAsync<Dictionary<Uri, ObserverInfo>>(topic);
            if (topicState.HasValue)
            {
                throw new ArgumentException($"{id} is already an observable for Topic=[{topic}]");
            }
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    IRegistryService registryService = ServiceProxy.Create<IRegistryService>(ConfigurationHelper.RegistryServiceUri, 
                                                                                             new ServicePartitionKey(PartitionResolver.Resolve(topic, ConfigurationHelper.RegistryServicePartitionCount)));
                    await registryService.RegisterObservableAsync(topic, id);
                    ActorEventSource.Current.Message($"Observable successfully registered.\r\n[Observable]: {id}\r\n[Publication]: Topic=[{topic}].");
                    await this.StateManager.SetStateAsync(topic, new Dictionary<Uri, ObserverInfo>());
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
        /// Clear all observers for all topics.
        /// </summary>
        /// <param name="useObserverAsProxy">Observable uses one observer for each cluster node as a proxy when true, 
        /// it directly sends the message to all observers otherwise.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task ClearObserversAndPublicationsAsync(bool useObserverAsProxy)
        {
            try
            {
                IEnumerable<string> stateNames = await this.StateManager.GetStateNamesAsync();
                IEnumerable<Task> taskEnumerable = stateNames.Where(state => string.Compare(state, EntityIdState, StringComparison.InvariantCultureIgnoreCase) != 0)
                                                             .Select(topic => this.UnregisterObservableActorAsync(topic, useObserverAsProxy));
                await Task.WhenAll(taskEnumerable);
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
        /// Unregisters an entity as observable for a given topic. 
        /// This method is called by a management service or actor.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="useObserverAsProxy">Observable uses one observer for each cluster node as a proxy when true, 
        /// it directly sends the message to all observers otherwise.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task UnregisterObservableActorAsync(string topic, bool useObserverAsProxy)
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
            ConditionalValue<Dictionary<Uri, ObserverInfo>> topicState = await this.StateManager.TryGetStateAsync<Dictionary<Uri, ObserverInfo>>(topic);
            if (!topicState.HasValue)
            {
                throw new ArgumentException($"{id} is not an observable for Topic=[{topic}]");
            }
            Dictionary<Uri, ObserverInfo> observerDictionary = topicState.Value;
            try
            {
                for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
                {
                    try
                    {
                        IRegistryService registryService = ServiceProxy.Create<IRegistryService>(ConfigurationHelper.RegistryServiceUri,
                                                                                                 new ServicePartitionKey(PartitionResolver.Resolve(topic, ConfigurationHelper.RegistryServicePartitionCount)));
                        await registryService.UnregisterObservableAsync(topic, id);
                        break;
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
                List<Task> taskList = new List<Task>();

                try
                {
                    if (useObserverAsProxy)
                    {
                        // observers are grouped by NodeName
                        taskList.AddRange(
                            observerDictionary.
                                Select(kvp => kvp.Value.EntityId).
                                GroupBy(e => e.NodeName).
                                Select(groupingByNodeName => ProcessingHelper.GetObserverProxyAndList(groupingByNodeName, true)).
                                Select(tuple => ProcessingHelper.UnregisterObservableAsync(topic, tuple.Item1, id, tuple.Item2)));
                    }
                    else
                    {
                        taskList.AddRange(
                            observerDictionary.Select(
                                observer => ProcessingHelper.UnregisterObservableAsync(topic, observer.Value.EntityId, id, null)));
                    }
                    await Task.WhenAll(taskList.ToArray());
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
                await this.StateManager.TryRemoveStateAsync(topic);
                ActorEventSource.Current.Message($"Observable successfully unregistered.\r\n[Observable]: {id}\r\n[Publication]: Topic=[{topic}].");
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
            EntityId id = await this.GetEntityIdAsync();
            if (id == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException($"The {nameof(topic)} parameter cannot be null.", nameof(topic));
            }
            ConditionalValue<Dictionary<Uri, ObserverInfo>> topicState = await this.StateManager.TryGetStateAsync<Dictionary<Uri, ObserverInfo>>(topic);
            if (!topicState.HasValue)
            {
                throw new ArgumentException($"{id} is not an observable for Topic=[{topic}]");
            }
            Dictionary<Uri, ObserverInfo> observerDictionary = topicState.Value;
            if (string.IsNullOrWhiteSpace(message?.Body))
            {
                throw new ArgumentException($"The {nameof(message)} parameter cannot be null.", nameof(message));
            }
            try
            {
                List<Task> taskList = new List<Task>();
                if (useObserverAsProxy)
                {
                    if (JsonSerializerHelper.IsJson(message.Body))
                    {
                        // Create the list of observers: an observer is added to the list only at least of of its 
                        // filter predicates is satisified by the message.
                        JObject jObject = JsonSerializerHelper.Deserialize(message.Body);
                        IEnumerable<EntityId> observerEnumerable = (from subscriptionInfo in observerDictionary.
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
                            observerDictionary.
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
                            (from subscriptionInfo in observerDictionary.
                                Where(kvp => kvp.Value.Predicates.Any()).
                                Select(observer => observer.Value)
                                let ok = subscriptionInfo.Predicates.Any(predicate => predicate(jObject))
                                where ok
                                select subscriptionInfo.EntityId).
                                Select(
                                    entityId => ProcessingHelper.NotifyObserverAsync(
                                        topic,
                                        message,
                                        entityId,
                                        id,
                                        null)));
                    }
                    else
                    {
                        taskList.AddRange(
                            observerDictionary.Select(
                                observer => ProcessingHelper.NotifyObserverAsync(
                                    topic,
                                    message,
                                    observer.Value.EntityId,
                                    id,
                                    null)));
                    }
                }
                await Task.WhenAll(taskList.ToArray());
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
        }

        #endregion
    }
}