using System.Collections.Generic;
using System.Linq;
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

public class BertrandCleanerExchangeFactory(
    IMongRepositoryFactory mongodbFactory,
    IJsonConverterProvider jsonConverterProvider,
    ILoggerFactory loggerFactory,
    IConfiguration configuration,
    IConfigurationRepository configurationRepository,
    ICrawlerResponseShardedRepository crawlerResponseShardedRepository
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
        var consumers = await CreateConsumers();

        var crawlerResponseConfiguration = new DatabaseConfiguration(
            "crawler_responses_cleaned",
            configuration
        );

        // PUBLISHERS - None (Transformer publishes)
        
        // TRANSFORMER - clean and write to targets
        var datalakeRepository = await mongodbFactory.CreateRepositoryAsync<DataPipelineCleanText>(
            new DatabaseConfiguration(
                "crawler_responses_datalake",
                configuration
            )
        );

        var crawlerResponseTransformer = new CrawlerResponseCleanerTransformer<CrawlResponseModel>(
            loggerFactory.CreateLogger<IBertrandTransformer>(),
            readRepository: crawlerResponseShardedRepository,
            writeRepository: datalakeRepository,
            configurationRepository
        );

        var exchangeName = "Data-Pipeline-Cleaner-Exchange";
        return new BertrandExchange(
            exchangeName,
            consumers: consumers.ToList(),
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

    private async Task<IEnumerable<IBertrandConsumer>> CreateConsumers()
    {
        // READ FROM Data sources
        return
        [
            CreateMongoBatchConsumer(
                await CreateRepository("crawler_responses_www.theguardian.com")
            ),
            CreateMongoBatchConsumer(await CreateRepository("crawler_responses_www.cdc.gov")),
            CreateMongoBatchConsumer(await CreateRepository("crawler_responses_www.amnesty.org")),
            CreateMongoBatchConsumer(await CreateRepository("crawler_responses_chomsky.info")),
            CreateMongoBatchConsumer(await CreateRepository("crawler_responses_www.medialens.org")),
            CreateMongoBatchConsumer(await CreateRepository("crawler_responses_press.un.org")),
            CreateMongoBatchConsumer(await CreateRepository("crawler_responses_main.un.org")),
            CreateMongoBatchConsumer(await CreateRepository("crawler_responses_undocs.org")),
            CreateMongoBatchConsumer(await CreateRepository("crawler_responses_www.undocs.org")),
            CreateMongoBatchConsumer(await CreateRepository("crawler_responses_docs.un.org")),
        ];
    }

    private async Task<IMongoDbRepository<CrawlResponseModel>> CreateRepository(
        string collectionName
    )
    {
        var databaseConfig = new DatabaseConfiguration(
            collectionName: collectionName,
            configuration
        );
        var repository = await mongodbFactory.CreateRepositoryAsync<CrawlResponseModel>(
            databaseConfig
        );
        return repository;
    }

    private IBertrandConsumer CreateMongoBatchConsumer(
        IMongoDbRepository<CrawlResponseModel> readRespository
    )
    {
        return new MongoDbBertrandBatchConsumer<CrawlResponseModel>(
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
