//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2022  Paul Eger

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
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Amqp;
using Microservice.Amqp.Configuration;
using Microservice.Amqp.Rabbitmq;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Rabbitmq
{
    public class RabbitMqConsumer<T>(
        ILogger<RabbitMqConsumer<T, T>> logger,
        IJsonConverterProvider converterProvider,
        IRabbitMqConnectionFactory rabbitMqConnectionFactory,
        IMessageHandler<T, T> messageHandler
    )
        : RabbitMqConsumer<T, T>(
            logger,
            converterProvider,
            rabbitMqConnectionFactory,
            messageHandler
        ) { }

    /// <summary>
    /// Consumes RabbitMq message from a Queue and provides the messages to the exchange.
    /// </summary>
    public class RabbitMqConsumer<T, R> : IConsumer<R>, IConfigInitializor
    {
        private IMessageSubscriber<T, R> _subscriber;

        private readonly IJsonConverterProvider _converterProvider;
        private readonly IRabbitMqConnectionFactory _rabbitMqConnectionFactory;
        private readonly IMessageHandler<T, R> _messageHandler;
        private readonly ILogger<RabbitMqConsumer<T, R>> _logger;

        public RabbitMqConsumer(
            ILogger<RabbitMqConsumer<T, R>> logger,
            IJsonConverterProvider converterProvider,
            IRabbitMqConnectionFactory rabbitMqConnectionFactory,
            IMessageHandler<T, R> messageHandler
        )
        {
            _logger = logger;
            _converterProvider = converterProvider;
            _rabbitMqConnectionFactory = rabbitMqConnectionFactory;
            _messageHandler = messageHandler;
        }

        public TryOptionAsync<Unit> End()
        {
            return async () =>
            {
                _subscriber.Dispose();

                return await Task.FromResult(Unit.Default);
            };
        }

        public IObservable<Either<Message<R>, ConsumerException>> GetObservable()
        {
            return _subscriber
                .GetMessageObservable()
                .Select(either =>
                    either
                        .Map(e => new ConsumerException(e))
                        .MapLeft(inData =>
                        {
                            var imessage =
                                (inData as IMessage) == null
                                    ? Option<IMessage>.None
                                    : Option<IMessage>.Some(inData as IMessage);
                            return new Message<R>(imessage)
                            {
                                Payload = inData.Payload,
                                Id = inData.Id,
                                CorrelationId = inData.CorrelationId,
                                RoutingKey = inData.RoutingKey,
                            };
                        })
                );
        }

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration
                .ToTryOptionAsync()
                .Bind<IConfiguration, Unit>(config =>
                    async () =>
                    {
                        var amqpConfiguration = new AmqpConfiguration(config);
                        var rabbitMqConfig = AmqpProvider.LoadRabbitmqConfiguration(config);

                        _subscriber = AmqpProvider.CreateSubscriber(
                            _messageHandler,
                            amqpConfiguration.AmqpContexts.FirstOrDefault(),
                            rabbitMqConfig,
                            _rabbitMqConnectionFactory,
                            _converterProvider
                        );

                        return await Task.FromResult(Unit.Default);
                    }
                );
        }

        public TryOptionAsync<Unit> Start()
        {
            return async () =>
            {
                _subscriber.Start();

                return await Task.FromResult(Unit.Default);
            };
        }
    }
}
