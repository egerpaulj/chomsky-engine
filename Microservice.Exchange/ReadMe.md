# Microservice.Exchange 

.NET library provides a Messaging Exchange library for Enterprise Integration Patterns

## Abstract

Distributed systems result in having to integrate to/from various systems.  

**E.g.**
- Message Queues 
- REST services 
- Databases
- File/Directories
- Operating System Commands 
- etc.

A Message Exchange allows safe consumption, routing, filtering, caching, translation/mapping of messages from source-data-source to a target-data-source.

Additionally the Exchange allows publication to any target system.

  ![Exchange Design](/Documentation/Exchange.png)

The Exchange can be summarised using the steps outline below:
1) Consume messages from a data source (**DataIn**)
2) Perform an action on the Message (**Transformer**)
3) Filter for matching Publishers (**Filters**)
4) Publish the output to the Matched Publishers (**DataOut**)

Additionally the Exchange should recover from crashes and continue from last working state. 

## Message Exchange

The Message Exchange implements the followin interface:

**IMessageExchange<in T, out R>**
- **T** is the type of the Input Message
- **R** is the type of the Output Message
- TryOptionAsync<Unit> **Start()**;
- TryOptionAsync<Unit> **End()**;

###  Creating an Exchange

A factory is provided where exchanges can be created based on a configuration.

**IExchangeFactory**
- TryOptionAsync<IMessageExchange<T, R>> **CreateMessageExchange<T, R>**(Option<IConfigurationSection> config);
- TryOptionAsync<IMessageExchange<T, R>> **CreateMessageExchange<T, R>**(Option<IConfiguration> configuration, Option<string> exchangeName);
- TryOptionAsync<IMessageExchange<T, R>> **CreateMessageExchange<T, R>**(Option<string> jsonConfig, Option<string> exchangeName);

## Configuration

Setting up Message Exchanges dynamically, using a configuration; simplifies various integration activities and allows implementation of all integration patterns.

The following key components define a Message Exchange (and it's Configuration):

### ExchangeName

Name of the Exchange. Consumed Messages are temporarily stored in a **.working** directory to ensure recovery on crash.

### DataIn

The data source to consume data from. Can be any type that implements the following interface:

**IConsumer<T>**
- **T** is the incoming data type
- TryOptionAsync<Unit> **Start()**;
- IObservable<Either<Message<T>, ConsumerException>> **GetObservable()**;
- TryOptionAsync<Unit> **End()**;

E.g. File Consumer gets all data from a configured directory

```
"DataIn": {
            "FileConsumer": {
              "TypeName": "Microservice.Exchange.Endpoints.FileEndpoint`2, Microservice.Exchange.Core",
              "InPath": "testData/in",
              "FileFilter": "*.*"
            }
```

**Note:** Only one consumer is allowed

**Note:** If multiple data sources exists, then configure a second/third Message Exchange

### Transformer

Transforms the data from **DataIn** to **DataOut**. Can be any type that implements the following interface:

**ITransformer<T, R>**
- **T** is the **DataIn** type
- **R** is the **DataOut** type
- TryOptionAsync<Message<R>> **Transform(Option<Message<T>> input)**;

**Note:** If the data does not need any Transformation, then the Transformer configuration can be skipped

**Note:** The Transformers can also be used to perform some business logic, then the result of the logic will be sent to **DataOut**

- Transform a message to another type
- Enrich a message
- Encode/encrypt a message
- Decode/decrypt a message
- Notify a 3rd party 
- Cache the message
- Update a registry
- Etc.

**E.g.** The integration test has a Transformer which converts the data to another type:

```
 "Transformer" : {
            "TestDataTransformer": {
              "TypeName": "Microservice.Exchange.Test.TestDataTransformer, Microservice.Exchange.Test"
            }
          }
```
### DataOut

The target data source to publish. Can be any type that implements the following interface:

**IPublisher<R>**
- **R** is the type of message to be sent out
- string **Name** { get; }
- TryOptionAsync<Unit> **Publish**(Option<Message<R>> message);

**Note:** Multiple Data Outputs can be configured

**E.g.** below the output is published to both the **Console** and as a **File** 

```
"DataOut": {
              "ConsolePublisher": {
                "TypeName": "Microservice.Exchange.Endpoints.ConsolePublisher`2, Microservice.Exchange.Core"
              },
              "FilePublisher": {
                "TypeName": "Microservice.Exchange.Endpoints.FileEndpoint`2, Microservice.Exchange.Core",
                "OutPath": "testData/out",
                "FileFilter": "*.*"
              }
          }
```


### Deadletter

The data source to publish Message Exchange errors (e.g. unknown exception during translation, or exception during publishing).

Can be any type that implements the following interface:

**IDeadletterPublisher<T, R>**
- TryOptionAsync<Unit> **PublishError**(Option<ErrorMessage<T>> message);
- TryOptionAsync<Unit> **PublishError**(Option<ErrorMessage<R>> message);
- TryOptionAsync<Unit> **PublishError**(Option<string> message);

**Note:** Only one deadletter is allowed

**E.g.**

```
"Deadletter": {
            "Console": {
              "WriteTo": "Console"
            }
        }
```

### TypeMappings

Each name used for **DataIn** **DataOut** **Deadletter** can be linked to a type.

**Note:** The name should be mapped to a type (note: the type should be loadable; i.e. DLL in executing directory or GAC or a configured path for assembly loading)

**Note:** The publisher should be configured for resiliency if required

**Note:** Alternatively, you can skip the mappings and provide the **TypeName** explicitly (but this might result in the configuration being difficult to read)

**E.g.** The **CommandConsumer**, **File** and **Console** are abbrevations, the Type mappings, map the name to a type

```
"MessageExchanges": {
    "CommandAsDatIn" : {
        "DataIn": {
          "CommandConsumer": {
            "Command": "/bin/bash",
            "Arguments": "-c \"ls -m |grep -i appsettings.Development.json | tail -5\""
          }
        },
        "DataOut": {
          "File": {
              "OutPath": "testData/command/in"
          }
        },
        "Deadletter": {
            "Console": {
              "WriteTo": "Console"
            }
        },
        "TypeMappings" : {
          "CommandConsumer": "Microservice.Exchange.Endpoints.Command.CommandConsumer, Microservice.Exchange.Endpoints.Command",
          "File": "Microservice.Exchange.Endpoints.FileEndpoint`2, Microservice.Exchange.Core",
          "Console": "Microservice.Exchange.Endpoints.ConsolePublisher`2, Microservice.Exchange.Core"
        }
    }
}
```

## Endpoints

The Author plans to publish several Endpoints. To date, the following endpoints are available:
- Console (Publisher only)
- File
- Command/Process
- MongoDb
- RabbitMq
- Elasticsearch

## Acknowledgements

This library was highly influenced by the excellent work published by:

- Gregor Hohpe's Enterprise Integration Patterns

https://www.enterpriseintegrationpatterns.com/

- Apache Camel - Java implementation of many enterprise integration patterns and endpoints/components

https://camel.apache.org

## License

Copyright (C) 2022  Paul Eger

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
