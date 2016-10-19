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

    #endregion

    public class NotificationEventArgs<T> : EventArgs where T : Message, new()
    {
        #region Public Constructors

        /// <summary>
        ///     Initializes a new instance of the NotificationEventArgs class.
        /// </summary>
        public NotificationEventArgs()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the NotificationEventArgs class.
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
        ///     Gets the observable entity id.
        /// </summary>
        public EntityId EntityId { get; private set; }

        /// <summary>
        ///     Gets the notification topic.
        /// </summary>
        public string Topic { get; private set; }

        /// <summary>
        ///     Gets the notification message.
        /// </summary>
        public T Message { get; private set; }

        #endregion
    }
}