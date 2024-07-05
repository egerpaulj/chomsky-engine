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

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Timers;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Elasticsearch.Repo;
using Microservice.Exchange.Core.Polling;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Elasticsearch
{
    public class ElasticsearchConsumer<T> : IConsumer<T>, IConfigInitializor where T : class, IDataModel
    {
        private IObserver<Either<Message<T>, ConsumerException>> _observer;
        private readonly IObservable<Either<Message<T>, ConsumerException>> _observable;
        private readonly ILogger<ElasticsearchConsumer<T>> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private string _query;
        private string _index;
        private ElasticsearchRepository _repository;
        private PollingConsumer<T> _pollingConsumer;

        public ElasticsearchConsumer(ILoggerFactory loggerFactory, ILogger<ElasticsearchConsumer<T>> logger, IJsonConverterProvider jsonConverterProvider)
        {
            
            _observable = Observable.Create<Either<Message<T>, ConsumerException>>(observer =>
            {
                _observer = observer;
                return Disposable.Empty;
            });

            _loggerFactory = loggerFactory;
            _logger = logger;
            _jsonConverterProvider = jsonConverterProvider;
        }

        public TryOptionAsync<Unit> End()
        {
            return _pollingConsumer.End();
        }

        public IObservable<Either<Message<T>, ConsumerException>> GetObservable()
        {
            return _observable;
        }

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration.ToTryOptionAsync().Bind<IConfiguration, Unit>(config => async () =>
            {
                _query = config.GetValue<string>("Query");
                _index = config.GetValue<string>("Index");
                _repository = new ElasticsearchRepository(_loggerFactory.CreateLogger<ElasticsearchRepository>(), config, _jsonConverterProvider);

                _pollingConsumer = new PollingConsumer<T>(_logger, () => _repository.Search<T>(_index, _query), config.GetValue<int>(PollingConfiguration.IntervalInMsKey));

                return await Task.FromResult(Unit.Default);
            });
        }

        public TryOptionAsync<Unit> Start()
        {
            return _pollingConsumer.Start(_observer);
        }
    }
}