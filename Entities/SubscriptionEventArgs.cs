// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Entities
{
    using System;
    using System.Collections.Generic;

    public class SubscriptionEventArgs : EventArgs
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the NotificationEventArgs class.
        /// </summary>
        public SubscriptionEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the NotificationEventArgs class.
        /// </summary>
        /// <param name="topic">The notification topic.</param>
        /// <param name="entityId">The observable entity id.</param>
        public SubscriptionEventArgs(string topic, EntityId entityId)
        {
            this.EntityId = entityId;
            this.Topic = topic;
        }

        /// <summary>
        /// Initializes a new instance of the NotificationEventArgs class.
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
        /// Gets the observer entity id.
        /// </summary>
        public EntityId EntityId { get; private set; }

        /// <summary>
        /// Gets the filter expressions.
        /// </summary>
        public IEnumerable<string> FilterExpressions { get; private set; }

        /// <summary>
        /// Gets the notification topic.
        /// </summary>
        public string Topic { get; private set; }

        #endregion
    }
}