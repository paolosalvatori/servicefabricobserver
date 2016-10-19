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

    using System.Collections.Generic;
    using Newtonsoft.Json;

    #endregion

    /// <summary>
    ///     Represents a gateway request
    /// </summary>
    public class GatewayRequest
    {
        /// <summary>
        ///     Gets or sets the topic.
        /// </summary>
        [JsonProperty(PropertyName = "topic", Order = 1)]
        public string Topic { get; set; }

        /// <summary>
        ///     Gets or sets the message.
        /// </summary>
        [JsonProperty(PropertyName = "messages", Order = 2)]
        public IEnumerable<Message> Messages { get; set; }

        /// <summary>
        ///     Gets or sets the entityId of an observable.
        /// </summary>
        [JsonProperty(PropertyName = "observableEntityId", Order = 3)]
        public ShortEntityId ObservableEntityId { get; set; }

        /// <summary>
        ///     Gets or sets the entityId of an observer.
        /// </summary>
        [JsonProperty(PropertyName = "observerEntityId", Order = 4)]
        public ShortEntityId ObserverEntityId { get; set; }

        /// <summary>
        ///     Gets or sets a boolean value that indicates the proxoy behavior:
        ///     the observable uses one observer for each cluster node
        ///     as a proxy when true, it directly sends the message to all observers otherwise.
        /// </summary>
        [JsonProperty(PropertyName = "useObserverAsProxy", Order = 5)]
        public bool UseObserverAsProxy { get; set; }

        /// <summary>
        ///     Gets or sets a collection of filter expressions.
        /// </summary>
        [JsonProperty(PropertyName = "filterExpressions", Order = 6)]
        public IEnumerable<string> FilterExpressions { get; set; }
    }
}