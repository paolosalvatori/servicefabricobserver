---
services: service-fabric
platforms: dotnet
author: paolosalvatori
---
# Service Fabric Observer Sample #
This project contains a framework that provides an implementation of the Observer design pattern for Service Fabric stateful services and actors.

# Introduction #
The Service Fabric Observer Framework provides the base classes and services to implement the Observer design pattern in a Service Fabric application.

<center>![Architecture](https://raw.githubusercontent.com/paolosalvatori/servicefabricobserver/master/Images/Architecture.png)</center>

The design pattern shown in the above diagram represents a hybrid between Observable-Observer and Observer design patterns: in fact, in this framework observables and observers communicate with each other directly as in the Observer pattern, but observers typically discover topic-specific observables via a stateful registry service. In addition, they can specify filter expressions to receive only a subset of messages sent by observables. The registry service exposes the **IRegistryService** interface that can used by observable services and actors to register and unregister as an observable for a given topic. Observers can invoke the **QueryObservables** method to discover observables for a given topic. The observer can register with one or multiple observables by invoking the **RegisterObserver** method on the **IObservable** interface exposed by an observable. Likewise, when a service partition or actor is removed from the system, or whenever it decides to no longer receive messages from a given observable, it can invoke the **UnregisterObserver** on the **IObservable** interface exposed by the observable service partition or actor. Each observer maintains a list of the observable to which is registered. Likewise, an observable maintains a list of its observers, and it sends a copy of the message to each of them by invoking the **Notify** method on their **ISubscribe** interface. The observable can directly send the message to each of the observers, or can divide observers by cluster node, and use one the observers for each node as a proxy. In this case, along with the message, the observer will receive a list of observers located on the same node to which forward the message. When the observable is unable to send a message to the proxy observer, it will write the message to the messagebox of the observer by invoking the **WriteMessages** method exposed by the **MessageBoxService** and will promote another observer from the same list as proxy for the node. More in general, the framework retries every operation a configurable amount of time and uses a configurable backoff delay  between retries. In particular, when an observable sends messages to its observers, the observable tries to send the message to each observer directly or via a proxy and in case the maximum number of retries expires, he message is saved to the messagebox of the observer, so that, when it comes back online, it can retrieve along with the other messages by invoking the ReadMessages method exposed by the **MessageBoxService**. Before shutting down, an observable invokes the **UnregisterObservable** method on the **ISubscribe** interface of its observers to notify them that it is leaving. Likewise, the observable invokes the **UnregisterObservable** on the **IRegistry** interface exposed by the registry service, to notify it that it’s leaving. The registry service proceed to remove the observable from the list of observables for a certain topic. The registry service uses a **ReliableDictionary** to store observables: the item key is the topic, while the value is a list of observables for that topic. Observables are required to periodically send a heartbeat message to the registry service. 

## Demo ##
The following diagram shows the architecture design of the demo:

<center>![Demo](https://raw.githubusercontent.com/paolosalvatori/servicefabricobserver/master/Images/Demo.png)</center>

As you can see, you can use a client application to invoke three different tests, directly via ActorProxy/ServiceProxy when testing the application on the local cluster, or via gateway service that is implemented as a stateless reliable service. The gateway service exposes a REST interface implemented by six ASP.NET Web API REST services (**ApiController**). These services, allows to interact with: 


- Observable Services and Actors
- Observer Services and Actors
- Registry Service
- MessageBox Service

The test application contains two samples:
 - IoT Sample
 - Stock Market
Both samples can be run on the local Service Fabric cluster or a remote Service Fabric cluster on Azure using the client the companion test console application.

<center>![Demo](https://raw.githubusercontent.com/paolosalvatori/servicefabricobserver/master/Images/Client.png)</center>

The client application can invoke the underlying reliable and actor test services directly using a [ServiceProxy](https://msdn.microsoft.com/en-us/library/microsoft.servicefabric.services.remoting.client.serviceproxy.aspx "ServiceProxy") or [ActorProxy](https://msdn.microsoft.com/en-us/library/microsoft.servicefabric.actors.client.actorproxy.aspx "ActorProxy") instance or via the **GatewayService** using an [HttpClient](https://msdn.microsoft.com/en-us/library/system.net.http.httpclient(v=vs.118).aspx) object. When client running the client application against a remote cluster, you can only invoke the reliable and actor services via the **GatewayService**. 
In addition, the client application allows to enumerate or delete all the instance of the **TestObservableObserverActor** actor type. For more information on the client application, see the code of the **Program.cs** file under the **ObservableObserverApplication.TestClient** project.

# IoT Sample #
In the IoT samples there are two observable entities, each associated to a different factory plant or industrial site:

 - The first observable is represented by a **TestObservableObserverActor** actor with **ActorId** equal to **Milan Site**. This actor is an observable for the **Milan**.
 -  The second observable is represented by a partition of the  **TestObservableObserverService** reliable service and its topic is equal to **Rome**.

The client application creates a set of observers which subscribe with the two observable. Each actor is an instance of the **TestObservableObserverActor** class and has an alphabet letter as ActorId from P to Z. When registering with an observable, observer specify a filter expression like the following:

- "id != null and value != null and id = 10"

Filter expressions can only be used by observable entities when sending messages in JSON format to their observers on given topic. 
The **TestObservableObserverService** class inherits from the **ObservableObserverServiceBase** abstract class. Hence, its partitions are both observable and observer entities. Likewise, the **TestObservableObserverActor** class inherits from the **ObservableObserverActorBase** abstract class. This means that the actors of this type are  both observable and observer entities. This means that they can act as an observable and send messages to a set of observer but at the same time they can receive messages from one or multiple observable entities on different topics.

The following picture depicts the message flow implemented by the sample. 

<center>![Demo](https://raw.githubusercontent.com/paolosalvatori/servicefabricobserver/master/Images/IoTSample01.png)</center>

As you can see, the **Milan Site** observable actor sends a JSON message to multiple observers. In particular, when the value of the **useObserverAsProxy** boolean parameter is equal to **true**, the **NotifyObserversAsync** method splits the observers by cluster node name and instead of invoking each observer, it sends the message to an observer per cluster node, and includes in the call the list of the remaining observer entities located on the same node. The observers directly invoked by the observable will act as a proxy and will invoke the remaining observers located on the same node. When the value of the **useObserverAsProxy** boolean parameter is equal to **false**, the observable invoked each of the observer directly, regardless if they are located on the same or a different cluster node.   

The following picture depicts the second message flow implemented by the sample. 

<center>![Demo](https://raw.githubusercontent.com/paolosalvatori/servicefabricobserver/master/Images/IoTSample02.png)</center>

In this case, a partition of the **TestObservableObserverService** service sends a JSON message to multiple observer actors. Each message has the following format:

 - {'id': <device-id>, 'value': <numeric-value>}

# Stock Market #
In the second sample, there is an observable actor with **ActorId** equal to **Stock Market** that sends JSON messages to notify to three observers, each interested to a different stock ticker. Each message contains a stock price change and has the following format.

 - {'stock': '<stock-ticker>', 'value': <numeric-value>} 

The three observers that register with the **Stock Market** observable actor on the **Stocks** topic are the following:

 - A partition of the **TestObservableObserverService** which uses the filter **stock = 'AAAA'** to receive only stock price changes of the **AAAA** stock.
 - An actor with **ActorId** equal to **BBBB*** which uses the filter **stock = 'BBBB'** to receive only stock price changes of the **BBBB** stock.
 -  - An actor with **ActorId** equal to **CCCC*** which uses the filter **stock = 'CCCC'** to receive only stock price changes of the **CCCC** stock.

The following picture depicts the second message flow implemented by the sample. 

<center>![Demo](https://raw.githubusercontent.com/paolosalvatori/servicefabricobserver/master/Images/StockMarket.png)</center>

# TestObservableObserverService #
The following table shows the code of the **TestObservableObserverService** class. As you can see, in order to create a stateful reliable service that acts as both an observable and observer, you need to inherit the service class from the **ObservableObserverServiceBase** abstract class contained in the **Framework** project. If you want to define a service that acts only as an observable, the related class needs to inherit from the **ObservableServiceBase** abstract class. Likewise, if you want to create a service that acts only as an observer, the class needs to inherit from the **ObserverServiceBase** abstract class. In order to handle the events exposed by the base class, you need to define event handlers in the class constructor as shown in the code below.


	// ------------------------------------------------------------
	//  Copyright (c) Microsoft Corporation.  All rights reserved.
	//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
	// ------------------------------------------------------------
	
	namespace Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverService
	{
	    using System;
	    using System.Fabric;
	    using System.Linq;
	    using System.Text;
	    using System.Threading.Tasks;
	    using Microsoft.AzureCat.Samples.ObserverPattern.Entities;
	    using Microsoft.AzureCat.Samples.ObserverPattern.Framework;
	
	    /// <summary>
	    /// The FabricRuntime creates an instance of this class for each service type instance.
	    /// </summary>
	    internal sealed class TestObservableObserverService : ObservableObserverServiceBase
	    {
	        #region Public Constructor
	
	        /// <summary>
	        /// Initializes a new instance of the TestObservableObserverService class.
	        /// </summary>
	        public TestObservableObserverService(StatefulServiceContext context)
	            : base(context)
	        {
	            try
	            {
	                // Create Handlers for Observer Events
	                this.NotificationMessageReceived += this.TestObservableObserverService_NotificationMessageReceived;
	                this.ObservableUnregistered += this.TestObservableObserverService_ObservableUnregistered;
	
	                // Create Handlers for Observable Events
	                this.ObserverRegistered += this.TestObservableObserverService_ObserverRegistered;
	                this.ObserverUnregistered += this.TestObservableObserverService_ObserverUnregistered;
	                ServiceEventSource.Current.Message("TestObservableObserverService instance created.");
	            }
	            catch (Exception ex)
	            {
	                ServiceEventSource.Current.Error(ex);
	                throw;
	            }
	        }
	
	        #endregion
	
	        #region Observable Event Handlers
	
	        private async Task TestObservableObserverService_ObserverRegistered(SubscriptionEventArgs args)
	        {
	            try
	            {
	                EntityId id = await this.GetEntityIdAsync();
	                StringBuilder stringBuilder =
	                    new StringBuilder(
	                        $"Observer successfully registered.\r\n[Observable]: {id}\r\n[Observer]: {args.EntityId}\r\n[Subscription]: Topic=[{args.Topic}]");
	                int i = 1;
	                foreach (string expression in args.FilterExpressions.Where(expression => !string.IsNullOrWhiteSpace(expression)))
	                {
	                    stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
	                }
	                ServiceEventSource.Current.Message(stringBuilder.ToString());
	            }
	            catch (Exception ex)
	            {
	                ServiceEventSource.Current.Error(ex);
	                throw;
	            }
	        }
	
	        private async Task TestObservableObserverService_ObserverUnregistered(SubscriptionEventArgs args)
	        {
	            try
	            {
	                EntityId id = await this.GetEntityIdAsync();
	                ServiceEventSource.Current.Message(
	                    $"Observer successfully unregistered.\r\n[Observable]: {id}\r\n[Observer]: {args.EntityId}\r\n[Subscription]: Topic=[{args.Topic}]");
	            }
	            catch (Exception ex)
	            {
	                ServiceEventSource.Current.Error(ex);
	                throw;
	            }
	        }
	
	        #endregion
	
	        #region Observer Event Handlers
	
	        private async Task TestObservableObserverService_NotificationMessageReceived(NotificationEventArgs<Message> args)
	        {
	            try
	            {
	                EntityId id = await this.GetEntityIdAsync();
	                ServiceEventSource.Current.Message(
	                    $"Message Received.\r\n[Observable]: {args.EntityId}\r\n[Observer]: {id}\r\n[Message]: Topic=[{args.Topic}] Body=[{args.Message?.Body ?? "NULL"}]");
	            }
	            catch (Exception ex)
	            {
	                ServiceEventSource.Current.Error(ex);
	                throw;
	            }
	        }
	
	        private async Task TestObservableObserverService_ObservableUnregistered(SubscriptionEventArgs args)
	        {
	            try
	            {
	                EntityId id = await this.GetEntityIdAsync();
	                ServiceEventSource.Current.Message($"Observable successfully unregistered.\r\n[Observable]: {args.EntityId}\r\n[Observer]: {id}");
	            }
	            catch (Exception ex)
	            {
	                ServiceEventSource.Current.Error(ex);
	                throw;
	            }
	        }
	
	        #endregion
	    }
	}

# TestObservableObserverActor #
The following table shows the code of the **TestObservableObserverActor** class. As you can see, in order to create a stateful actor service that acts as both an observable and observer, you need to inherit the service class from the **ObservableObserverActorBase** abstract class contained in the **Framework** project. If you want to define a service that acts only as an observable, the related class needs to inherit from the **ObservableActorBase** abstract class. Likewise, if you want to create a service that acts only as an observer, the class needs to inherit from the **ObserverActorBase** abstract class. In order to handle the events exposed by the base class, you need to define event handlers in the class constructor as shown in the code below. The sample also demonstrates that an actor class can implement additional, application-specific service interfaces.

	// ------------------------------------------------------------
	//  Copyright (c) Microsoft Corporation.  All rights reserved.
	//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
	// ------------------------------------------------------------
	
	namespace Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverActor
	{
	    using System;
	    using System.Linq;
	    using System.Text;
	    using System.Threading.Tasks;
	    using global::Microsoft.AzureCat.Samples.ObserverPattern.Entities;
	    using global::Microsoft.AzureCat.Samples.ObserverPattern.Framework;
	    using global::Microsoft.AzureCat.Samples.ObserverPattern.TestObservableObserverActor.Interfaces;
	    using global::Microsoft.ServiceFabric.Actors.Runtime;
	
	    [ActorService(Name = "TestObservableObserverActor")]
	    internal class TestObservableObserverActor : ObservableObserverActorBase, ITestObservableObserverActor
	    {
	        #region Public Constructor
	
	        /// <summary>
	        /// Initializes a new instance of the TestObservableObserverActor class.
	        /// </summary>
	        public TestObservableObserverActor()
	        {
	            // Create Handlers for Observer Events
	            this.NotificationMessageReceived += this.TestObservableObserverActor_NotificationMessageReceived;
	            this.ObservableUnregistered += this.TestObservableObserverActor_ObservableUnregistered;
	
	            // Create Handlers for Observable Events
	            this.ObserverRegistered += this.TestObservableObserverActor_ObserverRegistered;
	            this.ObserverUnregistered += this.TestObservableObserverActor_ObserverUnregistered;
	
	            ActorEventSource.Current.Message("TestObservableObserverActor actor created.");
	        }
	
	        #endregion
	
	        #region ITestObservableObserverActor methods
	
	        public Task ExecuteCommandAsync(string command)
	        {
	            try
	            {
	                ActorEventSource.Current.Message($"Command Received.\r\n[Command]: [{command ?? "NULL"}]");
	            }
	            catch (Exception ex)
	            {
	                ActorEventSource.Current.Error(ex);
	                throw;
	            }
	            return Task.FromResult(true);
	        }
	
	        #endregion
	
	        #region Observable Event Handlers
	
	        private async Task TestObservableObserverActor_ObserverRegistered(SubscriptionEventArgs args)
	        {
	            try
	            {
	                EntityId entityId = await this.GetEntityIdAsync();
	                StringBuilder stringBuilder = new StringBuilder($"Observer successfully registered.\r\n[Observable]: {entityId}\r\n[Observer]: {args.EntityId}\r\n[Subscription]: Topic=[{args.Topic}]");
	                int i = 1;
	                foreach (string expression in args.FilterExpressions.Where(expression => !string.IsNullOrWhiteSpace(expression)))
	                {
	                    stringBuilder.Append($" FilterExpression[{i++}]=[{expression}]");
	                }
	                ActorEventSource.Current.Message(stringBuilder.ToString());
	            }
	            catch (Exception ex)
	            {
	                ActorEventSource.Current.Error(ex);
	                throw;
	            }
	        }
	
	        private async Task TestObservableObserverActor_ObserverUnregistered(SubscriptionEventArgs args)
	        {
	            try
	            {
	                EntityId entityId = await this.GetEntityIdAsync();
	                ActorEventSource.Current.Message($"Observer successfully unregistered.\r\n[Observable]: {entityId}\r\n[Observer]: {args.EntityId}\r\n[Subscription]: Topic=[{args.Topic}]");
	            }
	            catch (Exception ex)
	            {
	                ActorEventSource.Current.Error(ex);
	                throw;
	            }
	        }
	
	        #endregion
	
	        #region Observer Event Handlers
	
	        private async Task TestObservableObserverActor_NotificationMessageReceived(NotificationEventArgs<Message> args)
	        {
	            try
	            {
	                EntityId entityId = await this.GetEntityIdAsync();
	                ActorEventSource.Current.Message($"Message Received.\r\n[Observable]: {args.EntityId}\r\n[Observer]: {entityId}\r\n[Message]: Topic=[{args.Topic}] Body=[{args.Message?.Body ?? "NULL"}]");
	            }
	            catch (Exception ex)
	            {
	                ActorEventSource.Current.Error(ex);
	                throw;
	            }
	        }
	
	        private async Task TestObservableObserverActor_ObservableUnregistered(SubscriptionEventArgs args)
	        {
	            try
	            {
	                EntityId entityId = await this.GetEntityIdAsync();
	                ActorEventSource.Current.Message($"Observable successfully unregistered.\r\n[Observable]: {args.EntityId}\r\n[Observer]: {entityId}");
	            }
	            catch (Exception ex)
	            {
	                ActorEventSource.Current.Error(ex);
	                throw;
	            }
	        }
	
	        #endregion
	    }
	}

# Monitoring the Test Application on the Local Cluster #
The observable and observer classes use custom **SourceEvent** classes to generate ETW events. You can use Visual Studio to see streaming events while running the application on the local cluster, as shown in the following picture.

<center>![Demo](https://raw.githubusercontent.com/paolosalvatori/servicefabricobserver/master/Images/Diagnostics.png)</center>

# Appendix #
This section contains a description of the Observer design pattern.

## Observer Design Pattern ##
The observer pattern is a [Gang of Four design pattern](http://www.blackwasp.co.uk/GofPatterns.aspx). The Gang of Four are the authors of the book, "[Design Patterns: Elements of Reusable Object-Oriented Software](http://en.wikipedia.org/wiki/Design_Patterns_(book))". This important book describes various development techniques and pitfalls in addition to providing twenty-three object-oriented programming design patterns. The four authors were Erich Gamma, Richard Helm, Ralph Johnson and John Vlissides. The Observer is a behavioral pattern as it defines a manner for controlling communication between classes or entities. The observer pattern is used to allow a single object, known as the subject, to publish changes to its state. Many other observer objects that depend upon the subject can subscribe to it so that they are immediately and automatically notified of any changes to the subject's state.
The pattern gives loose coupling between the subject and its observers. The subject holds a collection of observers that are set only at run-time. Each observer may be of any class that inherits from a known base class or implements a common interface. The actual functionality of the observers and their use of the state data need not be known by the subject.

<center>![UML Diagram of the Observer Design Pattern](https://raw.githubusercontent.com/paolosalvatori/servicefabricobserver/master/Images/UMLChartOfObserverDesignPattern.png)</center> 

<center>**UML Diagram of the Observer Design Pattern**</center>

The UML class diagram above describes an implementation of the observer design pattern. The items in the diagram are described below:
<ul>
<li>
**SubjectBase**. This is the abstract base class for concrete subjects. It contains a private collection of the observers that are subscribed to a subject and methods to allow new subscriptions to be added and existing ones to be removed. It also includes a method that can be called by concrete subjects to notify their observers of state changes. This Notify method loops through all of the registered observers, calling their Update methods.
</li>
<li>
**ConcreteSubject**. Each concrete subject maintains its own state. When a change is made to that state, the object calls the base class's Notify method to indicate this to all of its observers. As the functionality of the observers is unknown, the concrete subjects also provide the means for the observers to read the updated state, in this case via a GetState method.
</li>
<li>
**ObserverBase**. This is the abstract base class for all observers. It defines a method to be called when the subject's state changes. In many cases this Update method will be abstract, in which case you may decide to implement the base class as an interface instead.
</li>
<li>
**ConcreteObserver**. The concrete observer objects are the observers that react to changes in the subject's state. When the Update method for an observer is called, it examines the subject to determine which information has changed. It can then take appropriate action.
</li>
</lu>

## Example of Observer Design Pattern ##
An example of the pattern could be used in a logging system. A central logging module could be used to receive errors, warnings and other messages from a variety of services. This would be the subject object, whose publicly visible state included details of the last message received. The logging module itself would not perform any additional processing of the messages received. Instead, it would raise a notification to its observers for each new message.
The observers in this example could be varied in functionality but all would receive the same notifications. There could be an observer that formatted the last message into an email and sent this to an administrator. Another observer may store the message in the server's event log. A third could record it in a database, on-premises or in the cloud. In each case, the subject object would be unaware of the actions being undertaken. The observers in use could be selected by a user at run-time or via a configuration system to allow control of the logger's behavior without modification to the source code.

## The Observer Design Pattern in the .NET Framework ##
The .NET Framework provides support for a full-fledged implementation of the Observer pattern: the [IObserver<T>](https://msdn.microsoft.com/en-us/library/dd783449(v=vs.110).aspx) and [IObservable<T>](https://msdn.microsoft.com/en-us/library/dd990377(v=vs.110).aspx) interfaces provide a generalized mechanism for push-based notification. The [IObservable<T>](https://msdn.microsoft.com/en-us/library/dd990377(v=vs.110).aspx) interface represents the class that sends notifications (the provider); the [IObserver<T>](https://msdn.microsoft.com/en-us/library/dd783449(v=vs.110).aspx) interface represents the class that receives them (the observer). T represents the class that provides the notification information. In some push-based notifications, the [IObserver<T>](https://msdn.microsoft.com/en-us/library/dd783449(v=vs.110).aspx) implementation and T can represent the same type. The provider must implement a single method, Subscribe, that indicates that an observer wants to receive push-based notifications. Callers to the method pass an instance of the observer. The method returns an IDisposable implementation that enables observers to cancel notifications at any time before the provider has stopped sending them.
At any given time, a given provider may have zero, one, or multiple observers. The provider is responsible for storing references to observers and ensuring that they are valid before it sends notifications. The [IObservable<T>](https://msdn.microsoft.com/en-us/library/dd990377(v=vs.110).aspx) interface does not make any assumptions about the number of observers or the order in which notifications are sent.
The provider sends the following three kinds of notifications to the observer by calling [IObserver<T>](https://msdn.microsoft.com/en-us/library/dd783449(v=vs.110).aspx) methods:
<ul>
<li> 
The current data. The provider can call the [IObserver<T>](https://msdn.microsoft.com/en-us/library/dd783449(v=vs.110).aspx).[OnNext](https://msdn.microsoft.com/en-us/library/dd782792(v=vs.110).aspx) method to pass the observer a T object that has current data, changed data, or fresh data.
</li>
<li>
An error condition. The provider can call the [IObserver<T>](https://msdn.microsoft.com/en-us/library/dd783449(v=vs.110).aspx).[OnError](https://msdn.microsoft.com/en-us/library/dd781657(v=vs.110).aspx) method to notify the observer that some error condition has occurred.
</li>
<li>
No further data. The provider can call the [IObserver<T>](https://msdn.microsoft.com/en-us/library/dd783449(v=vs.110).aspx).[OnCompleted](https://msdn.microsoft.com/en-us/library/dd782982(v=vs.110).aspx) method to notify the observer that it has finished sending notifications.
</li>
</lu>
For more information, see [IObservable<T>](https://msdn.microsoft.com/en-us/library/dd990377(v=vs.110).aspx) Interface and [IObserver<T>](https://msdn.microsoft.com/en-us/library/dd783449(v=vs.110).aspx) Interface. 
The .NET Framework provides an easy and flexible way to implement the Observer pattern using Reactive Extensions. [Reactive Extensions (Rx)](https://msdn.microsoft.com/en-us/data/gg577609.aspx) is a library for composing asynchronous and event-based programs using observable sequences and LINQ-style query operators. Data sequences can take many forms, such as a stream of data from a file or web service, web services requests, system notifications, or a series of events such as user input. Reactive Extensions represents all these data sequences as observable sequences. An application can subscribe to these observable sequences to receive asynchronous notifications as new data arrive. 
When using the Reactive Extensions, you do not need to implement the [IObservable<T>](https://msdn.microsoft.com/en-us/library/dd990377(v=vs.110).aspx) interface manually to create an observable sequences. Similarly, you do not need to implement [IObserver<T>](https://msdn.microsoft.com/en-us/library/dd783449(v=vs.110).aspx) either to subscribe to a sequence. By installing the Reactive Extension assemblies, you can take advantage of the Observable type which provides many static LINQ operators for you to create a simple sequence with zero, one or more elements. In addition, Rx provides Subscribe extension methods that take various combinations of OnNext, OnError and OnCompleted handlers in terms of delegates.
The following sample uses the Range operator of the Observable type to create a simple observable collection of numbers. The observer subscribes to this collection using the Subscribe method of the Observable class, and provides actions that are delegates which handle OnNext, OnError and OnCompleted. 
The Range operator has several overloads. In our example, it creates a sequence of integers that starts with x and produces y sequential numbers afterwards.

