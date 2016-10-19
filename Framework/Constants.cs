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