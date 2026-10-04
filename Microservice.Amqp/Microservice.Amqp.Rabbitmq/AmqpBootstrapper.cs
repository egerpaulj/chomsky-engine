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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Schema;
using LanguageExt;
using Microservice.Amqp.Configuration;
using Microservice.Amqp.Rabbitmq.Configuration;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Microservice.Amqp.Rabbitmq
{
    public class AmqpBootstrapper : IAmqpBootstrapper, IDisposable
    {
        private readonly RabbitmqConfig _config;
        private readonly AmqpConfiguration _amqpConfiguration;

        private IConnection connection;
        private IChannel channel;
        private readonly ConcurrentBag<string> createdQueueNames = [];
        private bool disposedValue;
        private readonly ConnectionFactory connectionFactory;

        public AmqpBootstrapper(IConfiguration configuration)
        {
            _config = AmqpProvider.LoadRabbitmqConfiguration(configuration);
            _amqpConfiguration = new AmqpConfiguration(configuration);

            connectionFactory = CreateChanellFactory();
            ConnectToRabbitMq();
        }

        private void ConnectToRabbitMq()
        {
            connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
            channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
        }

        public TryOptionAsync<Unit> Bootstrap()
        {
            return async () =>
            {
                try
                {
                    // Connect to RabbitMQ and Create Exchanges and Queues.
                    foreach (var context in _amqpConfiguration.AmqpContexts)
                    {
                        if (createdQueueNames.Exists(s => s == context.QueueName))
                            continue;

                        createdQueueNames.Add(context.QueueName);

                        // Message in the Exchange will be directly sent to a Queue
                        await channel.ExchangeDeclareAsync(context.Exchange, "direct", true, false);

                        // Configure a Deadletter Queue for message lost/NACKed
                        await channel.ExchangeDeclareAsync($"{context.Exchange}_dlq", "direct", true, false);

                        if (!string.IsNullOrEmpty(context.QueueName))
                        {
                            // Create a Queue and link to the exchange. Specify all message with the respective Routing Key, to be sent to this Queue.
                            // Also ensure NACKed message are sent to the Deadletter exchange.
                            await channel.QueueDeclareAsync(
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
                            await channel.QueueDeclareAsync($"{context.QueueName}_dlq", true, false, false);
                            await channel.QueueBindAsync(
                                $"{context.QueueName}_dlq",
                                $"{context.Exchange}_dlq",
                                context.RoutingKey
                            );
                            await channel.QueueBindAsync(
                                context.QueueName,
                                context.Exchange,
                                context.RoutingKey
                            );
                        }
                    }
                }
                catch
                {
                    channel.Dispose();
                    connection.Dispose();

                    ConnectToRabbitMq();
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
                using (var connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult())
                using (var channel = connection.CreateChannelAsync().GetAwaiter().GetResult())
                {
                    foreach (var queueName in queues)
                    {
                        await channel.QueueDeleteAsync(queueName, ifUnused: false, ifEmpty: false);
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
                try
                {
                    if (createdQueueNames.Exists(s => s == queueName))
                        return Unit.Default;

                    createdQueueNames.Add(queueName);

                    // Create a Queue and link to the exchange. Specify all message with the respective Routing Key, to be sent to this Queue.
                    // Also ensure NACKed message are sent to the Deadletter exchange.
                    await channel.QueueDeclareAsync(
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
                    await channel.QueueDeclareAsync($"{queueName}_dlq", true, false, false);
                    await channel.QueueBindAsync($"{queueName}_dlq", $"{exchangeName}_dlq", routingKey);
                    await channel.QueueBindAsync(queueName, exchangeName, routingKey);
                }
                catch
                {
                    channel.Dispose();
                    connection.Dispose();

                    ConnectToRabbitMq();
                }

                return await Task.FromResult(Unit.Default);
            };
        }

        public TryOptionAsync<uint> GetQueueSize(string queueName)
        {
            return async () =>
            {
                if (string.IsNullOrWhiteSpace(queueName))
                    return (uint)0;

                try
                {
                    var queue = await channel.QueueDeclarePassiveAsync(queueName);
                    return queue.MessageCount;
                }
                catch
                {
                    return (uint)0;
                }
            };
        }

        private ConnectionFactory CreateChanellFactory() =>
            new ConnectionFactory
            {
                HostName = _config.Host,
                VirtualHost = _config.VirtHost,
                Port = _config.Port,
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
                    Port = _config.Port,
                    UserName = _config.Username,
                    Password = _config.Password,
                };

                using (var connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult())
                using (var channel = connection.CreateChannelAsync().GetAwaiter().GetResult())
                {
                    foreach (var context in _amqpConfiguration.AmqpContexts)
                    {
                        await channel.ExchangeDeleteAsync(context.Exchange, ifUnused: false);
                        await channel.ExchangeDeleteAsync($"{context.Exchange}_dlq", ifUnused: false);

                        if (!string.IsNullOrEmpty(context.QueueName))
                        {
                            await channel.QueueDeleteAsync(context.QueueName, ifUnused: false, ifEmpty: false);
                            await channel.QueueDeleteAsync($"{context.QueueName}_dlq", ifUnused: false, ifEmpty: false);
                        }
                    }
                }

                return await Task.FromResult(Unit.Default);
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    channel.Dispose();
                    connection.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
