// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverActor
{
    using System;
    using System.Fabric;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.ServiceFabric.Actors.Runtime;

    #region Public Constructor
    public class TestObservableObserverActorService : ObservableObserverActorServiceBase
    {
        public TestObservableObserverActorService(StatefulServiceContext context,
                                                  ActorTypeInformation actorTypeInfo,
                                                  Func<ActorBase> actorFactory = null,
                                                  IActorStateProvider stateProvider = null,
                                                  ActorServiceSettings settings = null)
            : base(context, actorTypeInfo, actorFactory, stateProvider, settings)
        {
        }
    } 
    #endregion
}
