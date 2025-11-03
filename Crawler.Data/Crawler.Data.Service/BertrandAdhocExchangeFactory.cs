using System;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core.Requests;
using Crawler.Data.Adhoc;
using Crawler.Data.Repository;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.RequestHandling.Core;
using Microservice.Amqp;
using Microservice.Exchange.Bertrand;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Exchange.Endpoints.Mongodb;
using Microservice.Exchange.Endpoints.Rabbitmq;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using IMongRepositoryFactory = Microservice.Mongodb.Repo.IRepositoryFactory;

namespace Crawler.Data.Service;

public class BertrandAdhocExchangeFactory(
    IMongRepositoryFactory mongodbFactory,
    IJsonConverterProvider jsonConverterProvider,
    ILoggerFactory loggerFactory,
    IConfiguration configuration,
    IConfigurationRepository configurationRepository,
    ISchedulerRepository schedulerRepository,
    ICrawlerResponseShardedRepository crawlerResponseShardedRepository,
    IAmqpProvider amqpProvider,
    IAmqpBootstrapper amqpBootstrapper,
    IRequestPublisher requestPublisher
) : IBertrandExchangeFactory
{
    private const string AdhocConsumerName = "adhoc-request-consumer";

    private const string CleanedRabbitMqContextName = "DatapipelineCleanData";

    public async Task<IBertrandExchange> CreateExchange(CancellationToken cancellationToken)
    {
        var exchangeManager = CreateExchangeManager(
            out var bertrandStateStore,
            out var bertrandExchangeStore,
            out var bertrandStateRepository
        );

        // CONSUMERS
        var crawlerResponseDatabaseConfig = new DatabaseConfiguration(
            collectionName: "crawler_responses_www.theguardian.com",
            configuration
        );

        // Adhoc consumer
        var adhocConsumer = new RabbitMqBertrandConsumer<CrawlRequest>(
            amqpProvider,
            contextName: "CrawlRequest",
            name: "cleanup",
            queueName: "www.theguardian.com."
        );

        // TRANSFORMER - clean and publish/write to targets
        var adhocTransformer = new AdhocTransformer<CrawlRequest>(
            loggerFactory.CreateLogger<IBertrandTransformer>(),
            requestPublisher: requestPublisher,
            configurationRepository,
            schedulerRepository: schedulerRepository,
            responseShardedRepository: crawlerResponseShardedRepository
        );

        var exchangeName = "Data-Pipeline-Adhoc-Exchange";
        return new BertrandExchange(
            exchangeName,
            consumers: [adhocConsumer],
            transformers: [adhocTransformer],
            transformerFilters: [],
            publisherFilters: [],
            publishers: [],
            loggerFactory.CreateLogger<BertrandExchange>(),
            new BertrandMetrics(exchangeName),
            bertrandStateStore,
            bertrandExchangeStore,
            exchangeManager
        );
    }

    private BertrandExchangeManager CreateExchangeManager(
        out MongoDbBertrandStateStore bertrandStateStore,
        out MongoDbBertrandExchangeStore bertrandExchangeStore,
        out IMongoDbRepository<BertrandStateDataModel> bertrandStateRepository
    )
    {
        var stateStoreConfiguration = new DatabaseConfiguration(
            collectionName: "bertrand_exchange_datapipeline_cleaner_state",
            configuration
        );
        var deadletterStoreConfiguration = new DatabaseConfiguration(
            collectionName: "bertrand_exchange_datapipeline_cleaner_deadletter",
            configuration
        );
        var exchangeStoreConfiguration = new DatabaseConfiguration(
            collectionName: "bertrand_exchange_datapipeline_cleaner_exchange_store",
            configuration
        );

        bertrandStateRepository = mongodbFactory
            .CreateRepositoryAsync<BertrandStateDataModel>(stateStoreConfiguration)
            .Result;
        var bertrandStateDeadletterRepository = mongodbFactory
            .CreateRepositoryAsync<BertrandStateDataModel>(deadletterStoreConfiguration)
            .Result;
        bertrandStateStore = new MongoDbBertrandStateStore(
            jsonConverterProvider,
            bertrandStateRepository,
            bertrandStateDeadletterRepository
        );

        var bertrandExchangeRepository = mongodbFactory
            .CreateRepositoryAsync<BertrandExchangeDataModel>(exchangeStoreConfiguration)
            .Result;
        bertrandExchangeStore = new MongoDbBertrandExchangeStore(bertrandExchangeRepository);
        return new BertrandExchangeManager(
            bertrandExchangeStore,
            loggerFactory.CreateLogger<BertrandExchangeManager>()
        );
    }
}
