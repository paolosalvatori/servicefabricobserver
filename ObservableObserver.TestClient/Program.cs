// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.TestClient
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Fabric;
    using System.Fabric.Query;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
    using Microsoft.AzureCat.Samples.ObserverPattern.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Query;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Newtonsoft.Json;

    public class Program
    {
        #region Private Static Fields
        private static FabricClient FabricClient = new FabricClient();
        private static List<Test> TestList = new List<Test>
                {
                    new Test
                    {
                        Name = "IoT Test Via Proxy",
                        Description = "Simulates an IoT scenario with multiple observables and observers.",
                        Action = IoTTestViaProxy
                    },
                    new Test
                    {
                        Name = "IoT Test Via Gateway",
                        Description = "Simulates an IoT scenario with multiple observables and observers.",
                        Action = IoTTestViaGateway
                    },
                    new Test
                    {
                        Name = "Stock Market Test Via Proxy",
                        Description = "Simulates a stock trading scenario.",
                        Action = StockMarketTestViaProxy
                    },
                    new Test
                    {
                        Name = "Stock Market Test Via Gateway",
                        Description = "Simulates a stock trading scenario.",
                        Action = StockMarketTestViaGateway
                    },
                    new Test
                    {
                        Name = "MessageBox Test Via Proxy",
                        Description = "Simulates an observer reading messages from its messagebox.",
                        Action = MessageBoxTestViaProxy
                    },
                    new Test
                    {
                        Name = "MessageBox Test Via Gateway",
                        Description = "Simulates an observer reading messages from its messagebox.",
                        Action = MessageBoxTestViaGateway
                    },
                    new Test
                    {
                        Name = "Enumerate Actors Via Proxy",
                        Description = "Enumerates TestObservableObserverActor actors.",
                        Action = EnumerateActorsViaActorProxy
                    },
                    new Test
                    {
                        Name = "Delete Actors Via Proxy",
                        Description = "Deletes TestObservableObserverActor actors.",
                        Action = DeleteActorsViaActorProxy
                    }
                };
        
        private static string Line = new string('-', 139);
        private static string GatewayUrl;
        private static Uri TestObservableObserverActorUri;
        private static Uri TestObservableObserverServiceUri;
        #endregion

        #region Main Method
        public static void Main(string[] args)
        {
            try
            {
                // Set window size and cursor color
                Console.SetWindowSize(140, 50);
                Console.ForegroundColor = ConsoleColor.White;

                // Reads configuration settings
                ReadConfiguration();

                // Sets actor service URIs
                TestObservableObserverActorUri = new Uri($"{ApplicationUri}{TestObservableObserverActor}");
                TestObservableObserverServiceUri = new Uri($"{ApplicationUri}{TestObservableObserverService}");

                int i;
                while ((i = SelectOption()) != TestList.Count + 1)
                {
                    try
                    {
                        TestList[i - 1].Action();
                    }
                    catch (Exception ex)
                    {
                        PrintException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }
        #endregion

        #region Test Methods

        private static void IoTTestViaGateway()
        {
            // Write Line
            Console.WriteLine(Line);

            // Create ShortEntityId for observable service
            ShortEntityId observableObserverServiceEntityId = new ShortEntityId(1, new Uri($"{ApplicationUri}{TestObservableObserverService}"));
            Console.WriteLine(" - ShortEntityId for the observable service partition created.");

            // Create request message for observable service
            GatewayRequest gatewayRequest = new GatewayRequest
            {
                ObservableEntityId = observableObserverServiceEntityId,
                UseObserverAsProxy = true
            };
            
            // Clear all observers and publications for observable service partition
            SendRequestToGateway(gatewayRequest, "api/observable/service/clear");
            Console.WriteLine(" - All observers and publications cleared for the observable service partition.");

            // Register observable service partition
            string topic = "Rome";
            gatewayRequest.Topic = topic;
            SendRequestToGateway(gatewayRequest, "api/observable/service/register");
            Console.Write(" - Service partition registered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            // Create ShortEntityId for observable actor
            ShortEntityId observableObserverActorEntityId = new ShortEntityId("MilanSite", TestObservableObserverActorUri);
            Console.WriteLine(" - ShortEntityId for the observable actor created.");

            // Create request message for observable service
            gatewayRequest = new GatewayRequest
            {
                ObservableEntityId = observableObserverActorEntityId,
                UseObserverAsProxy = true
            };
            
            // Clear all observers and publications for observable actor
            SendRequestToGateway(gatewayRequest, "api/observable/actor/clear");
            Console.WriteLine(" - All observers and publications cleared for the observable actor.");

            // Register observable actor
            topic = "Milan";
            gatewayRequest.Topic = topic;
            SendRequestToGateway(gatewayRequest, "api/observable/actor/register");
            Console.Write(" - Actor registered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            // Retrieve observables by topic from the registry
            string[] topicArray = { "Milan", "Rome" };
            foreach (string t in topicArray)
            {
                gatewayRequest = new GatewayRequest
                {
                    Topic = t
                };
                HttpResponseMessage response = SendRequestToGateway(gatewayRequest, "api/registry/service/get");
                string json = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }
                IEnumerable<ShortEntityId> observableList = JsonConvert.DeserializeObject<IEnumerable<ShortEntityId>>(json);
                Console.Write(" - Observables for [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{t}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("] topic:");
                foreach (ShortEntityId entity in observableList)
                {
                    Console.Write("   > ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(entity.ToString());
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            // Define filer Expressions
            const string filterExpressionForId10 = "id != null and value != null and id = 10";
            const string filterExpressionForId20 = "id != null and value != null and id = 20";
            const string filterExpressionForId50 = "id != null and value != null and id = 50";
            const string filterExpressionForId60 = "id != null and value != null and id = 60";

            // Register actor observers:
            // - topic = Milan and observable = observableObserverActorIoT
            // - topic = Rome and observable =  observableObserverServiceIoT
            topic = "Milan";

            // Register observers from P to Y on topic Milan using filterExpressionForId10 as filter expression
            foreach (char c in "PQRSTUVWXY")
            {
                gatewayRequest = new GatewayRequest
                {
                    Topic = topic,
                    FilterExpressions = new List<string> { filterExpressionForId10 },
                    ObserverEntityId = new ShortEntityId(c.ToString(), TestObservableObserverActorUri),
                    ObservableEntityId = observableObserverActorEntityId
                };
                SendRequestToGateway(gatewayRequest, "api/observer/actor/register");
            }
            
            // Z is using a different filter expression.
            // So when the observable actor will send a JSON message,
            // Z won't receive the message as its filter expression is not satisfied by the JSON message

            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                FilterExpressions = new List<string> { filterExpressionForId20 },
                ObserverEntityId = new ShortEntityId("Z", TestObservableObserverActorUri),
                ObservableEntityId = observableObserverActorEntityId
            };
            SendRequestToGateway(gatewayRequest, "api/observer/actor/register");

            Console.Write(" - Observer actors registered: Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] Observable = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{observableObserverActorEntityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // Register observers from P to Y on topic Rome using filterExpressionForId50 as filter expression
            topic = "Rome";
            foreach (char c in "PQRSTUVWXY")
            {
                gatewayRequest = new GatewayRequest
                {
                    Topic = topic,
                    FilterExpressions = new List<string> { filterExpressionForId50 },
                    ObserverEntityId = new ShortEntityId(c.ToString(), TestObservableObserverActorUri),
                    ObservableEntityId = observableObserverServiceEntityId
                };
                SendRequestToGateway(gatewayRequest, "api/observer/actor/register");
            }

            // Z is using a different filter expression.
            // So when the observable service will send a JSON message,
            // Z won't receive the message as its filter expression is not satisfied by the JSON message

            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                FilterExpressions = new List<string> { filterExpressionForId60 },
                ObserverEntityId = new ShortEntityId("Z", TestObservableObserverActorUri),
                ObservableEntityId = observableObserverServiceEntityId
            };
            SendRequestToGateway(gatewayRequest, "api/observer/actor/register");

            Console.Write(" - Observer actors registered: Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] Observable = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{observableObserverServiceEntityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // The observableObserverActorIoT observable actor sends a JSON message
            topic = "Milan";
            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                Messages = new[]
                {
                    new Message{Body = "{'id': 10, 'value': 52}"},
                    new Message{Body = "{'id': 10, 'value': 64}"}
                },
                ObservableEntityId = observableObserverActorEntityId,
                UseObserverAsProxy = true
            };
            SendRequestToGateway(gatewayRequest, "api/observable/actor/notify");

            Console.Write(" - Observable actor has sent the following messages. Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] Messages:");
            foreach (Message message in gatewayRequest.Messages)
            {
                Console.Write("   > [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{message.Body}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("]");
            }

            // The observableObserverService observable sends a JSON message
            topic = "Rome";
            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                Messages = new[]
                {
                    new Message{Body = "{'id': 50, 'value': 42}"},
                    new Message{Body = "{'id': 50, 'value': 48}"}
                },
                ObservableEntityId = observableObserverServiceEntityId,
                UseObserverAsProxy = true
            };
            SendRequestToGateway(gatewayRequest, "api/observable/service/notify");

            Console.Write(" - Observable service partition has sent the following messages. Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] Messages:");
            foreach (Message message in gatewayRequest.Messages)
            {
                Console.Write("   > [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{message.Body}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("]");
            }

            // Unregister one of the observers
            topic = "Milan";
            ShortEntityId entityId = new ShortEntityId("X", TestObservableObserverActorUri);
            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                ObserverEntityId = entityId,
                ObservableEntityId = observableObserverActorEntityId,
                UseObserverAsProxy = true
            };
            SendRequestToGateway(gatewayRequest, "api/observer/actor/unregister");

            Console.Write(" - Observer unregistered: Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] Observer = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{entityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // The observable sends a text, non-JSON message.
            // actor X is no longer receiving messages as it unregistered as observer of observable A for Milan topic.
            // Note: the message is not in JSON format, hence the message is broadcasted to all observers as filter expressions
            //       are not evaluated.
            topic = "Milan";
            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                Messages = new[]
                {
                    new Message{Body = "This is a NON-JSON message"},
                    new Message{Body = "This is another NON-JSON message"}
                },
                ObservableEntityId = observableObserverActorEntityId,
                UseObserverAsProxy = true
            };
            SendRequestToGateway(gatewayRequest, "api/observable/actor/notify");

            Console.Write(" - Observable actor has sent the following messages. Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] Messages:");
            foreach (Message message in gatewayRequest.Messages)
            {
                Console.Write("   > [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{message.Body}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("]");
            }

            // Unregister the observables
            topic = "Milan";
            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                ObservableEntityId = observableObserverActorEntityId
            };
            SendRequestToGateway(gatewayRequest, "api/observable/actor/unregister");

            Console.Write(" - Actor unregistered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            topic = "Rome";
            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                ObservableEntityId = observableObserverServiceEntityId,
                UseObserverAsProxy = true
            };
            SendRequestToGateway(gatewayRequest, "api/observable/service/unregister");

            Console.Write(" - Service partition unregistered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            // Write Line
            Console.WriteLine(Line);
        }

        private static void IoTTestViaProxy()
        {
            // Write Line
            Console.WriteLine(Line);

            // Create observable service proxy
            IClientObservableService observableObserverServiceIoT = ServiceProxy.Create<IClientObservableService>(TestObservableObserverServiceUri, new ServicePartitionKey(1));
            Console.WriteLine(" - ServiceProxy for the observable service partition created.");

            // Clear all observers and publications.
            observableObserverServiceIoT.ClearObserversAndPublicationsAsync(true).Wait();
            Console.WriteLine(" - All observers and publications cleared for the observable service partition.");

            // Register observable service
            string topic = "Rome";
            observableObserverServiceIoT.RegisterObservableServiceAsync(topic).Wait();
            Console.Write(" - Service partition registered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            // Create observable actor proxy
            IClientObservableObserverActor observableObserverActorIoT = ActorProxy.Create<IClientObservableObserverActor>(new ActorId("MilanSite"), TestObservableObserverActorUri);
            Console.WriteLine(" - ActorProxy for the observable actor created.");

            // Clear all observers and publications.
            observableObserverActorIoT.ClearObserversAndPublicationsAsync(true).Wait();
            Console.WriteLine(" - All observers and publications cleared for the observable actor.");

            // Register observable actors
            topic = "Milan";
            observableObserverActorIoT.RegisterObservableActorAsync(topic).Wait();
            Console.Write(" - Actor registered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            // Retrieve observables by topic from the registry
            Uri registryServiceUri = new Uri($"{ApplicationUri}{RegistryServiceUri}");
            ServicePartitionList servicePartitionList = FabricClient.QueryManager.GetPartitionListAsync(registryServiceUri).Result;
            int registryServicePartitionCount = servicePartitionList != null && servicePartitionList.Any() ? servicePartitionList.Count : 1;
            string[] topicArray = { "Milan", "Rome" };
            foreach (string t in topicArray)
            {
                IRegistryService registryServiceProxy = ServiceProxy.Create<IRegistryService>(registryServiceUri, new ServicePartitionKey(PartitionResolver.Resolve(t, registryServicePartitionCount)));
                IEnumerable<EntityId> observableList = registryServiceProxy.QueryObservablesAsync(t, null).Result;
                Console.Write(" - Observables for [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{t}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("] topic:");
                foreach (EntityId entity in observableList)
                {
                    Console.Write("   > ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(entity.ToString());
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            // Create observer actor proxies
            IClientObserverActor observableObserverActorP = ActorProxy.Create<IClientObserverActor>(new ActorId("P"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorQ = ActorProxy.Create<IClientObserverActor>(new ActorId("Q"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorR = ActorProxy.Create<IClientObserverActor>(new ActorId("R"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorS = ActorProxy.Create<IClientObserverActor>(new ActorId("S"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorT = ActorProxy.Create<IClientObserverActor>(new ActorId("T"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorU = ActorProxy.Create<IClientObserverActor>(new ActorId("U"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorV = ActorProxy.Create<IClientObserverActor>(new ActorId("V"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorW = ActorProxy.Create<IClientObserverActor>(new ActorId("W"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorX = ActorProxy.Create<IClientObserverActor>(new ActorId("X"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorY = ActorProxy.Create<IClientObserverActor>(new ActorId("Y"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorZ = ActorProxy.Create<IClientObserverActor>(new ActorId("Z"), TestObservableObserverActorUri);
            Console.WriteLine(" - ActorProxy for the observer actors created.");

            // Get observables identity
            EntityId observableObserverActorIoTEntityId = observableObserverActorIoT.GetEntityIdAsync().Result;
            Console.WriteLine(" - EntityId for the observable service partition retrieved.");

            EntityId observableObserverServiceIoTEntityId = observableObserverServiceIoT.GetEntityIdAsync().Result;
            Console.WriteLine(" - EntityId for the observable actor retrieved.");

            // Define filer Expressions
            const string filterExpressionForId10 = "id != null and value != null and id = 10";
            const string filterExpressionForId20 = "id != null and value != null and id = 20";
            const string filterExpressionForId50 = "id != null and value != null and id = 50";
            const string filterExpressionForId60 = "id != null and value != null and id = 60";

            // Register actor observers:
            // - topic = Milan and observable = observableObserverActorIoT
            // - topic = Rome and observable =  observableObserverServiceIoT
            topic = "Milan";

            observableObserverActorP.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId10 }, observableObserverActorIoTEntityId).Wait();
            observableObserverActorQ.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId10 }, observableObserverActorIoTEntityId).Wait();
            observableObserverActorR.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId10 }, observableObserverActorIoTEntityId).Wait();
            observableObserverActorS.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId10 }, observableObserverActorIoTEntityId).Wait();
            observableObserverActorT.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId10 }, observableObserverActorIoTEntityId).Wait();
            observableObserverActorU.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId10 }, observableObserverActorIoTEntityId).Wait();
            observableObserverActorV.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId10 }, observableObserverActorIoTEntityId).Wait();
            observableObserverActorW.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId10 }, observableObserverActorIoTEntityId).Wait();
            observableObserverActorX.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId10 }, observableObserverActorIoTEntityId).Wait();
            observableObserverActorY.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId10 }, observableObserverActorIoTEntityId).Wait();

            // When the observable for Milan topic will send a JSON message,
            // Z won't receive the message as its filter expression is not satisfied by the JSON message
            observableObserverActorZ.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId20 }, observableObserverActorIoTEntityId).Wait();

            Console.Write(" - Observer actors registered: Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] Observable = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{observableObserverActorIoTEntityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            topic = "Rome";

            observableObserverActorP.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();
            observableObserverActorQ.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();
            observableObserverActorR.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();
            observableObserverActorS.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();
            observableObserverActorT.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();
            observableObserverActorU.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();
            observableObserverActorV.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();
            observableObserverActorW.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();
            observableObserverActorX.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();
            observableObserverActorY.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();

            // The observableObserverActorIoT observable actor for Milan topic registers as an observer:
            // - topic = Rome and observable =  observableObserverServiceIoT
            observableObserverActorIoT.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId50 }, observableObserverServiceIoTEntityId).Wait();

            // When the observable for Rome topic will send a JSON message,
            // Z won't receive the message as its filter expression is not satisfied by the JSON message
            observableObserverActorZ.RegisterObserverActorAsync(topic, new List<string> { filterExpressionForId60 }, observableObserverServiceIoTEntityId).Wait();

            Console.Write(" - Observer actors registered: Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] Observable = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{observableObserverServiceIoTEntityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // The observableObserverActorIoT observable actor sends a JSON message
            topic = "Milan";
            Message message = new Message { Body = "{'id': 10, 'value': 52}" };
            observableObserverActorIoT.NotifyObserversAsync("Milan", message, true).Wait();

            Console.Write(" - Observable actor has sent a message. Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] Messages = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{message.Body}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // The observableObserverServiceIoT observable service sends a JSON message
            topic = "Rome";
            message = new Message { Body = "{'id': 50, 'value': 52}" };
            observableObserverServiceIoT.NotifyObserversAsync(topic, message, true).Wait();

            Console.Write(" - Observable service partition has sent a message. Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] Messages = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{message.Body}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // Unregister one of the observers
            topic = "Milan";
            observableObserverActorX.UnregisterObserverActorAsync(topic, observableObserverActorIoTEntityId).Wait();

            Console.Write(" - Actor unregistered as an observer: Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] Observer = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{new EntityId(new ActorId("X"), TestObservableObserverActorUri).EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // The observable sends a text, non-JSON message.
            // observableObserverActorX is no longer receiving messages as it unregistered as observer of observable A for Milan topic.
            // Note: the message is not in JSON format, hence the message is broadcasted to all observers as filter expressions
            //       are not evaluated.
            topic = "Milan";
            message = new Message { Body = "Non-JSON message" };
            observableObserverActorIoT.NotifyObserversAsync(topic, message, true).Wait();

            Console.Write(" - Observable actor has sent a message. Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] Messages = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{message.Body}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");


            // Unregister the observables
            topic = "Milan";
            observableObserverActorIoT.UnregisterObservableActorAsync(topic, true).Wait();

            Console.Write(" - Actor unregistered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            topic = "Rome";
            observableObserverServiceIoT.UnregisterObservableServiceAsync(topic, true).Wait();

            Console.Write(" - Service partition unregistered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            // Write Line
            Console.WriteLine(Line);
        }

        private static void StockMarketTestViaProxy()
        {
            // Write Line
            Console.WriteLine(Line);

            IClientObservableObserverActor observableObserverActorStocks = ActorProxy.Create<IClientObservableObserverActor>(new ActorId("StockMarket"), TestObservableObserverActorUri);
            Console.WriteLine(" - ActorProxy for the observable actor created.");

            // Clear all observers and publications.
            observableObserverActorStocks.ClearObserversAndPublicationsAsync(true).Wait();
            Console.WriteLine(" - All observers and publications cleared for the observable actor.");

            // Register observable actors
            string topic = "Stocks";
            observableObserverActorStocks.RegisterObservableActorAsync(topic).Wait();
            Console.Write(" - Actor registered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            // Retrieve observables by topic from the registry
            Uri registryServiceUri = new Uri($"{ApplicationUri}{RegistryServiceUri}");
            ServicePartitionList servicePartitionList = FabricClient.QueryManager.GetPartitionListAsync(registryServiceUri).Result;
            int registryServicePartitionCount = servicePartitionList != null && servicePartitionList.Any() ? servicePartitionList.Count : 1;
            IRegistryService registryServiceProxy = ServiceProxy.Create<IRegistryService>(registryServiceUri, new ServicePartitionKey(PartitionResolver.Resolve(topic, registryServicePartitionCount)));
            IEnumerable<EntityId> observableList = registryServiceProxy.QueryObservablesAsync(topic, null).Result;
            Console.Write(" - Observables for [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic:");
            foreach (EntityId entity in observableList)
            {
                Console.Write("   > ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(entity.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }

            // Create observer service proxies
            IClientObserverService observableObserverServiceMsft = ServiceProxy.Create<IClientObserverService>(TestObservableObserverServiceUri, new ServicePartitionKey(2));

            // Create observer actor proxies
            IClientObserverActor observableObserverActorAmzn = ActorProxy.Create<IClientObserverActor>(new ActorId("BBBB"), TestObservableObserverActorUri);
            IClientObserverActor observableObserverActorOrcl = ActorProxy.Create<IClientObserverActor>(new ActorId("CCCC"), TestObservableObserverActorUri);

            // Get observables identity
            EntityId observableObserverActorStocksEntityId = observableObserverActorStocks.GetEntityIdAsync().Result;

            // Register observableObserverServiceMsft observableObserverActorAmzn as observers:
            // - topic = Stocks and observable =  observableObserverActorStocks
            const string filterExpressionForMsftTicker = "stock = 'AAAA'";
            const string filterExpressionForAmznTicker = "stock = 'BBBB'";
            const string filterExpressionForOrclTicker = "stock = 'CCCC'";

            observableObserverServiceMsft.RegisterObserverServiceAsync("Stocks", new List<string> { filterExpressionForMsftTicker }, observableObserverActorStocksEntityId).Wait();
            observableObserverActorAmzn.RegisterObserverActorAsync("Stocks", new List<string> { filterExpressionForAmznTicker }, observableObserverActorStocksEntityId).Wait();
            observableObserverActorOrcl.RegisterObserverActorAsync("Stocks", new List<string> { filterExpressionForOrclTicker }, observableObserverActorStocksEntityId).Wait();

            Console.Write(" - Observer actors and service registered: Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] Observable = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{observableObserverActorStocksEntityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // The observableObserverActorStocks observable actor sends a JSON message
            Message[] messageArray =
            {
                new Message { Body = "{'stock': 'AAAA', 'value': 56}" },
                new Message { Body = "{'stock': 'BBBB', 'value': 675}" },
                new Message { Body = "{'stock': 'CCCC', 'value': 39}" }
            };
            foreach (Message message in messageArray)
            {
                observableObserverActorStocks.NotifyObserversAsync(topic, message, true).Wait();

                Console.Write(" - Observable actor has sent a message. Topic = [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{topic}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("] Messages = [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{message.Body}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("]");
            }

            // Unregister the observables
            observableObserverActorStocks.UnregisterObservableActorAsync("Stocks", true).Wait();

            Console.Write(" - Actor unregistered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            // Write Line
            Console.WriteLine(Line);
        }

        private static void StockMarketTestViaGateway()
        {
            // Create ShortEntityId for observable actor
            ShortEntityId entityId = new ShortEntityId("StockMarket", TestObservableObserverActorUri);
            Console.WriteLine(" - ShortEntityId for the observable actor created.");

            // Create request message for observable service
            GatewayRequest gatewayRequest = new GatewayRequest
            {
                ObservableEntityId = entityId,
                UseObserverAsProxy = true
            };

            // Clear all observers and publications for observable actor
            SendRequestToGateway(gatewayRequest, "api/observable/actor/clear");
            Console.WriteLine(" - All observers and publications cleared for the observable actor.");

            // Register observable actor
            const string topic = "Stocks";
            gatewayRequest.Topic = topic;
            SendRequestToGateway(gatewayRequest, "api/observable/actor/register");
            Console.Write(" - Actor registered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            // Retrieve observables by topic from the registry
            string[] topicArray = { "Stocks" };
            foreach (string t in topicArray)
            {
                gatewayRequest = new GatewayRequest
                {
                    Topic = t
                };
                HttpResponseMessage response = SendRequestToGateway(gatewayRequest, "api/registry/service/get");
                string json = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }
                IEnumerable<ShortEntityId> observableList = JsonConvert.DeserializeObject<IEnumerable<ShortEntityId>>(json);
                Console.Write(" - Observables for [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{t}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("] topic:");
                foreach (ShortEntityId entity in observableList)
                {
                    Console.Write("   > ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(entity.ToString());
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            // Define filter expressions
            const string filterExpressionForMsftTicker = "stock = 'AAAA'";
            const string filterExpressionForAmznTicker = "stock = 'BBBB'";
            const string filterExpressionForOrclTicker = "stock = 'CCCC'";

            // Register AAAA observer service
            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                FilterExpressions = new List<string> { filterExpressionForMsftTicker },
                ObserverEntityId = new ShortEntityId(2, TestObservableObserverServiceUri),
                ObservableEntityId = entityId
            };
            SendRequestToGateway(gatewayRequest, "api/observer/service/register");

            Console.Write(" - Observer actor [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{gatewayRequest.ObserverEntityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] registered: Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // Register BBBB observer actor
            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                FilterExpressions = new List<string> { filterExpressionForAmznTicker },
                ObserverEntityId = new ShortEntityId("BBBB", TestObservableObserverActorUri),
                ObservableEntityId = entityId
            };
            SendRequestToGateway(gatewayRequest, "api/observer/actor/register");

            Console.Write(" - Observer actor [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{gatewayRequest.ObserverEntityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] registered: Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // Register CCCC observer actor
            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                FilterExpressions = new List<string> { filterExpressionForOrclTicker },
                ObserverEntityId = new ShortEntityId("CCCC", TestObservableObserverActorUri),
                ObservableEntityId = entityId
            };
            SendRequestToGateway(gatewayRequest, "api/observer/actor/register");

            Console.Write(" - Observer actor [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{gatewayRequest.ObserverEntityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] registered: Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("]");

            // The observable actor sends an array of JSON messages
            Message[] messageArray =
            {
                new Message { Body = "{'stock': 'AAAA', 'value': 56}" },
                new Message { Body = "{'stock': 'BBBB', 'value': 675}" },
                new Message { Body = "{'stock': 'CCCC', 'value': 39}" }
            };

            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                Messages = messageArray,
                ObservableEntityId = entityId,
                UseObserverAsProxy = true
            };
            SendRequestToGateway(gatewayRequest, "api/observable/actor/notify");

            Console.Write(" - Observable actor has sent the following messages. Topic = [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] Messages:");
            foreach (Message message in gatewayRequest.Messages)
            {
                Console.Write("   > [");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{message.Body}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("]");
            }

            // Unregister the observable
            gatewayRequest = new GatewayRequest
            {
                Topic = topic,
                ObservableEntityId = entityId
            };
            SendRequestToGateway(gatewayRequest, "api/observable/actor/unregister");

            Console.Write(" - Actor unregistered as an observable for the [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{topic}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] topic.");

            // Write Line
            Console.WriteLine(Line);
        }

        private static HttpResponseMessage SendRequestToGateway(GatewayRequest gatewayRequest, string relativeUri)
        {
            HttpClient httpClient = new HttpClient
            {
                BaseAddress = new Uri(GatewayUrl)
            };
            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string json = JsonConvert.SerializeObject(gatewayRequest);
            StringContent postContent = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = httpClient.PostAsync(Combine(httpClient.BaseAddress.AbsoluteUri, relativeUri), postContent).Result;
            response.EnsureSuccessStatusCode();
            return response;
        }

        private static void MessageBoxTestViaProxy()
        {
            // Write Line
            Console.WriteLine(Line);

            // Create observer actor proxies
            IClientObserverActor observableObserverActorP = ActorProxy.Create<IClientObserverActor>(new ActorId("P"), TestObservableObserverActorUri);
            Console.WriteLine(" - ActorProxy for the observer actor created.");

            // Test MessageBoxService
            EntityId entityId = observableObserverActorP.GetEntityIdAsync().Result;

            Console.Write(" - EntityId for observer [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{entityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] retrieved.");

            Uri messageBoxServiceUri = new Uri($"{ApplicationUri}{MessageBoxServiceUri}");
            ServicePartitionList servicePartitionList = FabricClient.QueryManager.GetPartitionListAsync(messageBoxServiceUri).Result;
            Console.WriteLine(" - Partition list for the MessageBoxService retrieved.");
            int messageBoxServicePartitionCount = servicePartitionList != null && servicePartitionList.Any() ? servicePartitionList.Count : 1;

            IMessageBoxService messageBoxServiceProxy = ServiceProxy.Create<IMessageBoxService>(messageBoxServiceUri,
                                                                                                new ServicePartitionKey(PartitionResolver.Resolve(entityId.EntityUri.AbsoluteUri, messageBoxServicePartitionCount)));
            Console.WriteLine(" - ServiceProxy for the MessageBoxService created.");
            Console.Write(" - Enter the number of messages to write: ");
            string value = Console.ReadLine();
            int n;
            if (!int.TryParse(value, out n))
            {
                n = 3;
            }
            Message[] messageArray = new Message[n];
            for (int k = 0; k < n; k++)
            {
                messageArray[k] = new Message {Body = $"Test{k + 1}"};
            }
            
            messageBoxServiceProxy.WriteMessagesAsync(entityId.EntityUri, messageArray).Wait();

            Console.Write(" - Messages for the observer actor [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{entityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] have been written to the MessageBoxService:");
            foreach (Message message in messageArray)
            {
                Console.Write("   > ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message.Body);
                Console.ForegroundColor = ConsoleColor.White;
            }

            IEnumerable<Message> messages = observableObserverActorP.ReadMessagesFromMessageBoxAsync().Result;
            if (messages == null)
            {
                return;
            }

            Console.Write(" - Observer [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{entityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] has received the following messages from the MessageBoxService:");

            foreach (Message message in messages)
            {
                Console.Write("   > ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message.Body);
                Console.ForegroundColor = ConsoleColor.White;
            }

            // Write Line
            Console.WriteLine(Line);
        }

        private static void MessageBoxTestViaGateway()
        {
            // Write Line
            Console.WriteLine(Line);

            // Create ShortEntityId for observer actor
            ShortEntityId entityId = new ShortEntityId("P", TestObservableObserverActorUri);
            Console.Write(" - ShortEntityId for the observer actor [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{entityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] created.");

            // Enters the amount of messages to send
            Console.Write(" - Enter the number of messages to write: ");
            string value = Console.ReadLine();
            int n;
            if (!int.TryParse(value, out n))
            {
                n = 3;
            }
            Message[] messageArray = new Message[n];
            for (int k = 0; k < n; k++)
            {
                messageArray[k] = new Message { Body = $"Test{k + 1}" };
            }

            // Create a request message to write messages to MessageBox service via the gateway service 
            GatewayRequest gatewayRequest = new GatewayRequest
            {
                ObserverEntityId = entityId,
                Messages = messageArray
            };

            // Send messages to the MessageBox
            SendRequestToGateway(gatewayRequest, "api/messagebox/service/write");

            Console.Write(" - Messages for the observer actor [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{entityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] have been written to the MessageBoxService:");
            foreach (Message message in messageArray)
            {
                Console.Write("   > ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message.Body);
                Console.ForegroundColor = ConsoleColor.White;
            }

            // Create a request message to read messages from the MessageBox service via the gateway service
            gatewayRequest = new GatewayRequest
            {
                ObserverEntityId = entityId
            };

            // Send messages to the MessageBox
            HttpResponseMessage response = SendRequestToGateway(gatewayRequest, "api/messagebox/service/read");
            string json = response.Content.ReadAsStringAsync().Result;
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }
            IEnumerable<Message> messages = JsonConvert.DeserializeObject<IEnumerable<Message>>(json);
            if (messages == null)
            {
                return;
            }

            Console.Write(" - Observer [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{entityId.EntityUri.AbsoluteUri}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("] has received the following messages from the MessageBoxService:");

            foreach (Message message in messages)
            {
                Console.Write("   > ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message.Body);
                Console.ForegroundColor = ConsoleColor.White;
            }

            // Write Line
            Console.WriteLine(Line);
        }

        public static void EnumerateActorsViaActorProxy()
        {
            try
            {
                FabricClient fabricClient = new FabricClient();
                // Creates Uri list
                List<Uri> uriList = new List<Uri>
                {
                    TestObservableObserverActorUri
                };

                foreach (Uri uri in uriList)
                {
                    ServicePartitionList partitionList = fabricClient.QueryManager.GetPartitionListAsync(uri).Result;

                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] [{uri}]:");
                    int total = 0;

                    foreach (Partition partition in partitionList)
                    {
                        Int64RangePartitionInformation partitionInformation = partition.PartitionInformation as Int64RangePartitionInformation;
                        if (partitionInformation == null)
                        {
                            continue;
                        }
                        long partitionKey = partitionInformation.LowKey;

                        // Creates CancellationTokenSource
                        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                        // Creates ContinuationToken
                        ContinuationToken continuationToken = null;

                        // Creates ActorServiceProxy for WorkerActorService
                        IActorService actorServiceProxy = ActorServiceProxy.Create(uri, partitionKey);
                        int actorCount = 0;

                        List<ActorInformation> actorInformationList = new List<ActorInformation>();
                        do
                        {
                            PagedResult<ActorInformation> queryResult = actorServiceProxy.GetActorsAsync(continuationToken, cancellationTokenSource.Token).Result;
                            if (queryResult.Items.Any())
                            {
                                actorInformationList.AddRange(queryResult.Items);
                                actorCount += queryResult.Items.Count();
                            }
                            continuationToken = queryResult.ContinuationToken;
                        } while (continuationToken != null);

                        // Prints results
                        Console.WriteLine($"                          > Partition [{partitionInformation.Id}] contains [{actorCount}] actors.");
                        foreach (ActorInformation actorInformation in actorInformationList)
                        {
                            Console.WriteLine($"                            > ActorId [{actorInformation.ActorId}]");
                        }
                        total += actorCount;
                    }

                    // Prints results
                    Console.WriteLine($"                          > Total: [{total}] actors");
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        public static void DeleteActorsViaActorProxy()
        {
            try
            {
                FabricClient fabricClient = new FabricClient();
                // Creates Uri list
                List<Uri> uriList = new List<Uri>
                {
                    TestObservableObserverActorUri
                };

                foreach (Uri uri in uriList)
                {
                    ServicePartitionList partitionList = fabricClient.QueryManager.GetPartitionListAsync(uri).Result;

                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] [{uri}]:");
                    int total = 0;

                    foreach (Partition partition in partitionList)
                    {
                        Int64RangePartitionInformation partitionInformation = partition.PartitionInformation as Int64RangePartitionInformation;
                        if (partitionInformation == null)
                        {
                            continue;
                        }
                        long partitionKey = partitionInformation.LowKey;

                        // Creates CancellationTokenSource
                        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                        // Creates ContinuationToken
                        ContinuationToken continuationToken = null;

                        // Creates ActorServiceProxy for WorkerActorService
                        IActorService actorServiceProxy = ActorServiceProxy.Create(uri, partitionKey);
                        int actorCount = 0;

                        List<ActorInformation> actorInformationList = new List<ActorInformation>();
                        do
                        {
                            PagedResult<ActorInformation> queryResult = actorServiceProxy.GetActorsAsync(continuationToken, cancellationTokenSource.Token).Result;
                            if (queryResult.Items.Any())
                            {
                                actorInformationList.AddRange(queryResult.Items);
                                actorCount += queryResult.Items.Count();
                            }
                            continuationToken = queryResult.ContinuationToken;
                        } while (continuationToken != null);

                        // Prints results
                        Console.WriteLine($"                          > Partition [{partitionInformation.Id}] contains [{actorCount}] actors.");
                        foreach (ActorInformation actorInformation in actorInformationList)
                        {
                            actorServiceProxy.DeleteActorAsync(actorInformation.ActorId, cancellationTokenSource.Token).Wait(cancellationTokenSource.Token);
                            Console.WriteLine($"                            > ActorId [{actorInformation.ActorId}] deleted");
                        }
                        total += actorCount;
                    }

                    // Prints results
                    Console.WriteLine($"                          > Total: [{total}] actors deleted");
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }
        #endregion

        #region Private Static Methods
        private static void ReadConfiguration()
        {
            try
            {
                GatewayUrl = ConfigurationManager.AppSettings[GatewayUrlParameter] ?? DefaultGatewayUrl;
                if (string.IsNullOrWhiteSpace(GatewayUrl))
                {
                    throw new ArgumentException($"The [{GatewayUrlParameter}] setting in the configuration file is null or invalid.");
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        public static string Combine(string uri1, string uri2)
        {
            uri1 = uri1.TrimEnd('/');
            uri2 = uri2.TrimStart('/');
            return $"{uri1}/{uri2}";
        }       

        private static int SelectOption()
        {
            // Create a line
            
            int optionCount = TestList.Count + 1;

            Console.WriteLine("Select an option:");
            Console.WriteLine(Line);

            for (int i = 0; i < TestList.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[{0}] ", i + 1);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(TestList[i].Name);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" - " + TestList[i].Description);
            }

            // Add exit option
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[{0}] ", optionCount);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Exit");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - Close the test application.");
            Console.WriteLine(Line);

            // Select an option
            Console.WriteLine($"Press a key between [1] and [{optionCount}]: ");
            char key = 'a';
            while (key < '1' || key > ('1' + optionCount))
            {
                key = Console.ReadKey(true).KeyChar;
            }
            return key - '1' + 1;
        }

        private static void PrintException(
            Exception ex,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            // Write Line
            Console.WriteLine(Line);

            InternalPrintException(ex, sourceFilePath, memberName, sourceLineNumber);

            // Write Line
            Console.WriteLine(Line);
        }

        private static void InternalPrintException(Exception ex,
                                                   string sourceFilePath = "",
                                                   string memberName = "",
                                                   int sourceLineNumber = 0)
        {
            AggregateException exception = ex as AggregateException;
            if (exception != null)
            {
                foreach (Exception e in exception.InnerExceptions)
                {
                    if (sourceFilePath != null) InternalPrintException(e, sourceFilePath, memberName, sourceLineNumber);
                }
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{ex.GetType().Name}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            string fileName = null;
            if (File.Exists(sourceFilePath))
            {
                FileInfo file = new FileInfo(sourceFilePath);
                fileName = file.Name;
            }
            Console.Write(string.IsNullOrWhiteSpace(fileName) ? "Unknown" : fileName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(string.IsNullOrWhiteSpace(memberName) ? "Unknown" : memberName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(sourceLineNumber.ToString(CultureInfo.InvariantCulture));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("]");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(": ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(!string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "An error occurred.");
        }
        #endregion

        #region Private Constants
        //************************************
        // Private Constants
        //************************************
        private const string ApplicationUri = "fabric:/ObserverPattern/";
        private const string RegistryServiceUri = "RegistryService";
        private const string MessageBoxServiceUri = "MessageBoxService";
        private const string TestObservableObserverActor = "TestObservableObserverActor";
        private const string TestObservableObserverService = "TestObservableObserverService";

        //***************************
        // Configuration Parameters
        //***************************
        private const string GatewayUrlParameter = "gatewayUrl";

        //************************************
        // Default Values
        //************************************
        private const string DefaultGatewayUrl = "http://localhost:8083/gateway";

        #endregion
    }

    public class Test
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Action Action { get; set; }
    }
}
