// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverActor
{
    using System;
    using System.Threading;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.ServiceFabric.Actors.Runtime;

    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // Create default garbage collection settings for all the actor types
                ActorGarbageCollectionSettings actorGarbageCollectionSettings = new ActorGarbageCollectionSettings(300, 60);

                // This line registers your actor class with the Fabric Runtime.
                // The contents of your ServiceManifest.xml and ApplicationManifest.xml files
                // are automatically populated when you build this project.
                // For more information, see http://aka.ms/servicefabricactorsplatform

                ActorRuntime.RegisterActorAsync<TestObservableObserverActor>(
                    (context, actorType) => new TestObservableObserverActorService(
                        context,
                        actorType,
                        () => new TestObservableObserverActor(),
                        null,
                        new ActorServiceSettings
                        {
                            ActorGarbageCollectionSettings = actorGarbageCollectionSettings
                        })).GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e);
                throw;
            }
        }
    }
}