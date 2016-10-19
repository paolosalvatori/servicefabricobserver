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
    using System.Fabric;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    #endregion

    #region Public Constructor

    public class TestObservableObserverActorService : ObservableObserverActorServiceBase
    {
        public TestObservableObserverActorService(StatefulServiceContext context,
                                                  ActorTypeInformation typeInfo,
                                                  Func<ActorService, ActorId, ActorBase> actorFactory,
                                                  Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory,
                                                  IActorStateProvider stateProvider = null,
                                                  ActorServiceSettings settings = null)
            : base(context, typeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
        {
        }
    }

    #endregion
}