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

namespace Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverActor
{
    #region Using Directives

    using System;
    using System.Threading;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.ServiceFabric.Actors.Runtime;

    #endregion

    internal static class Program
    {
        /// <summary>
        ///     This is the entry point of the service host process.
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
                        (s, i) => new TestObservableObserverActor(s, i),
                        null,
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