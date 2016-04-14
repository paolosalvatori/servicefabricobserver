// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Framework
{
    using System;
    using System.Fabric;
    using Microsoft.ServiceFabric.Actors.Runtime;

    public abstract class ObservableActorServiceBase : ActorService
    {
        #region Protected Constructor
        protected ObservableActorServiceBase(StatefulServiceContext context,
                                             ActorTypeInformation actorTypeInfo,
                                             Func<ActorBase> actorFactory = null,
                                             IActorStateProvider stateProvider = null,
                                             ActorServiceSettings settings = null)
            : base(context, actorTypeInfo, actorFactory, stateProvider, settings)
        {
            ConfigurationHelper.Initialize(this.Context);
        } 
        #endregion
    }
}
