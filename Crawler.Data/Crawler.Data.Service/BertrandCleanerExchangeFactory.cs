using System;
using System.Threading;
using System.Threading.Tasks;
using Crawler.DataModel;
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

public class BertrandCleanerExchangeFactory(
    IMongRepositoryFactory mongodbFactory,
    IJsonConverterProvider jsonConverterProvider,
    ILoggerFactory loggerFactory,
    IConfiguration configuration,
    IConfigurationRepository configurationRepository,
    IAmqpProvider amqpProvider,
    IAmqpBootstrapper amqpBootstrapper
) : IBertrandExchangeFactory
{
    private const string MongoDbResponseConsumerName = "mongo-crawler-response-consumer";
    private const string CrawlerResponseMongoDbConsumerRoutingKey = "mongo-crawler-response-route";
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

        // READ FROM
        var readRespository = await mongodbFactory.CreateRepositoryAsync<CrawlResponseModel>(
            crawlerResponseDatabaseConfig
        );

        var crawlerResponseConsumer = new MongoDbBertrandBatchConsumer<CrawlResponseModel>(
            name: MongoDbResponseConsumerName,
            logger: loggerFactory.CreateLogger<MongoDbBertrandBatchConsumer<CrawlResponseModel>>(),
            repository: readRespository,
            queryFilterDefinition: Builders<BsonDocument>.Filter.Eq(
                "DataPipelineUpdated",
                BsonNull.Value
            ),
            routingKey: CrawlerResponseMongoDbConsumerRoutingKey,
            batchSize: 100
        );

        var crawlerResponseConfiguration = new DatabaseConfiguration(
            "crawler_responses_cleaned",
            configuration
        );

        // WRITE TO
        var writeRepository = await mongodbFactory.CreateRepositoryAsync<DataPipelineCleanText>(
            crawlerResponseConfiguration
        );

        // PUBLISH TO WORKER QUEUE
        var cleanedAmqpPublisher = await amqpProvider
            .GetPublisher(CleanedRabbitMqContextName)
            .Match(r => r, () => throw new Exception("Failed to get publisher"));

        var context = amqpProvider
            .GetContext(CleanedRabbitMqContextName)
            .Match(
                r => r,
                () => throw new Exception("Failed to find rabbitmq context: DatapipelineCleanData")
            );

        await amqpBootstrapper
            .CreateQueue(context.Name, context.Exchange, context.RoutingKey)
            .Match(r => r, () => throw new Exception("Failed to create queue"));

        var cleanedRabbitMqPublisher = new RabbitMqPublisher<DataPipelineCleanText>(
            CleanedRabbitMqContextName,
            cleanedAmqpPublisher
        );

        // TRANSFORMER - clean and publish/write to targets
        var crawlerResponseTransformer = new CrawlerResponseCleanerTransformer<CrawlResponseModel>(
            loggerFactory.CreateLogger<IBertrandTransformer>(),
            readRespository,
            writeRepository,
            cleanedRabbitMqPublisher,
            configurationRepository
        );

        var exchangeName = "Data-Pipeline-Cleaner-Exchange";
        return new BertrandExchange(
            exchangeName,
            consumers: [crawlerResponseConsumer],
            transformers: [crawlerResponseTransformer],
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
