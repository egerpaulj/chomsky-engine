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
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.ClassInstances;
using Microservice.DataModel.Core;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Mongodb
{
    public class MongodbPublisher<T, R>(
        ILogger<MongodbPublisher<T, R>> logger,
        IJsonConverterProvider jsonConverterProvider
    ) : IPublisher<R>, IDeadletterPublisher<T, R>, IConfigInitializor
        where R : IDataModel
    {
        public string Name => "Mongodb";

        private readonly IJsonConverterProvider _jsonConverterProvider = jsonConverterProvider;
        private MongoDbRepository<R> _repository;
        private readonly MongoDbRepository<DeadletterModel> _deadletterRepository = null;

        readonly ILogger<MongodbPublisher<T, R>> _logger = logger;

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration
                .ToTryOptionAsync()
                .Bind<IConfiguration, Unit>(config =>
                    async () =>
                    {
                        var databaseConfiguration = new DatabaseConfiguration(
                            collectionName: config.GetValue<string>("CollectionName"),
                            databaseName: config.GetValue<string>("DatabaseName")
                        );

                        _repository = new MongoDbRepository<R>(
                            config,
                            databaseConfiguration,
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
                .Bind(m => _repository.AddOrUpdate(m.Payload))
                .Bind<Guid, Unit>(g =>
                    async () =>
                    {
                        _logger.LogInformation(
                            "Published message successfully. Inserted new Document. Document id: {g}",
                            g
                        );
                        return await Task.FromResult(Unit.Default);
                    }
                );
        }

        public TryOptionAsync<Unit> PublishError(Option<ErrorMessage<T>> message)
        {
            return message
                .ToTryOptionAsync()
                .Bind(error =>
                    _deadletterRepository.AddOrUpdate(
                        new DeadletterModel
                        {
                            ErrorMessage = error.ExceptionMessage,
                            Data = _jsonConverterProvider.Serialize(error.Message),
                        }
                    )
                )
                .Bind<Guid, Unit>(g => async () => await Task.FromResult(Unit.Default));
        }

        public TryOptionAsync<Unit> PublishError(Option<ErrorMessage<R>> message)
        {
            return message
                .ToTryOptionAsync()
                .Bind(error =>
                    _deadletterRepository.AddOrUpdate(
                        new DeadletterModel
                        {
                            ErrorMessage = error.ExceptionMessage,
                            Data = _jsonConverterProvider.Serialize(error.Message),
                        }
                    )
                )
                .Bind<Guid, Unit>(g => async () => await Task.FromResult(Unit.Default));
        }

        public TryOptionAsync<Unit> PublishError(Option<string> message)
        {
            return message
                .ToTryOptionAsync()
                .Bind(error =>
                    _deadletterRepository.AddOrUpdate(new DeadletterModel { ErrorMessage = error })
                )
                .Bind<Guid, Unit>(g => async () => await Task.FromResult(Unit.Default));
        }
    }

    public class MongoDbPublisher<T>(string name, IMongoDbRepository<T> repository) : IPublisher<T>
        where T : IDataModel
    {
        public string Name { get; } = name;
        public IMongoDbRepository<T> repository = repository;

        public TryOptionAsync<Unit> Publish(Option<Message<T>> message)
        {
            return message
                .Bind(m => m.Payload)
                .ToTryOptionAsync()
                .Bind(p => repository.AddOrUpdate(p))
                .Bind<Guid, Unit>(p => async () => await Task.FromResult(Unit.Default));
        }
    }
}
