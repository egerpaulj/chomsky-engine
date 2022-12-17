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
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Amqp.Configuration;
using Microservice.Amqp.Rabbitmq;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Rabbitmq
{
    /// <summary>
    /// Publishes Messages and Errors to a target data source.
    /// </summary>
    public class RabbitMqPublisher<T, R> : IPublisher<R>, IDeadletterPublisher<T, R>, IConfigInitializor
    {
        public string Name { get; private set; }
        private readonly IJsonConverterProvider _converterProvider;
        private readonly IRabbitMqConnectionFactory _rabbitMqConnectionFactory;
        private readonly ILogger<RabbitMqPublisher<T, R>> _logger;

        private MessagePublisher _publisher;
        private AmqpContextConfiguration _contextConfiguration;

        public RabbitMqPublisher(ILogger<RabbitMqPublisher<T, R>> logger, IJsonConverterProvider converterProvider, IRabbitMqConnectionFactory rabbitMqConnectionFactory)
        {
            _converterProvider = converterProvider;
            _rabbitMqConnectionFactory = rabbitMqConnectionFactory;
            _logger = logger;
        }

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration.ToTryOptionAsync().Bind<IConfiguration, Unit>(config => async () =>
            {
                var amqpConfiguration = new AmqpConfiguration(config);
                var rabbitMqConfig = AmqpProvider.LoadRabbitmqConfiguration(config);
                Name = "RabbitMqPublisher";
                _contextConfiguration = amqpConfiguration.AmqpContexts.FirstOrDefault();

                _publisher = AmqpProvider.CreatePublisher(Name, _contextConfiguration, rabbitMqConfig, _converterProvider, _rabbitMqConnectionFactory);

                return await Task.FromResult(Unit.Default);
            });
        }

        public TryOptionAsync<Unit> Publish(Option<Message<R>> message)
        {
            return message.ToTryOptionAsync().Bind(m => _publisher.Publish<R>(new Microservice.Amqp.Message<R>
            {
                Id = m.Id,
                CorrelationId = m.CorrelationId,
                RoutingKey = _contextConfiguration.RoutingKey,
                Context = Name,
                Payload = m.Payload
            }));
        }

        public TryOptionAsync<Unit> PublishError(Option<ErrorMessage<T>> message)
        {
            return message.ToTryOptionAsync().Bind(m => _publisher.Publish<ErrorMessage<T>>(m));
        }

        public TryOptionAsync<Unit> PublishError(Option<ErrorMessage<R>> message)
        {
            return message.ToTryOptionAsync().Bind(m => _publisher.Publish<ErrorMessage<R>>(m));
        }

        public TryOptionAsync<Unit> PublishError(Option<string> message)
        {
            return message.ToTryOptionAsync().Bind(m => _publisher.Publish<string>(m));
        }
    }
}