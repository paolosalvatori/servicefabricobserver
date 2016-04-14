// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Framework
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Fabric;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Microsoft.ServiceFabric.Services.Runtime;

    [EventSource(Name = "ObserverPattern-Framework-Service")]
    public sealed class ServiceEventSource : EventSource
    {
        public static ServiceEventSource Current = new ServiceEventSource();

        [Event(1, Level = EventLevel.Informational, Message = "{0}")]
        public void Message(string message, [CallerFilePath] string source = "", [CallerMemberName] string method = "")
        {
            if (!this.IsEnabled())
            {
                return;
            }
            this.WriteEvent(1, $"[{GetClassFromFilePath(source) ?? "UNKNOWN"}::{method ?? "UNKNOWN"}] {message}");
        }

        [NonEvent]
        public void ServiceMessage(StatelessService service, string message, params object[] args)
        {
            if (!this.IsEnabled())
            {
                return;
            }
            string finalMessage = string.Format(message, args);
            this.ServiceMessage(
                service.Context.ServiceName.ToString(),
                service.Context.ServiceTypeName,
                service.Context.InstanceId,
                service.Context.PartitionId,
                service.Context.CodePackageActivationContext.ApplicationName,
                service.Context.CodePackageActivationContext.ApplicationTypeName,
                FabricRuntime.GetNodeContext().NodeName,
                finalMessage);
        }

        [NonEvent]
        public void ServiceMessage(StatefulService service, string message, params object[] args)
        {
            if (!this.IsEnabled())
            {
                return;
            }
            string finalMessage = string.Format(message, args);
            this.ServiceMessage(
                service.Context.ServiceName.ToString(),
                service.Context.ServiceTypeName,
                service.Context.ReplicaId,
                service.Context.PartitionId,
                service.Context.CodePackageActivationContext.ApplicationName,
                service.Context.CodePackageActivationContext.ApplicationTypeName,
                FabricRuntime.GetNodeContext().NodeName,
                finalMessage);
        }

        [Event(3, Level = EventLevel.Informational, Message = "Service host process {0} registered service type {1}")]
        public void ServiceTypeRegistered(int hostProcessId, string serviceType)
        {
            this.WriteEvent(3, hostProcessId, serviceType);
        }

        [NonEvent]
        public void ServiceHostInitializationFailed(Exception e)
        {
            this.ServiceHostInitializationFailed(e.ToString());
        }

        [NonEvent]
        public void Error(Exception e, [CallerFilePath] string source = "", [CallerMemberName] string method = "")
        {
            if (this.IsEnabled())
            {
                this.Error($"[{GetClassFromFilePath(source) ?? "UNKNOWN"}::{method ?? "UNKNOWN"}] {e}");
            }
        }

        [Event(2, Level = EventLevel.Informational, Message = "{7}")]
        private void ServiceMessage(
            string serviceName,
            string serviceTypeName,
            long replicaOrInstanceId,
            Guid partitionId,
            string applicationName,
            string applicationTypeName,
            string nodeName,
            string message)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(2, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, nodeName, message);
            }
        }

        [Event(4, Level = EventLevel.Error, Message = "Service host initialization failed")]
        private void ServiceHostInitializationFailed(string exception)
        {
            this.WriteEvent(4, exception);
        }

        [Event(5, Level = EventLevel.Error, Message = "An error occurred: {0}")]
        private void Error(string exception)
        {
            this.WriteEvent(5, exception);
        }

        private static string GetClassFromFilePath(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
            {
                return null;
            }
            FileInfo file = new FileInfo(sourceFilePath);
            return Path.GetFileNameWithoutExtension(file.Name);
        }
    }
}