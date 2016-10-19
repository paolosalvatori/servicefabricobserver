#region Copyright

// //=======================================================================================
// // Microsoft Azure Customer Advisory Team  
// //
// // This sample is supplemental to the technical guidance published on the community
// // blog at http://blogs.msdn.com/b/paolos/. 
// // 
// // Author: Paolo Salvatori
// //=======================================================================================
// // Copyright © 2016 Microsoft Corporation. All rights reserved.
// // 
// // THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// // EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// // MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
// //=======================================================================================

#endregion

namespace Microsoft.AzureCat.Samples.ObserverPattern.Framework
{
    #region Using Directives

    using System;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Microsoft.ServiceFabric.Actors.Runtime;

    #endregion

    [EventSource(Name = "ObserverPattern-Framework-Actor")]
    public sealed class ActorEventSource : EventSource
    {
        public static ActorEventSource Current = new ActorEventSource();

        [Event(1, Level = EventLevel.Informational, Message = "{0}")]
        public void Message(string message, [CallerFilePath] string source = "", [CallerMemberName] string method = "")
        {
            if (!this.IsEnabled())
                return;
            this.WriteEvent(1, $"[{GetClassFromFilePath(source) ?? "UNKNOWN"}::{method ?? "UNKNOWN"}] {message}");
        }

        [NonEvent]
        public void ActorMessage(Actor actor, string message, [CallerFilePath] string source = "", [CallerMemberName] string method = "", params object[] args)
        {
            if (!this.IsEnabled())
                return;
            string finalMessage = string.Format(message, args);
            this.ActorMessage(
                actor.GetType().ToString(),
                actor.Id.ToString(),
                actor.ActorService.Context.CodePackageActivationContext.ApplicationTypeName,
                actor.ActorService.Context.CodePackageActivationContext.ApplicationName,
                actor.ActorService.Context.ServiceTypeName,
                actor.ActorService.Context.ServiceName.ToString(),
                actor.ActorService.Context.PartitionId,
                actor.ActorService.Context.ReplicaId,
                actor.ActorService.Context.NodeContext.NodeName,
                GetClassFromFilePath(source) ?? "UNKNOWN",
                method ?? "UNKNOWN",
                finalMessage);
        }

        [NonEvent]
        public void ActorHostInitializationFailed(Exception e, [CallerFilePath] string source = "", [CallerMemberName] string method = "")
        {
            if (this.IsEnabled())
                this.ActorHostInitializationFailed(e.ToString(), GetClassFromFilePath(source) ?? "UNKNOWN", method ?? "UNKNOWN");
        }

        [NonEvent]
        public void Error(Exception e, [CallerFilePath] string source = "", [CallerMemberName] string method = "")
        {
            if (this.IsEnabled())
                this.Error($"[{GetClassFromFilePath(source) ?? "UNKNOWN"}::{method ?? "UNKNOWN"}] {e}");
        }

        [Event(2, Level = EventLevel.Informational, Message = "{11}")]
        private void ActorMessage(
            string actorType,
            string actorId,
            string applicationTypeName,
            string applicationName,
            string serviceTypeName,
            string serviceName,
            Guid partitionId,
            long replicaOrInstanceId,
            string nodeName,
            string source,
            string method,
            string message)
        {
            this.WriteEvent(
                2,
                actorType,
                actorId,
                applicationTypeName,
                applicationName,
                serviceTypeName,
                serviceName,
                partitionId,
                replicaOrInstanceId,
                nodeName,
                source,
                method,
                message);
        }

        [Event(3, Level = EventLevel.Error, Message = "Actor host initialization failed: {0}")]
        private void ActorHostInitializationFailed(string exception, string source, string method)
        {
            this.WriteEvent(3, exception, source, method);
        }

        [Event(5, Level = EventLevel.Error, Message = "An error occurred: {0}")]
        private void Error(string exception)
        {
            this.WriteEvent(5, exception);
        }

        private static string GetClassFromFilePath(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
                return null;
            FileInfo file = new FileInfo(sourceFilePath);
            return Path.GetFileNameWithoutExtension(file.Name);
        }
    }
}