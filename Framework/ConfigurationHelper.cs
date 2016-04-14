// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Fabric.Query;
    using System.Linq;

    public static class ConfigurationHelper
    {
        #region Private Static Fields
        private static readonly object Semaphore = new object();
        #endregion

        #region Public Static Methods

        public static void Initialize(StatefulServiceContext parameters)
        {
            lock (Semaphore)
            {
                if (RegistryServiceUri != null)
                {
                    return;
                }
                if (parameters == null)
                {
                    return;
                }

                try
                {
                    // Read settings from the DeviceActorServiceConfig section in the Settings.xml file
                    ICodePackageActivationContext activationContext = parameters.CodePackageActivationContext;
                    ConfigurationPackage config = activationContext.GetConfigurationPackageObject(ConfigurationPackage);
                    ConfigurationSection section = config.Settings.Sections[ConfigurationSection];

                    // Read the MessageBoxServiceUri setting from the Settings.xml file
                    if (section.Parameters.Any(
                        p => string.Compare(
                            p.Name,
                            MessageBoxServiceUriParameter,
                            StringComparison.InvariantCultureIgnoreCase) == 0))
                    {
                        ConfigurationProperty parameter = section.Parameters[MessageBoxServiceUriParameter];
                        if (!string.IsNullOrWhiteSpace(parameter?.Value))
                        {
                            MessageBoxServiceUri = new Uri(parameter.Value);
                        }
                    }
                    else
                    {
                        MessageBoxServiceUri = new Uri($"fabric:/{parameters.ServiceName.Segments[1]}MessageBoxService");
                    }
                    ActorEventSource.Current.Message($"[{MessageBoxServiceUriParameter}] = [{MessageBoxServiceUri}]");

                    // Read the RegistryServiceUri setting from the Settings.xml file
                    if (section.Parameters.Any(
                        p => string.Compare(
                            p.Name,
                            RegistryServiceUriParameter,
                            StringComparison.InvariantCultureIgnoreCase) == 0))
                    {
                        ConfigurationProperty parameter = section.Parameters[RegistryServiceUriParameter];
                        if (!string.IsNullOrWhiteSpace(parameter?.Value))
                        {
                            RegistryServiceUri = new Uri(parameter.Value);
                        }
                    }
                    else
                    {
                        RegistryServiceUri = new Uri($"fabric:/{parameters.ServiceName.Segments[1]}RegistryService");
                    }
                    ActorEventSource.Current.Message($"[{RegistryServiceUriParameter}] = [{RegistryServiceUri}]");

                    // Read the MaxQueryRetryCount setting from the Settings.xml file
                    if (section.Parameters.Any(
                        p => string.Compare(
                            p.Name,
                            MaxQueryRetryCountParameter,
                            StringComparison.InvariantCultureIgnoreCase) == 0))
                    {
                        ConfigurationProperty parameter = section.Parameters[MaxQueryRetryCount];
                        if (!string.IsNullOrWhiteSpace(parameter?.Value))
                        {
                            int.TryParse(parameter.Value, out MaxQueryRetryCount);
                        }
                    }
                    ActorEventSource.Current.Message($"[{MaxQueryRetryCountParameter}] = [{MaxQueryRetryCount}]");

                    // Read the MaxQueryRetryCount setting from the Settings.xml file
                    if (section.Parameters.Any(
                        p => string.Compare(
                            p.Name,
                            BackoffQueryDelayInSecondsParameter,
                            StringComparison.InvariantCultureIgnoreCase) == 0))
                    {
                        ConfigurationProperty parameter = section.Parameters[BackoffQueryDelayInSecondsParameter];
                        if (!string.IsNullOrWhiteSpace(parameter?.Value))
                        {
                            int value;
                            if (int.TryParse(parameter.Value, out value))
                            {
                                BackoffQueryDelay = TimeSpan.FromSeconds(value);
                            }
                        }
                    }
                    ActorEventSource.Current.Message($"[{BackoffQueryDelayInSecondsParameter}] = [{BackoffQueryDelay.TotalSeconds}]");
                }
                catch (KeyNotFoundException)
                {
                    RegistryServiceUri = new Uri($"fabric:/{parameters.ServiceName.Segments[1]}RegistryService");
                    ActorEventSource.Current.Message($"[{MessageBoxServiceUriParameter}] = [{MessageBoxServiceUri}]");
                    ActorEventSource.Current.Message($"[{RegistryServiceUriParameter}] = [{RegistryServiceUri}]");
                    ActorEventSource.Current.Message($"[{MaxQueryRetryCountParameter}] = [{MaxQueryRetryCount}]");
                    ActorEventSource.Current.Message($"[{BackoffQueryDelayInSecondsParameter}] = [{BackoffQueryDelay.TotalSeconds}]");
                }
                if (RegistryServiceUri == null)
                {
                    return;
                }
                FabricClient fabricClient = new FabricClient();

                ServicePartitionList list = fabricClient.QueryManager.GetPartitionListAsync(RegistryServiceUri).Result;
                RegistryServicePartitionCount = list != null && list.Any() ? list.Count : 1;
                ActorEventSource.Current.Message($"[{nameof(RegistryServicePartitionCount)}] = [{RegistryServicePartitionCount}]");

                list = fabricClient.QueryManager.GetPartitionListAsync(MessageBoxServiceUri).Result;
                MessageBoxServicePartitionCount = list != null && list.Any() ? list.Count : 1;
                ActorEventSource.Current.Message($"[{nameof(MessageBoxServicePartitionCount)}] = [{MessageBoxServicePartitionCount}]");
            }
        }

        #endregion

        #region Private Constants

        //************************************
        // Parameters
        //************************************
        private const string ConfigurationPackage = "Config";
        private const string ConfigurationSection = "FrameworkConfig";
        private const string MessageBoxServiceUriParameter = "MessageBoxServiceUri";
        private const string RegistryServiceUriParameter = "RegistryServiceUri";
        private const string MaxQueryRetryCountParameter = "MaxQueryRetryCount";
        private const string BackoffQueryDelayInSecondsParameter = "BackoffQueryDelayInSeconds";

        //************************************
        // Default Values
        //************************************
        private const int DefaultMaxQueryRetryCount = 20;
        private const int DefaultBackoffQueryDelayInSeconds = 1;

        #endregion

        #region Public Static Fields

        public static int MaxQueryRetryCount = DefaultMaxQueryRetryCount;
        public static TimeSpan BackoffQueryDelay = TimeSpan.FromSeconds(DefaultBackoffQueryDelayInSeconds);
        public static Uri MessageBoxServiceUri;
        public static Uri RegistryServiceUri;
        public static int RegistryServicePartitionCount;
        public static int MessageBoxServicePartitionCount;

        #endregion
    }
}