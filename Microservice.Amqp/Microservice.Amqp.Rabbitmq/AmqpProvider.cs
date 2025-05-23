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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Amqp.Configuration;
using Microservice.Amqp.Rabbitmq.Configuration;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;

namespace Microservice.Amqp.Rabbitmq
{
    public class AmqpProvider : IAmqpProvider
    {
        private readonly RabbitmqConfig _configuration;
        private readonly AmqpConfiguration _amqpConfiguration;
        private readonly IJsonConverterProvider _converterProvider;
        private readonly IRabbitMqConnectionFactory _rabbitMqConnectionFactory;

        public AmqpProvider(
            IConfiguration configuration,
            IJsonConverterProvider converterProvider,
            IRabbitMqConnectionFactory rabbitMqConnectionFactory
        )
        {
            _rabbitMqConnectionFactory = rabbitMqConnectionFactory;
            _amqpConfiguration = new AmqpConfiguration(configuration);
            _configuration = LoadRabbitmqConfiguration(configuration);
            _converterProvider = converterProvider;
        }

        public TryOptionAsync<IMessagePublisher> GetPublisher(Option<string> contextName)
        {
            return contextName
                .ToTryOptionAsync()
                .Bind(
                    (Func<string, TryOptionAsync<IMessagePublisher>>)(
                        context =>
                            async () =>
                            {
                                var amqpContext = GetContextInternal(context);
                                var publisher = CreatePublisher(
                                    context,
                                    amqpContext,
                                    _configuration,
                                    _converterProvider,
                                    _rabbitMqConnectionFactory
                                );

                                return await Task.FromResult(publisher);
                            }
                    )
                );
        }

        public static MessagePublisher CreatePublisher(
            string context,
            AmqpContextConfiguration amqpContext,
            RabbitmqConfig configuration,
            IJsonConverterProvider converterProvider,
            IRabbitMqConnectionFactory rabbitMqConnectionFactory
        )
        {
            var publisherConfig = CreatePublisherConfiguration(context, amqpContext, configuration);
            return new MessagePublisher(
                publisherConfig,
                rabbitMqConnectionFactory,
                converterProvider
            );
        }

        public static RabbitMqPublisherConfig CreatePublisherConfiguration(
            string context,
            AmqpContextConfiguration amqpContext,
            RabbitmqConfig configuration
        )
        {
            return new RabbitMqPublisherConfig
            {
                Host = configuration.Host,
                VirtHost = configuration.VirtHost,
                Username = configuration.Username,
                Password = configuration.Password,
                Exchange = amqpContext.Exchange,
                RoutingKey = amqpContext.RoutingKey,
                Context = context,
            };
        }

        public TryOptionAsync<IMessageSubscriber<T, R>> GetSubsriber<T, R>(
            Option<string> contextName,
            IMessageHandler<T, R> messageHandler
        )
        {
            return contextName
                .ToTryOptionAsync()
                .Bind(
                    (Func<string, TryOptionAsync<IMessageSubscriber<T, R>>>)(
                        context =>
                            async () =>
                            {
                                var amqpContext = GetContextInternal(context);
                                var subscriber = CreateSubscriber(
                                    messageHandler,
                                    amqpContext,
                                    _configuration,
                                    _rabbitMqConnectionFactory,
                                    _converterProvider,
                                    queueName: null
                                );

                                return await Task.FromResult(subscriber);
                            }
                    )
                );
        }

        public TryOptionAsync<IMessageSubscriber<T, R>> GetSubsriber<T, R>(
            Option<string> contextName,
            Option<string> queueName,
            IMessageHandler<T, R> messageHandler
        )
        {
            return contextName
                .ToTryOptionAsync()
                .Bind(
                    (Func<string, TryOptionAsync<IMessageSubscriber<T, R>>>)(
                        context =>
                            async () =>
                            {
                                var amqpContext = GetContextInternal(context);
                                var subscriber = CreateSubscriber(
                                    messageHandler,
                                    amqpContext,
                                    _configuration,
                                    _rabbitMqConnectionFactory,
                                    _converterProvider,
                                    queueName.MatchUnsafe(q => q, () => null)
                                );

                                return await Task.FromResult(subscriber);
                            }
                    )
                );
        }

        public Option<AmqpContextConfiguration> GetContext(Option<string> contextName)
        {
            return contextName.Match(
                c => GetContextInternal(c),
                () => throw new Exception("context is empty")
            );
        }

        public static MessageSubscriber<T, R> CreateSubscriber<T, R>(
            IMessageHandler<T, R> messageHandler,
            AmqpContextConfiguration amqpContext,
            RabbitmqConfig configuration,
            IRabbitMqConnectionFactory rabbitMqConnectionFactory,
            IJsonConverterProvider converterProvider,
            string queueName = null
        )
        {
            var subscriberConfig = CreateSubscriberConfiguration(
                amqpContext,
                configuration,
                queueName
            );
            return CreateSubscriber(
                messageHandler,
                subscriberConfig,
                converterProvider,
                rabbitMqConnectionFactory
            );
        }

        public static MessageSubscriber<T, R> CreateSubscriber<T, R>(
            IMessageHandler<T, R> messageHandler,
            RabbitMqSubscriberConfig subscriberConfig,
            IJsonConverterProvider converterProvider,
            IRabbitMqConnectionFactory rabbitMqConnectionFactory
        )
        {
            return new MessageSubscriber<T, R>(
                subscriberConfig,
                converterProvider,
                rabbitMqConnectionFactory,
                messageHandler
            );
        }

        public static RabbitMqSubscriberConfig CreateSubscriberConfiguration(
            AmqpContextConfiguration amqpContext,
            RabbitmqConfig configuration,
            string queueName
        )
        {
            return new RabbitMqSubscriberConfig
            {
                Host = configuration.Host,
                VirtHost = configuration.VirtHost,
                Username = configuration.Username,
                Password = configuration.Password,
                QueueName = queueName ?? amqpContext.QueueName,
                PrefetchCount = amqpContext.PrefetchCount,
            };
        }

        private AmqpContextConfiguration GetContextInternal(string context)
        {
            var match = _amqpConfiguration.AmqpContexts.FirstOrDefault(c => c.Name == context);

            if (match == null)
            {
                throw new Exception($"Failed to find configuration for AMQP context: {context}");
            }

            return match;
        }

        public static RabbitmqConfig LoadRabbitmqConfiguration(IConfiguration configuration)
        {
            var section = configuration
                .GetSection(AmqpConfiguration.AmqpConfigurationRoot)
                .GetSection("Provider")
                .GetChildren()
                .FirstOrDefault();

            if (section == null)
            {
                throw new Exception(
                    $"Configuration missing for AMQP RabbitMq Provider. {configuration.GetChildren().Aggregate(new StringBuilder(), (sb, section) => sb.AppendLine(section.Key), sb => sb.ToString())}"
                );
            }

            return new RabbitmqConfig
            {
                Host = section.GetValue<string>("Host"),
                VirtHost = section.GetValue<string>("VirtHost"),
                Port = section.GetValue<int>("Port"),
                Username = section.GetValue<string>("Username"),
                Password = section.GetValue<string>("Password"),
            };
        }
    }
}
