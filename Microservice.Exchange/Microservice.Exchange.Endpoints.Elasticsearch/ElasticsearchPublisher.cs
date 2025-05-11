//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2024  Paul Eger

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

using System.Threading.Tasks;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Elasticsearch.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Elasticsearch
{
    public class ElasticsearchPublisher<T, R> : IPublisher<R>, IConfigInitializor
        where R : IDataModel
    {
        public string Name => "Elasticsearch";

        private readonly ILogger<ElasticsearchPublisher<T, R>> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private string _index;
        private ElasticsearchRepository _repository;

        public ElasticsearchPublisher(
            ILoggerFactory loggerFactory,
            ILogger<ElasticsearchPublisher<T, R>> logger,
            IJsonConverterProvider jsonConverterProvider
        )
        {
            _loggerFactory = loggerFactory;
            _logger = logger;
            _jsonConverterProvider = jsonConverterProvider;
        }

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration
                .ToTryOptionAsync()
                .Bind<IConfiguration, Unit>(config =>
                    async () =>
                    {
                        _index = config.GetValue<string>("Index");
                        _repository = new ElasticsearchRepository(
                            _loggerFactory.CreateLogger<ElasticsearchRepository>(),
                            config,
                            _jsonConverterProvider
                        );

                        return await Task.FromResult(Unit.Default);
                    }
                );
        }

        public TryOptionAsync<Unit> Publish(Option<Message<R>> message)
        {
            return message
                .ToTryOptionAsync()
                .Bind(m => _repository.IndexDocument<R>(m.Payload, _index));
        }
    }

    public class ElasticsearchPublisher<T>(
        string name,
        string index,
        IElasticsearchRepository repository
    ) : IPublisher<T>
        where T : IDataModel
    {
        public string Name { get; } = name;
        private string index { get; } = index;
        public IElasticsearchRepository repository = repository;

        public TryOptionAsync<Unit> Publish(Option<Message<T>> message)
        {
            return message
                .Bind(m => m.Payload)
                .ToTryOptionAsync()
                .Bind(p => repository.IndexDocument<T>(p, index));
        }
    }
}
