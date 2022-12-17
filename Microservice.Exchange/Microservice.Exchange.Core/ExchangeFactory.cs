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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Exchange.Filter;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange
{
    /// <summary>
    /// Creates instances of <see cref="IMessageExchange"/> based on a configuration.
    /// </summary>
    public interface IExchangeFactory
    {
        /// <summary>
        /// Creates an <see cref="IMessageExchange"/> based on the <see cref="IConfiguratonSection" />.
        /// </summar>
        TryOptionAsync<IMessageExchange<T, R>> CreateMessageExchange<T, R>(Option<IConfigurationSection> config);
        
        /// <summary>
        /// Creates an <see cref="IMessageExchange"/> based on the <see cref="IConfiguration" /> and an Exchange name.
        /// </summar>
        TryOptionAsync<IMessageExchange<T, R>> CreateMessageExchange<T, R>(Option<IConfiguration> configuration, Option<string> exchangeName);
        
        /// <summary>
        /// Creates an <see cref="IMessageExchange"/> based on the JSON configuration and an Exchange name.
        /// </summar>
        TryOptionAsync<IMessageExchange<T, R>> CreateMessageExchange<T, R>(Option<string> jsonConfig, Option<string> exchangeName);
    }

    public class ExchangeFactory : IExchangeFactory
    {
        private const string TypeNameKey = "TypeName";
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ExchangeFactory> _logger;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private readonly IExchangeMetrics _exchangeMetrics;

        public ExchangeFactory(ILoggerFactory loggerFactory, IJsonConverterProvider jsonConverterProvider, IServiceProvider serviceProvider, IExchangeMetrics exchangeMetrics)
        {
            _loggerFactory = loggerFactory;
            _jsonConverterProvider = jsonConverterProvider;
            _serviceProvider = serviceProvider;
            _exchangeMetrics = exchangeMetrics;

            _logger = _loggerFactory.CreateLogger<ExchangeFactory>();
        }

        public TryOptionAsync<IMessageExchange<T, R>> CreateMessageExchange<T, R>(Option<IConfigurationSection> config)
        {
            return config
                .ToTryOptionAsync()
                .Bind<IConfigurationSection, IMessageExchange<T, R>>(configuration => async () =>
                    {
                        var mappings = GetTypeMappings(configuration);
                        var dataInType = typeof(T);
                        var dataOutType = typeof(R);
                        var consumers = await CreateConsumers<T>(configuration, dataInType, dataOutType, mappings);

                        var filterConfiguration = configuration.GetSection("Filter").GetChildren().FirstOrDefault();
                        var filter = filterConfiguration?.Value != null || filterConfiguration.Exists()
                                        ? await CreateInstance<IFilter<T, R>>(Option<IConfigurationSection>.Some(filterConfiguration), dataInType, dataOutType, mappings)
                                        : new AlwaysMatchFilter<T, R>();

                        var publishers = await CreatePublisher<R>(configuration, dataInType, dataOutType, mappings);

                        var transformerConfig = configuration.GetSection("Transformer").GetChildren().FirstOrDefault();
                        var transformer = transformerConfig?.Value == null && typeof(T) == typeof(R)
                            ? new SameTypeTransformer<T, R>()
                            : await CreateInstance<ITransformer<T, R>>(Option<IConfigurationSection>.Some(transformerConfig), dataInType, dataOutType, mappings);

                        var deadletterConfig = configuration.GetSection("Deadletter").GetChildren().FirstOrDefault();
                        var deadletterPublisher = await CreateInstance<IDeadletterPublisher<T, R>>(Option<IConfigurationSection>.Some(deadletterConfig), dataInType, dataOutType, mappings);


                        return new MessageExchange<T, R>(
                            _loggerFactory.CreateLogger<MessageExchange<T, R>>(),
                            consumers,
                            publishers,
                            transformer,
                            deadletterPublisher,
                            filter,
                            _jsonConverterProvider,
                            _exchangeMetrics,
                            configuration.Key
                            );
                    });
        }

        public TryOptionAsync<IMessageExchange<T, R>> CreateMessageExchange<T, R>(Option<IConfiguration> configuration, Option<string> exchangeName)
        {
            return configuration
                .ToTryOptionAsync()
                .Bind(config =>
            {
                var exchange = exchangeName.Match(ex => ex, () => throw new ArgumentNullException("exchangeName"));
                var exchangeConfig = config
                    .GetSection("MessageExchanges")
                    .GetSection(exchange);
                return CreateMessageExchange<T, R>(Option<IConfigurationSection>.Some(exchangeConfig));
            });
        }

        public TryOptionAsync<IMessageExchange<T, R>> CreateMessageExchange<T, R>(Option<string> jsonConfig, Option<string> exchangeName)
        {
            return jsonConfig
                .ToTryOptionAsync()
                .Bind(config =>
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(config)))
                    {
                        var configuration = new ConfigurationBuilder()
                                        .AddJsonStream(stream)
                                        .Build();

                        return CreateMessageExchange<T, R>(Option<IConfiguration>.Some(configuration), exchangeName);
                    }
                });
        }

        private async Task<T> CreateInstance<T>(Option<IConfigurationSection> config, Type dataInType, Type dataOutType, Dictionary<string, string> typeMappings)
        {
            return await config
                .ToTryOptionAsync()
                .Bind<IConfigurationSection, T>(
                    configuration =>
                    {
                        
                        var typeName = typeMappings.ContainsKey(configuration.Key) 
                                        ? typeMappings[configuration.Key] 
                                        : configuration.GetValue<string>(TypeNameKey);

                        var itemType = Type.GetType(typeName, true);

                        T item;

                        if (itemType.IsGenericType)
                        {
                            var genericParameters = new List<Type>() { dataInType };
                            if (itemType.GetGenericArguments().Length > 1 && dataOutType != null)
                                genericParameters.Add(dataOutType);

                            var genericType = itemType.MakeGenericType(genericParameters.ToArray());

                            item = (T)ActivatorUtilities.CreateInstance(_serviceProvider, genericType);
                        }
                        else
                            item = (T)ActivatorUtilities.CreateInstance(_serviceProvider, itemType);

                        _logger.LogInformation($"Created instance of: {itemType.Name}");

                        var itemInitializable = item as IConfigInitializor;
                        if (itemInitializable != null)
                        {
                            return itemInitializable
                                .Initialize(Option<IConfiguration>.Some(configuration))
                                .Map(_ => item);
                        }

                        return async () => await Task.FromResult(item);
                    })
                .Match(
                    r => r,
                    () => throw new ExchangeBootstrapException($"Empty configuration. Failed to create: {typeof(T).Name}"),
                    ex => throw new ExchangeBootstrapException($"Empty configuration. Failed to create: {typeof(T).Name}", ex));
        }

        private async Task<List<IPublisher<T>>> CreatePublisher<T>(IConfiguration configuration, Type dataInType, Type dataOutType, Dictionary<string, string> mappings)
        {
            var publishers = new List<IPublisher<T>>();

            foreach (var publisherConfig in configuration
                                        .GetSection("DataOut")
                                        .GetChildren())
            {
                publishers.Add(await CreateInstance<IPublisher<T>>(Option<IConfigurationSection>.Some(publisherConfig), dataInType, dataOutType, mappings));
            }

            return publishers;
        }

        private async Task<List<IConsumer<T>>> CreateConsumers<T>(IConfiguration configuration, Type dataInType, Type dataOutType, Dictionary<string, string> mappings)
        {
            var consumerConfigurations = configuration
                                        .GetSection("DataIn")
                                        .GetChildren();

            var consumers = new List<IConsumer<T>>();

            foreach (var consumerConfig in consumerConfigurations)
            {
                var parallelConsumers = consumerConfig.GetValue<int>("NumberOfParallelConsumers");
                if (parallelConsumers > 1)
                {
                    for (var i = 0; i < parallelConsumers; i++)
                    {
                        consumers.Add(await CreateInstance<IConsumer<T>>(Option<IConfigurationSection>.Some(consumerConfig), dataInType, dataOutType, mappings));
                    }

                    continue;
                }

                consumers.Add(await CreateInstance<IConsumer<T>>(Option<IConfigurationSection>.Some(consumerConfig), dataInType, dataOutType, mappings));
            }

            if (consumers.Count == 0)
                throw new ExchangeBootstrapException("Consumers not configured. Not possible to start a Message Exchange without Input Data");

            return consumers;
        }

        private Dictionary<string, string> GetTypeMappings(IConfiguration configuration)
        {
            var mappings = new Dictionary<string, string>();

            foreach (var mapping in configuration.GetSection("TypeMappings").GetChildren().ToList())
            {
                if (mappings.Keys.Exists<string>(k => k == mapping.Key))
                {
                    _logger.LogWarning($"Duplicate keys for TypeMappings. Ignoring duplicate key: {mapping.Key}");
                    continue;
                }

                mappings.Add(mapping.Key, mapping.Value);
            }

            return mappings;
        }
    }
}