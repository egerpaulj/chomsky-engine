using System.Threading.Tasks;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Elasticsearch.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Endpoints.Elasticsearch
{
    public class ElasticsearchPublisher<T, R> : IPublisher<R>, IConfigInitializor where R : IDataModel
    {
        public string Name => "Elasticsearch";

        private readonly ILogger<ElasticsearchPublisher<T, R>> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private string _index;
        private ElasticsearchRepository _repository;

        public ElasticsearchPublisher(ILoggerFactory loggerFactory, ILogger<ElasticsearchPublisher<T, R>> logger, IJsonConverterProvider jsonConverterProvider)
        {
            _loggerFactory = loggerFactory;
            _logger = logger;
            _jsonConverterProvider = jsonConverterProvider;
        }

        public TryOptionAsync<Unit> Initialize(Option<IConfiguration> configuration)
        {
            return configuration.ToTryOptionAsync().Bind<IConfiguration, Unit>(config => async () =>
            {
                _index = config.GetValue<string>("Index");
                _repository = new ElasticsearchRepository(_loggerFactory.CreateLogger<ElasticsearchRepository>(), config, _jsonConverterProvider);


                return await Task.FromResult(Unit.Default);
            });
        }

        public TryOptionAsync<Unit> Publish(Option<Message<R>> message)
        {
            return message.ToTryOptionAsync().Bind(m => _repository.IndexDocument<R>(m.Payload, _index));
        }
    }
}