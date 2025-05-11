using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.Scheduler.Repository;
using Crawler.Stategies.Core;
using Microservice.Amqp;
using Microservice.Exchange;
using Microservice.Exchange.Bertrand;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Exchange.Endpoints.Elasticsearch;
using Microservice.Exchange.Endpoints.Mongodb;
using Microservice.Exchange.Endpoints.Rabbitmq;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using IElasticRepositoryFactory = Microservice.Elasticsearch.Repo.IRepositoryFactory;
using IMongRepositoryFactory = Microservice.Mongodb.Repo.IRepositoryFactory;

namespace Crawler.Management.Service;

public interface IBertrandExchangeFactory
{
    Task<IBertrandExchange> CreateExchange(CancellationToken cancellationToken);
}

public class BertrandExchangeFactory(
    IMongRepositoryFactory mongodbFactory,
    IElasticRepositoryFactory elasticFactory,
    IAmqpProvider amqpProvider,
    IJsonConverterProvider jsonConverterProvider,
    ILoggerFactory loggerFactory,
    ISchedulerRepository schedulerRepository,
    IConfigurationRepository configurationRepository,
    IConfiguration configuration,
    ICrawlStrategyMapper crawlStrategyMapper,
    IBertrandMetrics bertrandMetrics,
    IAmqpBootstrapper amqpBootstrapper,
    IErrorMessageProcessor errorMessageProcessor
) : IBertrandExchangeFactory
{
    private const string Publisher_Uri = "crawler-uri-mongodb-publisher";
    private const string Publisher_Stock_Mongodb = "stock-mongodb-publisher";
    private const string Publisher_Crawl_Mongodb = "crawler-result-mongodb-publisher";
    private const string Publisher_Crawl_Es = "crawler-result-elastic-publisher";
    private const string Transformer_Uri = "crawler-uri-transformer";
    private const string Transformer_Crawl = "crawler-request-transformer";
    private const string Routing_Key_From_Queue_Uri = "uri_queue";
    private const string Routing_Key_Internal_Uri = "uri-data-model";
    private const string Routing_Key_From_Queue_Bind_To_Transformer = "request*";
    private const string Routing_Key_Internal_Crawl = "crawler-request";

    private const string Transformer_Response_Es = $"crawler-response-transformer-es";
    private const string Transformer_Response_Mongodb = $"crawler-response-transformer-mongodb";

    public async Task<IBertrandExchange> CreateExchange(CancellationToken cancellationToken)
    {
        var exchangeManager = CreateExchangeManager(
            out var bertrandStateStore,
            out var bertrandExchangeStore,
            out var bertrandStateRepository
        );

        // Cleanup existing state
        var outstandingItems = await bertrandStateRepository
            .GetMany(FilterDefinition<BsonDocument>.Empty)
            .Match(r => r, []);

        await errorMessageProcessor.ProcessMessages(bertrandStateRepository, cancellationToken);

        // CONSUMERS
        var consumers = await CreateDynamicConsumers();

        var crawlUrlConsumer = new RabbitMqBertrandConsumer<CrawlUri>(
            amqpProvider,
            "CrawlUri",
            "crawl-uri-consumer"
        );

        var crawlRequestConsumer = new RabbitMqBertrandConsumer<CrawlRequest>(
            amqpProvider,
            contextName: "CrawlRequest",
            routingKeyBypass: null,
            name: "crawl-request-consumer",
            queueName: "request_queue"
        );

        consumers.Add(crawlUrlConsumer);
        consumers.Add(crawlRequestConsumer);

        // TRANSFORMERS
        var transformers = CreateTransformers();

        // PUBLISHERS
        var publishers = CreatePublishers(configuration);

        // Connect Consumers --> Transformers --> Publishers
        var publisherFilters = new List<IBertrandPublisherFilter>
        {
            new BertrandRoutingKeyFilter(Routing_Key_Internal_Uri, Publisher_Uri),
            new BertrandRoutingKeyFilter(Transformer_Response_Es, Publisher_Crawl_Es),
            new BertrandRoutingKeyFilter(Transformer_Response_Mongodb, Publisher_Crawl_Mongodb),
            new BertrandRoutingKeyFilter(
                StockDataMongoDbTransformer<CrawlResponse>.StockDataTransormer,
                Publisher_Stock_Mongodb
            ),
        };
        var transformerFilters = new List<IBetrandTransformerFilter>()
        {
            new BertrandRoutingKeyFilter(Routing_Key_From_Queue_Uri, Transformer_Uri),
            new BertrandRoutingKeyFilter(
                Routing_Key_From_Queue_Bind_To_Transformer,
                Transformer_Crawl
            ),
            new BertrandRoutingKeyFilter(Routing_Key_Internal_Crawl, Transformer_Response_Es),
            new BertrandRoutingKeyFilter(Routing_Key_Internal_Crawl, Transformer_Response_Mongodb),
            new BertrandUrlFilter<CrawlResponse>(
                "live-markets/market-data-dashboard/price-explorer",
                StockDataMongoDbTransformer<CrawlResponse>.StockDataTransormer
            ),
        };

        return new BertrandExchange(
            "Crawler-Exchange",
            consumers,
            transformers,
            transformerFilters,
            publisherFilters,
            publishers,
            loggerFactory.CreateLogger<BertrandExchange>(),
            bertrandMetrics,
            bertrandStateStore,
            bertrandExchangeStore,
            exchangeManager
        );
    }

    private async Task<List<IBertrandConsumer>> CreateDynamicConsumers()
    {
        var knownCrawlerRequests = jsonConverterProvider.Deserialize<List<CrawlRequestModel>>(
            File.ReadAllText("RequestRepository/crawl_requests/crawl_requests.json")
        );
        var consumers = new List<IBertrandConsumer>();

        var customRoutingKeyBypass = new Dictionary<string, string> { };

        foreach (var crawlRequest in knownCrawlerRequests)
        {
            await configurationRepository
                .AddOrUpdate(crawlRequest)
                .Match(
                    r => r,
                    () =>
                        throw new Exception(
                            "Failed to synch crawler configuraiton: " + crawlRequest.Uri
                        ),
                    ex => throw ex
                );
            if (!crawlRequest.ShouldDynamicBootstrap)
                continue;

            var consumer = new RabbitMqBertrandConsumer<CrawlRequest>(
                provider: amqpProvider,
                contextName: "CrawlRequest",
                name: $"{crawlRequest.Host}_consumer",
                routingKeyBypass: customRoutingKeyBypass.ContainsKey(crawlRequest.Host)
                    ? customRoutingKeyBypass[crawlRequest.Host]
                    : Routing_Key_From_Queue_Bind_To_Transformer,
                queueName: crawlRequest.Host
            );

            consumers.Add(consumer);

            await amqpBootstrapper
                .CreateQueue(
                    queueName: crawlRequest.Host,
                    exchangeName: "crawler",
                    routingKey: crawlRequest.Host
                )
                .Match(
                    r => r,
                    () => throw new Exception("Failed to create queue: " + crawlRequest.Host)
                );
        }

        return consumers;
    }

    private List<IBertrandTransformer> CreateTransformers()
    {
        DatabaseConfiguration databaseConfiguration = new DatabaseConfiguration(
            "crawler_responses",
            configuration
        );

        var responseRepository = mongodbFactory.CreateRepository<CrawlResponseModel>(
            databaseConfiguration
        );

        var transformers = new List<IBertrandTransformer>();
        var uriTransformer = new UriTransformer<CrawlUri>(
            loggerFactory.CreateLogger<UriTransformer<CrawlUri>>(),
            schedulerRepository,
            configurationRepository,
            responseRepository,
            Transformer_Uri,
            Routing_Key_Internal_Uri
        );

        var crawlRequestTransformer = new CrawlRequestTransformer<CrawlRequest, CrawlResponse>(
            loggerFactory.CreateLogger<CrawlRequestTransformer<CrawlRequest, CrawlResponse>>(),
            crawlStrategyMapper,
            Transformer_Crawl,
            Routing_Key_Internal_Crawl
        );

        var crawlResponseTransformerElastic = new CrawlResponseEsTransformer<CrawlResponse>(
            Transformer_Response_Es
        );
        var crawlResponseTransformerMongodb = new CrawlResponseMongoDbTransformer<CrawlResponse>(
            Transformer_Response_Mongodb
        );

        var stockDataTransformer = new StockDataMongoDbTransformer<CrawlResponse>();
        transformers.Add(uriTransformer);
        transformers.Add(crawlRequestTransformer);
        transformers.Add(crawlResponseTransformerElastic);
        transformers.Add(crawlResponseTransformerMongodb);
        transformers.Add(stockDataTransformer);

        return transformers;
    }

    private List<IPublisher<object>> CreatePublishers(IConfiguration configuration)
    {
        var publishers = new List<IPublisher<object>>();
        var crawlerResponseConfiguration = new DatabaseConfiguration(
            "crawler_responses",
            configuration
        );
        // CRAWL RESPONSE - MONGO DB
        var crawlerResponseRepository = mongodbFactory.CreateRepository<CrawlResponseModel>(
            crawlerResponseConfiguration
        );
        var crawlResultPublisherMongoDb = new BertrandPublisher<CrawlResponseModel>(
            Publisher_Crawl_Mongodb,
            new MongoDbPublisher<CrawlResponseModel>(
                Publisher_Crawl_Mongodb,
                crawlerResponseRepository
            )
        );

        // CRAWL RESPONSE - ELASTIC SEARCH
        var elasticSearchRepository = elasticFactory.CreateRepository();
        var crawlResultPublisherElastic = new BertrandPublisher<CrawlEsResponseModel>(
            Publisher_Crawl_Es,
            new ElasticsearchPublisher<CrawlEsResponseModel>(
                Publisher_Crawl_Es,
                "crawler_results",
                elasticSearchRepository
            )
        );

        // URI FOUND - MONGO DB
        var uriDataRepository = new MongoDbRepository<UriDataModel>(
            configuration,
            new UriDataConfiguration(configuration),
            jsonConverterProvider
        );
        var uriPublisherMongoDb = new BertrandPublisher<UriDataModel>(
            Publisher_Uri,
            new MongoDbPublisher<UriDataModel>(Publisher_Uri, uriDataRepository)
        );

        // Stock data - MONGO DB
        var stockDataRepository = new MongoDbRepository<StockDataResponseModel>(
            configuration,
            new StockDataConfiguration(configuration),
            jsonConverterProvider
        );

        var stockPublisherMongoDb = new BertrandPublisher<StockDataResponseModel>(
            Publisher_Stock_Mongodb,
            new MongoDbPublisher<StockDataResponseModel>(
                Publisher_Stock_Mongodb,
                stockDataRepository
            )
        );

        publishers.Add(crawlResultPublisherMongoDb);
        publishers.Add(crawlResultPublisherElastic);
        publishers.Add(uriPublisherMongoDb);
        publishers.Add(stockPublisherMongoDb);
        //publishers.Add(new ConsolePublisher<object, object>(jsonConverterProvider));

        return publishers;
    }

    private BertrandExchangeManager CreateExchangeManager(
        out MongoDbBertrandStateStore bertrandStateStore,
        out MongoDbBertrandExchangeStore bertrandExchangeStore,
        out IMongoDbRepository<BertrandStateDataModel> bertrandStateRepository
    )
    {
        var stateStoreConfiguration = new DatabaseConfiguration(
            "bertrand_exchange_crawler_state",
            configuration
        );
        var deadletterStoreConfiguration = new DatabaseConfiguration(
            "bertrand_exchange_crawler_deadletter",
            configuration
        );
        var exchangeStoreConfiguration = new DatabaseConfiguration(
            "bertrand_exchange_crawler_exchange_store",
            configuration
        );

        bertrandStateRepository = mongodbFactory.CreateRepository<BertrandStateDataModel>(
            stateStoreConfiguration
        );
        var bertrandStateDeadletterRepository =
            mongodbFactory.CreateRepository<BertrandStateDataModel>(deadletterStoreConfiguration);
        bertrandStateStore = new MongoDbBertrandStateStore(
            jsonConverterProvider,
            bertrandStateRepository,
            bertrandStateDeadletterRepository
        );

        var bertrandExchangeRepository = mongodbFactory.CreateRepository<BertrandExchangeDataModel>(
            exchangeStoreConfiguration
        );
        bertrandExchangeStore = new MongoDbBertrandExchangeStore(bertrandExchangeRepository);
        return new BertrandExchangeManager(
            bertrandExchangeStore,
            loggerFactory.CreateLogger<BertrandExchangeManager>()
        );
    }

    public class StockDataConfiguration(IConfiguration configuration) : IDatabaseConfiguration
    {
        public string DatabaseName =>
            configuration.GetSection(key: SchedulerRepository.MongoDbDatabaseNameKey).Value;

        public string CollectionName => "stockdata";
    }
}
