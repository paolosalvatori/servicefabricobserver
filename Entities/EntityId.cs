// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Entities
{
    using System;
    using System.Fabric;
    using System.Runtime.Serialization;
    using Microsoft.ServiceFabric.Actors;
    using Newtonsoft.Json;

    [DataContract]
    public class EntityId
    {
        #region Public Methods

        public override string ToString()
        {
            return this.Kind == EntityKind.Actor
                ? $"Id=[{this.ActorId}] ServiceUri=[{this.ServiceUri}] Nodename=[{this.NodeName}] Kind=[{this.Kind}]"
                : this.PartitionKey.HasValue
                    ? $"Id=[{this.PartitionKey}] ServiceUri=[{this.ServiceUri}] NodeName=[{this.NodeName}] Kind=[{this.Kind}]"
                    : $"ServiceUri=[{this.ServiceUri}] Nodename=[{this.NodeName?? "UNKNOWN"}] Kind=[{this.Kind}]";
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        public EntityId()
        {
        }

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        /// <param name="actorId">The entity ActorId.</param>
        /// <param name="serviceUri">The service URI.</param>
        public EntityId(ActorId actorId, Uri serviceUri)
        {
            this.ActorId = actorId;
            this.ServiceUri = serviceUri;
            this.Kind = EntityKind.Actor;
            try
            {
                this.NodeName = FabricRuntime.GetNodeContext().NodeName;
            }
            catch (Exception)
            {
                this.NodeName = "UNKNOWN";
            }
        }

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        /// <param name="actorId">The entity ActorId.</param>
        /// <param name="serviceUri">The service URI.</param>
        public EntityId(string actorId, Uri serviceUri)
        {
            this.ActorId = new ActorId(actorId);
            this.ServiceUri = serviceUri;
            this.Kind = EntityKind.Actor;
            try
            {
                this.NodeName = FabricRuntime.GetNodeContext().NodeName;
            }
            catch (Exception)
            {
                this.NodeName = "UNKNOWN";
            }
        }

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        /// <param name="serviceUri">The service URI.</param>
        public EntityId(Uri serviceUri)
        {
            this.ServiceUri = serviceUri;
            this.Kind = EntityKind.Service;
            try
            {
                this.NodeName = FabricRuntime.GetNodeContext().NodeName;
            }
            catch (Exception)
            {
                this.NodeName = "UNKNOWN";
            }
        }

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        /// <param name="partitionKey">The entity PartitionKey.</param>
        /// <param name="serviceUri">The service URI.</param>
        public EntityId(long partitionKey, Uri serviceUri)
        {
            this.PartitionKey = partitionKey;
            this.ServiceUri = serviceUri;
            this.Kind = EntityKind.Service;
            try
            {
                this.NodeName = FabricRuntime.GetNodeContext().NodeName;
            }
            catch (Exception)
            {
                this.NodeName = "UNKNOWN";
            }
        }

        /// <summary>
        /// Initializes a new instance of the EntityId class.
        /// </summary>
        /// <param name="shortEntityId">ShortEntityId parameter.</param>
        public EntityId(ShortEntityId shortEntityId)
        {
            if (!string.IsNullOrWhiteSpace(shortEntityId?.ActorId))
            {
                this.ActorId = new ActorId(shortEntityId.ActorId);
            }
            this.PartitionKey = shortEntityId.PartitionKey;
            this.ServiceUri = shortEntityId.ServiceUri;
            this.Kind = shortEntityId.Kind;
            try
            {
                this.NodeName = FabricRuntime.GetNodeContext().NodeName;
            }
            catch (Exception)
            {
                this.NodeName = "UNKNOWN";
            }
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the actor id. This field is empty in case of a service.
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "actorId", Order = 1)]
        public ActorId ActorId { get; set; }

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
        /// Gets the entity node name.
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "nodeName", Order = 4)]
        public string NodeName { get; private set; }

        /// <summary>
        /// Gets the entity type.
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "kind", Order = 5)]
        public EntityKind Kind { get; private set; }

        [JsonIgnore]
        public Uri EntityUri => this.Kind == EntityKind.Actor
            ? new Uri(Combine(this.ServiceUri, this.ActorId.ToString()))
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