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
    /// <summary>
    /// Consumes RabbitMq message from a Queue and provides the messages to the exchange.
    /// </summary>
    public class RabbitMqConsumer<T> : IConsumer<T>, IConfigInitializor
    {
        private IMessageSubscriber<T, T> _subscriber;

        private readonly IJsonConverterProvider _converterProvider;
        private readonly IRabbitMqConnectionFactory _rabbitMqConnectionFactory;
        private readonly ILogger<RabbitMqConsumer<T>> _logger;

        public RabbitMqConsumer(ILogger<RabbitMqConsumer<T>> logger, IJsonConverterProvider converterProvider, IRabbitMqConnectionFactory rabbitMqConnectionFactory)
        {
            _logger = logger;
            _converterProvider = converterProvider;
            _rabbitMqConnectionFactory = rabbitMqConnectionFactory;
        }

        public TryOptionAsync<Unit> End()
        {
            return async () =>
            {
                _subscriber.Dispose();

                return await Task.FromResult(Unit.Default);
            };
        }

        public IObservable<Either<Message<T>, ConsumerException>> GetObservable()
        {
            return _subscriber
                .GetMessageObservable()
                .Select(either => either
                                   .Map(e => new ConsumerException(e))
                                   .MapLeft(inData =>
                                  {
                                      var imessage = (inData as IMessage) == null ? Option<IMessage>.None : Option<IMessage>.Some(inData as IMessage);
                                      return new Message<T>(imessage)
                                      {
                                          Payload = inData.Payload,
                                          Id = inData.Id,
                                          CorrelationId = inData.CorrelationId,
                                          RoutingKey = inData.RoutingKey
                                      };
                                  }));
        }

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration
                .ToTryOptionAsync()
                .Bind<IConfiguration, Unit>(config => async () =>
                {
                    var amqpConfiguration = new AmqpConfiguration(config);
                    var rabbitMqConfig = AmqpProvider.LoadRabbitmqConfiguration(config);

                    _subscriber = AmqpProvider.CreateSubscriber(
                        MessageHandlerFactory.Create<T, T>(r => r),
                        amqpConfiguration.AmqpContexts.FirstOrDefault(),
                        rabbitMqConfig,
                        _rabbitMqConnectionFactory,
                        _converterProvider);

                    return await Task.FromResult(Unit.Default);
                });
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