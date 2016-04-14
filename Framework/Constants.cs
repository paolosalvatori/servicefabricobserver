// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Framework
{
    public class Constants
    {
        #region Public Constants

        //************************************
        // Public Constants
        //************************************
        public const string RetryTimeoutExhausted = "Retry timeout exhausted.";
        public const string NamedPartitionsNotSupported = "Stateful services using named partitions are not currently supported.";
        public const string ServiceReplicaListenersCreated = "Service replica listeners created.";
        public const string EntityIdKey = "EntityId";
        public const string TopicDictionary = "Topic";
        public const string ObserverDictionary = "Observer";
        public const string EntityIdDictionary = "EntityId";
        #endregion
    }
}