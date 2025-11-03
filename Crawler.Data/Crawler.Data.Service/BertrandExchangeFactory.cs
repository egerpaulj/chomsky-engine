using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Data.Repository;
using Crawler.DataModel;
using Microservice.Exchange.Bertrand;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Exchange.Endpoints.Mongodb;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using IMongRepositoryFactory = Microservice.Mongodb.Repo.IRepositoryFactory;

namespace Crawler.Data.Service;

public interface IBertrandExchangeFactory
{
    Task<IBertrandExchange> CreateExchange(CancellationToken cancellationToken);
}

public class BertrandExchangeFactory(
    ICrawlerResponseShardedRepository crawlerResponseShardedRepository,
    IMongRepositoryFactory mongodbFactory,
    IJsonConverterProvider jsonConverterProvider,
    ILoggerFactory loggerFactory,
    IConfiguration configuration
) : IBertrandExchangeFactory
{
    private const string MongoDbResponseConsumerName = "mongo-crawler-response-consumer";
    private const string CrawlerResponseMongoDbConsumerRoutingKey = "mongo-crawler-response-route";

    public async Task<IBertrandExchange> CreateExchange(CancellationToken cancellationToken)
    {
        var exchangeManager = CreateExchangeManager(
            out var bertrandStateStore,
            out var bertrandExchangeStore,
            out var bertrandStateRepository
        );

        // CONSUMERS
        var crawlerResponseDatabaseConfig = new DatabaseConfiguration(
            collectionName: "crawler_responses",
            configuration
        );

        var crawlerResponseConsumer = new MongoDbBertrandBatchConsumer<CrawlResponseModel>(
            name: MongoDbResponseConsumerName,
            logger: loggerFactory.CreateLogger<MongoDbBertrandBatchConsumer<CrawlResponseModel>>(),
            repository: await mongodbFactory.CreateRepositoryAsync<CrawlResponseModel>(
                crawlerResponseDatabaseConfig
            ),
            queryFilterDefinition: Builders<BsonDocument>.Filter.Eq("UriStr", BsonNull.Value),
            routingKey: CrawlerResponseMongoDbConsumerRoutingKey,
            batchSize: 100
        );

        // TRANSFORMERS
        var crawlerResponseTransformer = new CrawlerResponseTransformer<CrawlResponseModel>(
            loggerFactory.CreateLogger<IBertrandTransformer>(),
            await mongodbFactory.CreateRepositoryAsync<CrawlResponseModel>(
                crawlerResponseDatabaseConfig
            ),
            crawlerResponseShardedRepository
        );

        // Connect Consumer --> Transformer
        var transformerFilters = new List<IBetrandTransformerFilter>()
        {
            new BertrandRoutingKeyFilter(
                routingKey: CrawlerResponseMongoDbConsumerRoutingKey,
                matchingTargetName: crawlerResponseTransformer.Name
            ),
        };
        var exchangeName = "Data-Pipeline-Exchange";

        return new BertrandExchange(
            exchangeName,
            consumers: [crawlerResponseConsumer],
            transformers: [crawlerResponseTransformer],
            transformerFilters: transformerFilters,
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
            collectionName: "bertrand_exchange_datapipeline_state",
            configuration
        );
        var deadletterStoreConfiguration = new DatabaseConfiguration(
            collectionName: "bertrand_exchange_datapipeline_deadletter",
            configuration
        );
        var exchangeStoreConfiguration = new DatabaseConfiguration(
            collectionName: "bertrand_exchange_datapipeline_exchange_store",
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
