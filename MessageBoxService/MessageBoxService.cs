// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.MessageBoxService
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Linq;
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

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class MessageBoxService : StatefulService, IMessageBoxService
    {
        #region Private Static Fields
        private static int MaxQueueSize = DefaultMaxQueueSize;
        #endregion
        
        #region StatefulService Overriden Methods
        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service replica.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            this.ReadSettings();
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

        #region IMessageBoxService Methods

        /// <summary>
        /// Read messages stored for an observer.
        /// </summary>
        /// <param name="uri">Observer uri.</param>
        /// <returns>An enumerable containing messages for the observer.</returns>
        public async Task<IEnumerable<Message>> ReadMessagesAsync(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentException($"The {nameof(uri)} parameter cannot be null.", nameof(uri));
            }
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    List<Message> messageList = new List<Message>();
                    IReliableDictionary<string, List<Message>> topicsDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, List<Message>>>(Constants.ObserverDictionary);
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<List<Message>> result = await topicsDictionary.TryGetValueAsync(transaction, uri.AbsoluteUri);
                        if (result.HasValue)
                        {
                            messageList = result.Value;
                            await topicsDictionary.TryRemoveAsync(transaction, uri.AbsoluteUri);
                        }
                        await transaction.CommitAsync();
                    }
                    return messageList;
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
        /// Write messages for an observer.
        /// </summary>
        /// <param name="uri">Observer uri.</param>    
        /// <param name="messages">A collection of messages.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public async Task WriteMessagesAsync(Uri uri, IEnumerable<Message> messages)
        {
            if (uri == null)
            {
                throw new ArgumentException($"The {nameof(uri)} parameter cannot be null.", nameof(uri));
            }

            List<Message> messageList = messages as List<Message> ?? messages.ToList();
            if (messages == null || !messageList.Any())
            {
                return;
            }
            if (messageList.Count >= MaxQueueSize)
            {
                throw new ArgumentException("The queue has reached its maximum capacity.", $"{nameof(messageList)}");
            }
            for (int k = 1; k <= ConfigurationHelper.MaxQueryRetryCount; k++)
            {
                try
                {
                    IReliableDictionary<string, List<Message>> topicsDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, List<Message>>>(Constants.ObserverDictionary);
                    using (ITransaction transaction = this.StateManager.CreateTransaction())
                    {
                        await topicsDictionary.AddOrUpdateAsync(
                            transaction,
                            uri.AbsoluteUri,
                            messageList,
                            (key, list) =>
                            {
                                if (list.Count + messageList.Count >= MaxQueueSize)
                                {
                                    throw new ArgumentException("The queue has reached its maximum capacity.", $"{nameof(messageList)}");
                                }
                                list.AddRange(messageList);
                                return list;
                            });
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
        }
        #endregion

        #region Private Methods

        private void ReadSettings()
        {
            try
            {
                // Read settings from the DeviceActorServiceConfig section in the Settings.xml file
                ICodePackageActivationContext activationContext = this.Context.CodePackageActivationContext;
                ConfigurationPackage config = activationContext.GetConfigurationPackageObject(ConfigurationPackage);
                ConfigurationSection section = config.Settings.Sections[ConfigurationSection];

                // Read the MaxQueueSize setting from the Settings.xml file
                if (section.Parameters.Any(p => string.Compare(p.Name,
                                                               MaxQueueSizeParameter,
                                                               StringComparison.InvariantCultureIgnoreCase) == 0))
                {
                    ConfigurationProperty parameter = section.Parameters[MaxQueueSizeParameter];
                    if (!string.IsNullOrWhiteSpace(parameter?.Value))
                    {
                        int.TryParse(parameter.Value, out MaxQueueSize);
                    }
                }
                ServiceEventSource.Current.Message($"[{MaxQueueSizeParameter}] = [{MaxQueueSize}]");
            }
            catch (KeyNotFoundException)
            {
                ActorEventSource.Current.Message($"[{MaxQueueSizeParameter}] = [{MaxQueueSize}]");
            }
        }
        #endregion

        #region Private Constants

        //************************************
        // Parameters
        //************************************
        private const string ConfigurationPackage = "Config";
        private const string ConfigurationSection = "MessageBoxServiceConfig";
        private const string MaxQueueSizeParameter = "MaxQueueSize";

        //************************************
        // Default Values
        //************************************
        private const int DefaultMaxQueueSize = 1000;

        #endregion

        #region Public Constructors
        public MessageBoxService(StatefulServiceContext serviceContext) : base(serviceContext)
        {
        }

        public MessageBoxService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
        {
        } 
        #endregion
    }
}