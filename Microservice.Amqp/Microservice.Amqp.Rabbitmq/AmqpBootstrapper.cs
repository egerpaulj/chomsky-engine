//      Microservice AMQP Libraries for .Net C#
//      Copyright (C) 2021  Paul Eger

//      This program is free software: you can redistribute it and/or modify
//      it under the terms of the GNU General Public License as published by
//      the Free Software Foundation, either version 3 of the License, or
//      (at your option) any later version.

//      This program is distributed in the hope that it will be useful,
//      but WITHOUT ANY WARRANTY; without even the implied warranty of
//      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//      GNU General Public License for more details.

//      You should have received a copy of the GNU General Public License
//      along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Amqp.Configuration;
using Microservice.Amqp.Rabbitmq.Configuration;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Microservice.Amqp.Rabbitmq
{
    public class AmqpBootstrapper : IAmqpBootstrapper
    {
        private readonly RabbitmqConfig _config;
        private readonly AmqpConfiguration _amqpConfiguration;

        public AmqpBootstrapper(IConfiguration configuration)
        {
            _config = AmqpProvider.LoadRabbitmqConfiguration(configuration);
            _amqpConfiguration = new AmqpConfiguration(configuration);
        }

        public TryOptionAsync<Unit> Bootstrap()
        {
            return async () =>
            {
                var connectionFactory = CreateChanellFactory();

                // Connect to RabbitMQ and Create Exchanges and Queues.
                using (var connection = connectionFactory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    foreach (var context in _amqpConfiguration.AmqpContexts)
                    {
                        // Message in the Exchange will be directly sent to a Queue
                        channel.ExchangeDeclare(context.Exchange, "direct", true, false);

                        // Configure a Deadletter Queue for message lost/NACKed
                        channel.ExchangeDeclare($"{context.Exchange}_dlq", "direct", true, false);

                        if (!string.IsNullOrEmpty(context.QueueName))
                        {
                            // Create a Queue and link to the exchange. Specify all message with the respective Routing Key, to be sent to this Queue.
                            // Also ensure NACKed message are sent to the Deadletter exchange.
                            var queueResult = channel.QueueDeclare(
                                context.QueueName,
                                true,
                                false,
                                false,
                                new Dictionary<string, object>
                                {
                                    { "x-dead-letter-exchange", $"{context.Exchange}_dlq" },
                                    { "x-dead-letter-routing-key", context.RoutingKey },
                                }
                            );

                            // Setup a deadletter Queue and bind to the Exchange
                            channel.QueueDeclare($"{context.QueueName}_dlq", true, false, false);
                            channel.QueueBind(
                                $"{context.QueueName}_dlq",
                                $"{context.Exchange}_dlq",
                                context.RoutingKey
                            );
                            channel.QueueBind(
                                context.QueueName,
                                context.Exchange,
                                context.RoutingKey
                            );
                        }
                    }
                }

                return await Task.FromResult(Unit.Default);
            };
        }

        public TryOptionAsync<Unit> DeleteQueues(IEnumerable<string> queues)
        {
            return async () =>
            {
                ConnectionFactory connectionFactory = CreateChanellFactory();

                // Connect to RabbitMQ and Create Exchanges and Queues.
                using (var connection = connectionFactory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    foreach (var queueName in queues)
                    {
                        channel.QueueDelete(queueName, ifUnused: false, ifEmpty: false);
                    }
                }

                return await Task.FromResult(Unit.Default);
            };
        }

        public TryOptionAsync<Unit> CreateQueue(
            string queueName,
            string exchangeName,
            string routingKey
        )
        {
            return async () =>
            {
                ConnectionFactory connectionFactory = CreateChanellFactory();

                // Connect to RabbitMQ and Create Exchanges and Queues.
                using (var connection = connectionFactory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    // Create a Queue and link to the exchange. Specify all message with the respective Routing Key, to be sent to this Queue.
                    // Also ensure NACKed message are sent to the Deadletter exchange.
                    var queueResult = channel.QueueDeclare(
                        queueName,
                        true,
                        false,
                        false,
                        new Dictionary<string, object>
                        {
                            { "x-dead-letter-exchange", $"{exchangeName}_dlq" },
                            { "x-dead-letter-routing-key", routingKey },
                        }
                    );

                    // Setup a deadletter Queue and bind to the Exchange
                    channel.QueueDeclare($"{queueName}_dlq", true, false, false);
                    channel.QueueBind($"{queueName}_dlq", $"{exchangeName}_dlq", routingKey);
                    channel.QueueBind(queueName, exchangeName, routingKey);
                }

                return await Task.FromResult(Unit.Default);
            };
        }

        private ConnectionFactory CreateChanellFactory() =>
            new ConnectionFactory
            {
                HostName = _config.Host,
                VirtualHost = _config.VirtHost,
                UserName = _config.Username,
                Password = _config.Password,
            };

        public TryOptionAsync<Unit> Purge()
        {
            return async () =>
            {
                var connectionFactory = new ConnectionFactory
                {
                    HostName = _config.Host,
                    VirtualHost = _config.VirtHost,
                    UserName = _config.Username,
                    Password = _config.Password,
                };

                using (var connection = connectionFactory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    foreach (var context in _amqpConfiguration.AmqpContexts)
                    {
                        channel.ExchangeDeleteNoWait(context.Exchange);
                        channel.ExchangeDeleteNoWait($"{context.Exchange}_dlq");

                        if (!string.IsNullOrEmpty(context.QueueName))
                        {
                            channel.QueueDeleteNoWait(context.QueueName);
                            channel.QueueDeleteNoWait($"{context.QueueName}_dlq");
                        }
                    }
                }

                return await Task.FromResult(Unit.Default);
            };
        }
    }
}
