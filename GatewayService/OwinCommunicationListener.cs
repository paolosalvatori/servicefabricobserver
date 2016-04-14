// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.GatewayService
{
    using System;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Fabric.Query;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.Owin.Hosting;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    public class OwinCommunicationListener : ICommunicationListener
    {
        #region Public Constructor

        public OwinCommunicationListener(string appRoot, IOwinAppBuilder startup, StatelessServiceContext statelessServiceContext)
        {
            this.startup = startup;
            this.appRoot = appRoot;
            this.statelessServiceContext = statelessServiceContext;
        }

        #endregion

        #region Public Static Properties

        public static Uri RegistryServiceUri { get; private set; }
        public static Uri MessageBoxServiceUri { get; private set; }
        public static int RegistryServicePartitionCount { get; private set; }
        public static int MessageBoxServicePartitionCount { get; private set; }

        #endregion

        #region Private Methods

        private void StopWebServer()
        {
            if (this.serverHandle == null)
            {
                return;
            }
            try
            {
                this.serverHandle.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // no-op
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

        #endregion

        #region Private Fields

        private readonly IOwinAppBuilder startup;
        private readonly string appRoot;
        private readonly StatelessServiceContext statelessServiceContext;
        private IDisposable serverHandle;
        private string listeningAddress;

        #endregion

        #region ICommunicationListener Methods

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Read settings from the DeviceActorServiceConfig section in the Settings.xml file
                ICodePackageActivationContext activationContext = this.statelessServiceContext.CodePackageActivationContext;
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
                    MessageBoxServiceUri = new Uri($"fabric:/{this.statelessServiceContext.ServiceName.Segments[1]}MessageBoxService");
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
                    RegistryServiceUri = new Uri($"fabric:/{this.statelessServiceContext.ServiceName.Segments[1]}RegistryService");
                }
                ActorEventSource.Current.Message($"[{RegistryServiceUriParameter}] = [{RegistryServiceUri}]");

                FabricClient fabricClient = new FabricClient();

                ServicePartitionList list = fabricClient.QueryManager.GetPartitionListAsync(RegistryServiceUri).Result;
                RegistryServicePartitionCount = list != null && list.Any() ? list.Count : 1;
                ActorEventSource.Current.Message($"[{nameof(RegistryServicePartitionCount)}] = [{RegistryServicePartitionCount}]");

                list = fabricClient.QueryManager.GetPartitionListAsync(MessageBoxServiceUri).Result;
                MessageBoxServicePartitionCount = list != null && list.Any() ? list.Count : 1;
                ActorEventSource.Current.Message($"[{nameof(MessageBoxServicePartitionCount)}] = [{MessageBoxServicePartitionCount}]");

                EndpointResourceDescription serviceEndpoint = this.statelessServiceContext.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
                int port = serviceEndpoint.Port;

                this.listeningAddress = string.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}",
                    port,
                    String.IsNullOrWhiteSpace(this.appRoot)
                        ? String.Empty
                        : this.appRoot.TrimEnd('/') + '/');

                this.serverHandle = WebApp.Start(this.listeningAddress, appBuilder => this.startup.Configuration(appBuilder));
                string publishAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

                ServiceEventSource.Current.Message($"Listening on {publishAddress}");

                return Task.FromResult(publishAddress);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
                if (!string.IsNullOrWhiteSpace(ex.InnerException?.Message))
                {
                    ServiceEventSource.Current.Message(ex.InnerException.Message);
                }
                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.Message("Close");

            this.StopWebServer();

            return Task.FromResult(true);
        }

        public void Abort()
        {
            ServiceEventSource.Current.Message("Abort");

            this.StopWebServer();
        }

        #endregion
    }
}