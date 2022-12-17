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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Timers;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Exchange.Core.Polling;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microservice.Exchange.Endpoints.Mongodb
{
    public class MongodbConsumer<T> : IConsumer<T>, IConfigInitializor where T : IDataModel
    {
        private readonly ILogger<IConsumer<T>> _logger;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private IObserver<Either<Message<T>, ConsumerException>> _observer;
        private IObservable<Either<Message<T>, ConsumerException>> _observable;
        
        private FilterDefinition<BsonDocument> _filters;
        private MongoDbRepository<T> _repository;
        private PollingConsumer<T> _pollingConsumer;
        

        public MongodbConsumer(ILogger<MongodbConsumer<T>> logger, IJsonConverterProvider jsonConverterProvider)
        {
            _observable = Observable.Create<Either<Message<T>, ConsumerException>>(observer =>
            {
                _observer = observer;
                return Disposable.Empty;
            });

            _logger = logger;
            _jsonConverterProvider = jsonConverterProvider;
        }
        public TryOptionAsync<Unit> End()
        {
            return _pollingConsumer?.End();
        }

        public IObservable<Either<Message<T>, ConsumerException>> GetObservable()
        {
            return _observable;
        }

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration.ToTryOptionAsync().Bind<IConfiguration, Unit>(config => async () =>
            {
                var databaseConfiguration = new DatabaseConfiguration
                {
                    DatabaseName = config.GetValue<string>("DatabaseName"),
                    DocumentName = config.GetValue<string>("DocumentName")
                };
                
                _filters = await QueryParser.Parse(configuration).Match(r => r, () => throw new ExchangeBootstrapException("MongodbConsumer Configuration missing: DocumentFilters"));
                _repository = new MongoDbRepository<T>(config, databaseConfiguration, _jsonConverterProvider);

                _pollingConsumer = new PollingConsumer<T>(_logger, config, () => _repository.GetMany(_filters));

                return await Task.FromResult(Unit.Default);
            });
        }

        public TryOptionAsync<Unit> Start()
        {
            return _pollingConsumer?.Start(_observer);
        }
    }
}