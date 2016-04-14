// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Entities
{
    using System;

    public class NotificationEventArgs<T> : EventArgs where T : Message, new()
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the NotificationEventArgs class.
        /// </summary>
        public NotificationEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the NotificationEventArgs class.
        /// </summary>
        /// <param name="topic">The notification topic.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="entityId">The observable entity id.</param>
        public NotificationEventArgs(string topic, T message, EntityId entityId)
        {
            this.EntityId = entityId;
            this.Topic = topic;
            this.Message = message;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the observable entity id.
        /// </summary>
        public EntityId EntityId { get; private set; }

        /// <summary>
        /// Gets the notification topic.
        /// </summary>
        public string Topic { get; private set; }

        /// <summary>
        /// Gets the notification message.
        /// </summary>
        public T Message { get; private set; }

        #endregion
    }
}