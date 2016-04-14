// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.RegistryService
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.AzureCat.Samples.ObserverPattern.Interfaces;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    public class RegistryService : StatefulService, IRegistryService
    {
        #region StatefulService overriden methods

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service replica.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            ServiceEventSource.Current.Message(Constants.ServiceReplicaListenersCreated);
            return new[]
            {
                new ServiceReplicaListener(this.CreateServiceRemotingListener)
            };
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
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        #endregion

        #region IRegistryService Methods

        /// <summary>
        /// Registers an entity as observable for a given topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task RegisterObservableAsync(string topic, EntityId entityId)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException($"The {nameof(topic)} parameter cannot be null.", nameof(topic));
            }
            if (entityId == null)
            {
                throw new ArgumentException($"The {nameof(entityId)} parameter cannot be null.", nameof(entityId));
            }
            try
            {
                IReliableDictionary<string, EntityId> observablesDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, EntityId>>(topic);
                string entityUri = entityId.EntityUri.ToString();

                using (ITransaction transaction = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<EntityId> result = await observablesDictionary.TryGetValueAsync(transaction, entityUri);
                    if (!result.HasValue)
                    {
                        await observablesDictionary.AddOrUpdateAsync(transaction, entityUri, entityId, (k, v) => v);
                        ServiceEventSource.Current.Message($"Observable successfully registered.\r\n[Observable]: {entityId}\r\n[Publication]: Topic=[{topic}]");
                    }
                    await transaction.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Unregisters an entity as observable for a given topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task UnregisterObservableAsync(string topic, EntityId entityId)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException($"The {nameof(topic)} parameter cannot be null.", nameof(topic));
            }
            if (entityId == null)
            {
                throw new ArgumentException($"The {nameof(entityId)} parameter cannot be null.", nameof(entityId));
            }
            try
            {
                IReliableDictionary<string, EntityId> observablesDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, EntityId>>(topic);
                string entityUri = entityId.EntityUri.ToString();

                using (ITransaction transaction = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<EntityId> result = await observablesDictionary.TryGetValueAsync(transaction, entityUri);
                    if (result.HasValue)
                    {
                        await observablesDictionary.TryRemoveAsync(transaction, entityUri);
                        ServiceEventSource.Current.Message($"Observable successfully unregistered.\r\n[Observable]: {entityId}\r\n[Publication]: Topic=[{topic}]");
                    }
                    await transaction.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
                throw;
            }
        }


        /// <summary>
        /// Used by an observable to send an heartbeat message to the registry.
        /// </summary>
        /// <param name="entityId">The entity id of the observable.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public Task HearthbeatAsync(EntityId entityId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an enumerable containing the observables for a given topic.
        /// </summary>
        /// <param name="topic">The topic</param>
        /// <param name="filterExpression">Specifies a filter expression.</param>
        /// <returns>An enumerable containing the observables for the topic contained in the call argument.</returns>
        public async Task<IEnumerable<EntityId>> QueryObservablesAsync(string topic, string filterExpression)
        {
            List<EntityId> observables = new List<EntityId>();
            try
            {
                if (string.IsNullOrWhiteSpace(topic))
                {
                    return null;
                }
                IReliableDictionary<string, EntityId> observablesDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, EntityId>>(topic);

                using (ITransaction transaction = this.StateManager.CreateTransaction())
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    IAsyncEnumerable<KeyValuePair<string, EntityId>> enumerable = string.IsNullOrWhiteSpace(filterExpression) ?
                        await observablesDictionary.CreateEnumerableAsync(transaction, EnumerationMode.Ordered) :
                        await observablesDictionary.CreateEnumerableAsync(transaction,
                                                                          s => string.Compare(s, filterExpression, StringComparison.InvariantCultureIgnoreCase) == 0, 
                                                                          EnumerationMode.Ordered);
                    using (IAsyncEnumerator<KeyValuePair<string, EntityId>> enumerator = enumerable.GetAsyncEnumerator())
                    {
                        while (await enumerator.MoveNextAsync(cancellationTokenSource.Token).ConfigureAwait(false))
                        {
                            observables.Add(enumerator.Current.Value);
                        }
                    }
                }
                if (observables.Any())
                {
                    IList<EntityId> entityIds = observables;
                    if (entityIds.Any())
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (EntityId observable in entityIds)
                        {
                            if (observable == entityIds.Last())
                            {
                                stringBuilder.Append($"{observable}");
                            }
                            else
                            {
                                stringBuilder.AppendLine($"{observable}");
                            }
                        }
                        ServiceEventSource.Current.Message(
                            $"Observables successfully retrieved. Topic=[{topic}] FilterExpression=[{filterExpression}]\r\n{stringBuilder}");
                    }
                    else
                    {
                        ServiceEventSource.Current.Message($"No observables retrieved. Topic=[{topic}] FilterExpression=[{filterExpression}]");
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
                throw;
            }
            return observables;
        }

        #endregion

        #region Public Constructors
        public RegistryService(StatefulServiceContext serviceContext) : base(serviceContext)
        {
        }

        public RegistryService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
        {
        } 
        #endregion
    }
}