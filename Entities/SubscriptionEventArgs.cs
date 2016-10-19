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

namespace Microsoft.AzureCat.Samples.ObserverPattern.Entities
{
    #region Using Directives

    using System;
    using System.Collections.Generic;

    #endregion

    public class SubscriptionEventArgs : EventArgs
    {
        #region Public Constructors

        /// <summary>
        ///     Initializes a new instance of the NotificationEventArgs class.
        /// </summary>
        public SubscriptionEventArgs()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the NotificationEventArgs class.
        /// </summary>
        /// <param name="topic">The notification topic.</param>
        /// <param name="entityId">The observable entity id.</param>
        public SubscriptionEventArgs(string topic, EntityId entityId)
        {
            this.EntityId = entityId;
            this.Topic = topic;
        }

        /// <summary>
        ///     Initializes a new instance of the NotificationEventArgs class.
        /// </summary>
        /// <param name="topic">The notification topic.</param>
        /// <param name="filterExpressions">Specifies filter expressions.</param>
        /// <param name="entityId">The observable entity id.</param>
        public SubscriptionEventArgs(string topic, IEnumerable<string> filterExpressions, EntityId entityId)
        {
            this.EntityId = entityId;
            this.FilterExpressions = filterExpressions;
            this.Topic = topic;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the observer entity id.
        /// </summary>
        public EntityId EntityId { get; private set; }

        /// <summary>
        ///     Gets the filter expressions.
        /// </summary>
        public IEnumerable<string> FilterExpressions { get; private set; }

        /// <summary>
        ///     Gets the notification topic.
        /// </summary>
        public string Topic { get; private set; }

        #endregion
    }
}