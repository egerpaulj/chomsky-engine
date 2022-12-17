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
using System.Collections.Generic;
using System.Text;
using LanguageExt;
using Microservice.Amqp.Rabbitmq.Configuration;
using Microservice.Serialization;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Microservice.Amqp.Rabbitmq
{
    public class MessagePublisher : IMessagePublisher
    {
        private readonly RabbitMqPublisherConfig _config;
        private readonly string _routingKey;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IJsonConverterProvider _jsonConverterProvider;

        public MessagePublisher(RabbitMqPublisherConfig rabbitmqConfig, IRabbitMqConnectionFactory connectionFactory, IJsonConverterProvider jsonConverterProvider)
        {
            _jsonConverterProvider = jsonConverterProvider;
            _config = rabbitmqConfig;
            _routingKey = _config.RoutingKey.Match(r => r, () => string.Empty);

            _connectionFactory = connectionFactory.CreateConnectionFactory(_config);
        }

        public TryOptionAsync<Unit> Publish<T>(Option<Message<T>> message)
        {
            return _config.Exchange
                .ToTryOptionAsync()
                .SelectMany(ex =>
                   message.ToTryOptionAsync(), (exchange, message) =>
                       Publish<T>(
                           exchange,
                           _routingKey,
                           message));
        }

        public TryOptionAsync<Unit> Publish<T>(Option<T> message)
        {
            return Publish<T>(new Message<T> { Payload = message, Context = _config.Context });
        }

        public TryOptionAsync<Unit> Publish<T>(Option<T> message, Option<Guid> correlationId)
        {
            return Publish<T>(new Message<T>() { CorrelationId = correlationId, Payload = message, Context = _config.Context });
        }

        private Unit Publish<T>(string exchange, string routingKey, Message<T> message)
        {
            using (var connection = _connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {

                var corrId = message.CorrelationId.Match(c => c, () => Guid.NewGuid());
                var id = message.Id.Match(c => c, () => Guid.NewGuid());

                // Create Custom Properties for the message.
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = message.MessageType;
                properties.CorrelationId = corrId.ToString();
                // need to use AMQP timestamps for RabbitMQ to recognize it
                properties.Timestamp = new AmqpTimestamp((Int32)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);
                properties.Headers = new Dictionary<string, object>();
                properties.Headers.Add("RetryCount", message.RetryCount.Match(r => r, () => 0));
                properties.Headers.Add("Id", Encoding.UTF8.GetBytes(id.ToString()));
                message.Context.Match(c => properties.Headers.Add("Context", Encoding.UTF8.GetBytes(c)), () => { });

                channel.BasicPublish(
                    exchange,
                    routingKey,
                    false,
                    properties,
                    Encoding.UTF8.GetBytes(
                        _jsonConverterProvider.Serialize(
                            message.Payload.Match(p => p, () => throw new Exception("Not allowed to publish a message without a payload")))));
            }

            return Unit.Default;
        }
    }
}