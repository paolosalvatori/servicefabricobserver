// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Microsoft.AzureCat.Samples.ObserverPattern.Entities
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a gateway request
    /// </summary>
    public class GatewayRequest
    {
        /// <summary>
        /// Gets or sets the topic.
        /// </summary>
        [JsonProperty(PropertyName = "topic", Order = 1)]
        public string Topic { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [JsonProperty(PropertyName = "messages", Order = 2)]
        public IEnumerable<Message> Messages { get; set; }

        /// <summary>
        /// Gets or sets the entityId of an observable.
        /// </summary>
        [JsonProperty(PropertyName = "observableEntityId", Order = 3)]
        public ShortEntityId ObservableEntityId { get; set; }

        /// <summary>
        /// Gets or sets the entityId of an observer.
        /// </summary>
        [JsonProperty(PropertyName = "observerEntityId", Order = 4)]
        public ShortEntityId ObserverEntityId { get; set; }

        /// <summary>
        /// Gets or sets a boolean value that indicates the proxoy behavior:
        /// the observable uses one observer for each cluster node 
        /// as a proxy when true, it directly sends the message to all observers otherwise.
        /// </summary>
        [JsonProperty(PropertyName = "useObserverAsProxy", Order = 5)]
        public bool UseObserverAsProxy { get; set; }

        /// <summary>
        /// Gets or sets a collection of filter expressions.
        /// </summary>
        [JsonProperty(PropertyName = "filterExpressions", Order = 6)]
        public IEnumerable<string> FilterExpressions { get; set; }
    }
}