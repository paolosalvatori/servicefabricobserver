// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Entities
{
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a message sent by an observable to its observers.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the message body.
        /// </summary>
        [JsonProperty(PropertyName = "body", Order = 1)]
        public string Body { get; set; }
    }
}