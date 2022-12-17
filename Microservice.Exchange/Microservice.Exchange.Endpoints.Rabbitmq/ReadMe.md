# Microservice.Exchange.Endpoints.Rabbitmq

.NET library provides an implementation of Consumer/Publish Endpoint for the Messaging Exchange library.

The library provides the following:
- **DataIn:** Consumes messages from a Rabbitmq queue
- **DataOut:** Publishes data to a Rabbitmq Exchange
- **Deadletter:** Publishes the error data to a RabbitMq Exchange

## Usage

This Endpoint is based on the Microservice.Amqp.Rabbitmq library. Please refer to this library for configuration details.

### DataIn

```
"DataIn": {
        "RabbitMqConsumer": {
          "Amqp": {
            "Contexts": {
              "CrawlRequest": {
                "Exchange": "exchangetest",
                "Queue": "request_queue",
                "RoutingKey": "requests*",
                "RetryCount": "0"
              }
            },
            "Provider": {
              "Rabbitmq" : {
                "Host": "10.137.0.50",
                "VirtHost": "test",
                "Port": "5671",
                "Username": "guest",
                "Password": "guest"
              }
            }
          }
        }
      },
"TypeMappings" : {
        "RabbitMqConsumer": "Microservice.Exchange.Endpoints.Rabbitmq.RabbitMqConsumer`1, Microservice.Exchange.Endpoints.Rabbitmq"
      }      
```

### DataOut

```
"DataOut": {
            "RabbitMqPublisher": {
              "Amqp": {
                "Contexts": {
                  "CrawlRequest": {
                    "Exchange": "exchangetest",
                    "Queue": "request_queue",
                    "RoutingKey": "requests*",
                    "RetryCount": "0"
                  }
                },
                "Provider": {
                  "Rabbitmq" : {
                    "Host": "10.137.0.50",
                    "VirtHost": "test",
                    "Port": "5671",
                    "Username": "guest",
                    "Password": "guest"
                  }
                }
              }
            }
        },
"TypeMappings" : {
        "RabbitMqPublisher": "Microservice.Exchange.Endpoints.Rabbitmq.RabbitMqPublisher`2, Microservice.Exchange.Endpoints.Rabbitmq"
      }   

```


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