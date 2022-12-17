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
        private IObservable<Either<Message<T>, ConsumerException>> _observable;
        private readonly ILogger<ElasticsearchConsumer<T>> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private string _query;
        private string _index;
        private IElasticsearchRepository _repository;
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

                _pollingConsumer = new PollingConsumer<T>(_logger, config, () => _repository.Search<T>(_index, _query));

                return await Task.FromResult(Unit.Default);
            });
        }

        public TryOptionAsync<Unit> Start()
        {
            return _pollingConsumer.Start(_observer);
        }

        private async Task RunQuery()
        {
            await _repository.Search<T>(_index, _query).Match(
            resultList =>
            {
                if (resultList.Count == 0)
                    return;

                _logger.LogInformation($"Received data from Elasticsearch. Count: {resultList.Count}");

                foreach (var result in resultList)
                {
                    _observer.OnNext(new Message<T>
                    {
                        Payload = result,
                        Id = result.Id,
                        CorrelationId = Guid.NewGuid(),

                    });
                }
            },
            // EMPTY
            () =>
            {
                _observer.OnNext(new ConsumerException(new Exception("Elasticsearch Query: Empty result")));
            },
            // ERROR
            ex =>
            {
                _observer.OnNext(new ConsumerException(ex));
            });
        }
    }
}