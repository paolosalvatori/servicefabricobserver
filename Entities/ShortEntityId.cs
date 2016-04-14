// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Entities
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    public class ShortEntityId
    {
        #region Public Methods

        public override string ToString()
        {
            return this.Kind == EntityKind.Actor
                ? $"Id=[{this.ActorId}] ServiceUri=[{this.ServiceUri}] Kind=[{this.Kind}]"
                : this.PartitionKey.HasValue
                    ? $"Id=[{this.PartitionKey}] ServiceUri=[{this.ServiceUri}] Kind=[{this.Kind}]"
                    : $"ServiceUri=[{this.ServiceUri}] Kind=[{this.Kind}]";
        }

        #endregion
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        public ShortEntityId()
        {
        }

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        /// <param name="actorId">The entity ActorId.</param>
        /// <param name="serviceUri">The service URI.</param>
        public ShortEntityId(string actorId, Uri serviceUri)
        {
            this.ActorId = actorId;
            this.ServiceUri = serviceUri;
            this.Kind = EntityKind.Actor;
        }

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        /// <param name="serviceUri">The service URI.</param>
        public ShortEntityId(Uri serviceUri)
        {
            this.ServiceUri = serviceUri;
            this.Kind = EntityKind.Service;
        }

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        /// <param name="partitionKey">The entity PartitionKey.</param>
        /// <param name="serviceUri">The service URI.</param>
        public ShortEntityId(long partitionKey, Uri serviceUri)
        {
            this.PartitionKey = partitionKey;
            this.ServiceUri = serviceUri;
            this.Kind = EntityKind.Service;
        }

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        /// <param name="entityId">An EntityId.</param>
        public ShortEntityId(EntityId entityId)
        {
            this.ServiceUri = entityId.ServiceUri;
            this.Kind = entityId.Kind;
            this.ActorId = entityId.ActorId?.ToString();
            this.PartitionKey = entityId.PartitionKey;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the actor id. This field is empty in case of a service.
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "actorId", Order = 1)]
        public string ActorId { get; set; }

        /// <summary>
        /// Gets or sets the partition key. This field is empty in case of an actor.
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "partitionKey", Order = 2)]
        public long? PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the service URI;
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "serviceUri", Order = 3)]
        public Uri ServiceUri { get; set; }

        /// <summary>
        /// Gets the entity type.
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "kind", Order = 5)]
        public EntityKind Kind { get; private set; }


        [JsonIgnore]
        public Uri EntityUri => this.Kind == EntityKind.Actor
            ? new Uri(Combine(this.ServiceUri, this.ActorId))
            : this.PartitionKey.HasValue
                ? new Uri(Combine(this.ServiceUri, this.PartitionKey.ToString()))
                : this.ServiceUri;

        #endregion

        #region Private Static Methods

        private static string Combine(Uri uri1, string uri2)
        {
            return Combine(uri1.AbsoluteUri, uri2);
        }

        private static string Combine(string uri1, string uri2)
        {
            uri1 = uri1.TrimEnd('/');
            uri2 = uri2.TrimStart('/');
            return $"{uri1}/{uri2}";
        }

        #endregion
    }
}