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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using LanguageExt;
using LanguageExt.ClassInstances;
using Microservice.Amqp;
using Microservice.Amqp.Rabbitmq;
using Microservice.Elasticsearch.Repo;
using Microservice.Exchange.Bertrand;
using Microservice.Exchange.Core.Polling;
using Microservice.Exchange.Endpoints;
using Microservice.Exchange.Endpoints.Csv;
using Microservice.Exchange.Endpoints.Elasticsearch;
using Microservice.Exchange.Endpoints.Mongodb;
using Microservice.Exchange.Endpoints.Rabbitmq;
using Microservice.Exchange.Test;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace Microservice.Exchange.Core.Bertrand.Tests;

[TestClass]
public class BertrandExchangeIntegrationTest
{
    private const string EsIndexName = "testexchange";
    private readonly IMongoDbRepository<TestOutputMessage> _mongodbRepository;
    private readonly IMongoDbRepository<BertrandStateDataModel> _bertrandStateDeadletterRepository;
    private readonly IMongoDbRepository<BertrandStateDataModel> _bertrandStateRepository;
    private readonly IMongoDbRepository<BertrandExchangeDataModel> _bertrandExchangeRepository;
    private readonly IElasticsearchRepository _elasticSearchRepository;
    private readonly IAmqpBootstrapper _amqpbootstrapper;
    private readonly IAmqpProvider _amqpProvider;
    private const string RabbitMqOutputContext = "RabbitMqTestOutputContext";
    private const string RabbitMqInputContext = "RabbitMqTestInputContext";

    private readonly ServiceProvider serviceProvider;

    List<IBertrandConsumer> Consumers;
    ElasticsearchBertrandConsumer<TestOutputMessage> elasticsearchBertrandConsumer;
    readonly RabbitMqBertrandConsumer<TestOutputMessage> rabbitMqConsumer;

    readonly List<IBertrandTransformer> Transformers;
    readonly List<IBetrandTransformerFilter> TransformerFilters;

    readonly List<IPublisher<object>> Publishers;
    readonly List<IBertrandPublisherFilter> PublisherFilters;
    readonly MongoDbBertrandConsumer<TestOutputMessage> mongoDbConsumer;
    readonly IBertrandStateStore bertrandStateStore;
    readonly IBertrandExchangeStore bertrandExchangeStore;
    readonly IJsonConverterProvider jsonConverterProvider;
    readonly IBertrandExchangeManager exchangeManager;

    public BertrandExchangeIntegrationTest()
    {
        var configuration = TestHelper.TestHelper.GetConfiguration($"{TestHelper.TestHelper.GetEnvironment()}.Bertrand") as IConfiguration;

        serviceProvider = new ServiceCollection()
        .AddLogging(builder =>
        {
            builder.AddConsole();
        })
        .AddTransient<IAmqpProvider, AmqpProvider>()
        .AddTransient<IAmqpBootstrapper, AmqpBootstrapper>()
        .AddTransient<IJsonConverterProvider, EmptyJsonConverterProvider>()
        .AddTransient<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>()
        .AddTransient(_ => configuration)
        .BuildServiceProvider();


        jsonConverterProvider = new EmptyJsonConverterProvider();

        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

        // -- RabbitMq
        _amqpProvider = serviceProvider.GetService<IAmqpProvider>();
        _amqpbootstrapper = serviceProvider.GetService<IAmqpBootstrapper>();

        var messageublisher = _amqpProvider.GetPublisher(RabbitMqOutputContext).Match(r => r, () => throw new System.Exception("Failed")).Result;

        var bertrandPublisherRabbitMq = new RabbitMqPublisher<object>(
            "Rabbit",
            messageublisher);

        rabbitMqConsumer = new RabbitMqBertrandConsumer<TestOutputMessage>(_amqpProvider, RabbitMqInputContext, "rabbit-integration-test");

        // -- RabbitMq End

        // -- MongoDB
        var databaseConfiguration = new DatabaseConfiguration
        {
            CollectionName = "bertrand_exchange_test",
            DatabaseName = "test"
        };
        var pollingFactory = new PollingConsumerFactory(loggerFactory: loggerFactory, "bertrand_input");
        var repositoryFactory = new Microservice.Mongodb.Repo.RepositoryFactory(configuration, jsonConverterProvider, loggerFactory);
        _mongodbRepository = repositoryFactory.CreateRepository<TestOutputMessage>(databaseConfiguration);


        mongoDbConsumer = new MongoDbBertrandConsumer<TestOutputMessage>(
            "mongo-integration-test",
            databaseConfiguration,
            loggerFactory,
            FilterDefinition<BsonDocument>.Empty,
            pollingFactory,
            repositoryFactory,
            200,
            0,
            0
        );
        // -- MongoDB End


        // -- ES
        var elasticRepoFactory = new Microservice.Elasticsearch.Repo.RepositoryFactory(loggerFactory, configuration, jsonConverterProvider);
        _elasticSearchRepository = elasticRepoFactory.CreateRepository();
        elasticsearchBertrandConsumer = new ElasticsearchBertrandConsumer<TestOutputMessage>("es-integration-test", loggerFactory, "{\"query\": {\"match\": {\"EnrichedData\": {\"query\": \"some test data\"}}}}", EsIndexName, elasticRepoFactory, pollingFactory, 200);
        // -- ES end

        // -- Transformers
        var routingKeyTransformer = new TestTransformer("bertrand_output", false, "Test transformer");
        var routingKeyTransformerEs = new TestTransformer("bertrand_output_es", true, "ES Test Transformer");

        Transformers = [routingKeyTransformer, routingKeyTransformerEs];

        TransformerFilters = [
            new BertrandRoutingKeyFilter("bertrand_input", routingKeyTransformer.Name),
            new BertrandRoutingKeyFilter("bertrand_input", routingKeyTransformerEs.Name),
            new BertrandRoutingKeyFilter("bertrand_output", routingKeyTransformer.Name),
            new BertrandRoutingKeyFilter("bertrand_output", routingKeyTransformerEs.Name),
            ];
        // -- Transformers

        // -- Publishers
        var bertrandPublisherMongoDb = new BertrandPublisher<TestOutputMessage>(
            "MongoDb",
            new MongoDbPublisher<TestOutputMessage>("MongoDb", _mongodbRepository));

        var bertrandPublisherElasticsearch = new BertrandPublisher<TestEsOutputMessage>(
            "Elastic",
            new ElasticsearchPublisher<TestEsOutputMessage>("Elastic", EsIndexName, _elasticSearchRepository));

        Publishers = [
            new ConsolePublisher<object, object>(jsonConverterProvider),
            bertrandPublisherMongoDb,
            bertrandPublisherRabbitMq,
            bertrandPublisherElasticsearch];

        PublisherFilters = [
            new BertrandRoutingKeyFilter("bertrand_output", "Console"),
            new BertrandRoutingKeyFilter("bertrand_output", "MongoDb"),
            new BertrandRoutingKeyFilter("bertrand_output", "Rabbit"),
            new BertrandTypeFilter(typeof(TestEsOutputMessage).FullName, "Elastic"),
            ];
        // -- Publishers

        // -- State store Mongodb
        var stateStoreConfiguration = new DatabaseConfiguration
        {
            CollectionName = "bertrand_exchange_test_state",
            DatabaseName = "test"
        };
        var deadletterStoreConfiguration = new DatabaseConfiguration
        {
            CollectionName = "bertrand_exchange_test_deadletter",
            DatabaseName = "test"
        };
        var exchangeStoreConfiguration = new DatabaseConfiguration
        {
            CollectionName = "bertrand_exchange_test_exchange_store",
            DatabaseName = "test"
        };
        _bertrandStateRepository = repositoryFactory.CreateRepository<BertrandStateDataModel>(stateStoreConfiguration);
        _bertrandStateDeadletterRepository = repositoryFactory.CreateRepository<BertrandStateDataModel>(deadletterStoreConfiguration);
        bertrandStateStore = new MongoDbBertrandStateStore(jsonConverterProvider, _bertrandStateRepository, _bertrandStateDeadletterRepository);


        _bertrandExchangeRepository = repositoryFactory.CreateRepository<BertrandExchangeDataModel>(exchangeStoreConfiguration);
        bertrandExchangeStore = new MongoDbBertrandExchangeStore(_bertrandExchangeRepository);
        exchangeManager = new BertrandExchangeManager(bertrandExchangeStore, loggerFactory.CreateLogger<BertrandExchangeManager>());
        // -- State store
    }

    private BertrandExchange CreateExchange()
    {
        var logger = serviceProvider.GetService<ILogger<BertrandExchange>>();
        return new BertrandExchange(
                    "Test exchange",
                    Consumers,
                    Transformers,
                    TransformerFilters,
                    PublisherFilters,
                    Publishers,
                   logger,
                   Mock.Of<IBertrandMetrics>(),
                   bertrandStateStore,
                   bertrandExchangeStore,
                   exchangeManager);
    }

    [TestMethod]
    public async Task FromMongodbTo()
    {
        // SETUP - infrastructure cleanup
        await _amqpbootstrapper.Purge().Match(r => r, () => throw new System.Exception("Failed"));
        await _amqpbootstrapper.Bootstrap().Match(r => r, () => throw new System.Exception("Failed"));

        await _mongodbRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _bertrandStateDeadletterRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _bertrandStateRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _bertrandExchangeRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _elasticSearchRepository.Delete(EsIndexName).Match(r => { }, () => { });

        // SETUP - something to consume in Mongodb
        await _mongodbRepository.AddOrUpdate(new TestOutputMessage { OriginalData = "I am some data" }).Match(r => r, () => throw new System.Exception("Failed to update DB"));

        // SETUP - exchange consumers
        Consumers = [mongoDbConsumer];

        // ACT - start exchange
        var exchange = CreateExchange();
        await exchange.Start().Match(m => m, () => throw new System.Exception("Failed to start exchange"));
        await Task.Delay(5000);
        await exchange.End().Match(r => r, () => throw new System.Exception("Failed to start"));
        await Task.Delay(5000);

        // ASSERT - result in ES
        var esResultList = await _elasticSearchRepository
                .Search<TestEsOutputMessage>(
                    EsIndexName,
                    "{\"query\": {\"match\": {\"EnrichedData\": {\"query\": \"Transformed data\"}}}}")
                .Match(r => r, () => throw new Exception("Failed"), ex => throw ex);

        Assert.IsTrue(esResultList.Count >= 1, $"Count is: {esResultList.Count} ");

        // ASSERT - result in Mongodb
        var mongodbResultList = await _mongodbRepository.GetMany(Builders<BsonDocument>.Filter.Regex("EnrichedData", BsonRegularExpression.Create("Transformed data: bertrand_output"))).Match(r => r, () => throw new Exception("Failed to get Mongodb"));
        Assert.IsTrue(mongodbResultList.Count >= 1, $"Count is: {mongodbResultList.Count} ");
    }


    [TestMethod]
    public async Task FromRabbitMqToMongoDb()
    {
        // SETUP - infrastructure cleanup
        await _amqpbootstrapper.Purge().Match(r => r, () => throw new System.Exception("Failed"));
        await _amqpbootstrapper.Bootstrap().Match(r => r, () => throw new System.Exception("Failed"));

        await _mongodbRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        
        await _bertrandStateDeadletterRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _bertrandExchangeRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _bertrandStateRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _elasticSearchRepository.Delete(EsIndexName).Match(r => { }, () => { });

        // SETUP - something to consume in Rabbitmq
        await _amqpProvider
            .GetPublisher(RabbitMqInputContext)
            .Bind(publisher => publisher.Publish<TestOutputMessage>(new TestOutputMessage { OriginalData = "I am some data" }))
            .Match(r => r, () => throw new Exception("Failed"));

        // SETUP - exchange consumers
        Consumers = [rabbitMqConsumer];

        // ACT - start exchange
        var exchange = CreateExchange();

        // ACT - start exchange
        await exchange.Start().Match(m => m, () => throw new System.Exception("Failed to start exchange"));
        await Task.Delay(5000);
        await exchange.End().Match(r => r, () => throw new System.Exception("Failed to start"));
        await Task.Delay(5000);

        // ASSERT - result in ES
        var esResultList = await _elasticSearchRepository
                .Search<TestEsOutputMessage>(
                    EsIndexName,
                    "{\"query\": {\"match\": {\"EnrichedData\": {\"query\": \"Transformed data\"}}}}")
                .Match(r => r, () => throw new Exception("Failed"), ex => throw ex);

        Assert.IsTrue(esResultList.Count >= 1, $"Count is: {esResultList.Count} ");

        // ASSERT - result in Mongodb
        var mongodbResultList = await _mongodbRepository.GetMany(Builders<BsonDocument>.Filter.Regex("EnrichedData", BsonRegularExpression.Create("Transformed data: bertrand_output"))).Match(r => r, () => throw new Exception("Failed to get Mongodb"));
        Assert.IsTrue(mongodbResultList.Count >= 1, $"Count is: {mongodbResultList.Count} ");
    }

    [TestMethod]
    public async Task FromRabbitMq_ToBadTransformer_ToDeadletter()
    {
        // SETUP - infrastructure cleanup
        await _amqpbootstrapper.Purge().Match(r => r, () => throw new System.Exception("Failed"));
        await _amqpbootstrapper.Bootstrap().Match(r => r, () => throw new System.Exception("Failed"));

        await _mongodbRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _bertrandStateDeadletterRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _bertrandExchangeRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _bertrandStateRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _elasticSearchRepository.Delete(EsIndexName).Match(r => { }, () => { });

        // SETUP - something to consume in Rabbitmq
        await _amqpProvider
            .GetPublisher(RabbitMqInputContext)
            .Bind(publisher => publisher.Publish<TestOutputMessage>(new TestOutputMessage { OriginalData = "I am some data" }))
            .Match(r => r, () => throw new Exception("Failed"));

        // SETUP - ensure error transformer matches the incoming message - setup to fail
        var failingTransformer = new ErrorTransformer();
        TransformerFilters.Clear();

        TransformerFilters.Add(
        new BertrandRoutingKeyFilter("bertrand_output", failingTransformer.Name));

        TransformerFilters.Add(
        new BertrandRoutingKeyFilter("bertrand_input", failingTransformer.Name));

        Transformers.Clear();
        Transformers.Add(failingTransformer);

        // SETUP - exchange consumers
        Consumers = [rabbitMqConsumer];

        // ACT - start exchange
        var exchange = CreateExchange();

        // ACT - start exchange
        await exchange.Start().Match(m => m, () => throw new System.Exception("Failed to start exchange"));
        await Task.Delay(5000);
        await exchange.End().Match(r => r, () => throw new System.Exception("Failed to start"));
        await Task.Delay(5000);

        // ASSERT - result ended up in the deadletter
        var deadletter = await _bertrandStateDeadletterRepository.GetMany(Builders<BsonDocument>.Filter.Empty).Match(r => r, () => throw new Exception("Failed to get deadletter"), ex => throw ex);
        Assert.AreEqual(1, deadletter.Count);

        // ASSERT - cleared the state (i.e. moved to deadletter)
        var outstanding = await bertrandStateStore.GetOutstandingMessages().Match(r => r, () => throw new Exception("Nothing in the deadletter"), ex => throw ex);
        Assert.IsFalse(outstanding.Any());
    }

    [TestMethod]
    public async Task FromState_ToPublishers()
    {
        // SETUP - infrastructure cleanup - delete all input and state
        await _mongodbRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _bertrandStateDeadletterRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _bertrandStateRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));
        await _elasticSearchRepository.Delete(EsIndexName).Match(r => { }, () => { });
        await _bertrandExchangeRepository.Delete(FilterDefinition<BsonDocument>.Empty).Match(r => r, () => throw new System.Exception("Faile to purge collection"));

        // SETUP - Add something in state
        await _bertrandStateRepository
            .AddOrUpdate(new BertrandStateDataModel
            {
                CorrelationId = Guid.NewGuid(),
                Id = Guid.NewGuid(),
                RoutingKey = "bertrand_input",
                Payload = new EmptyJsonConverterProvider().Serialize(new TestOutputMessage { OriginalData = "I am some data" }),
                AssemblyQualifiedTypeName = typeof(TestOutputMessage).AssemblyQualifiedName
            })
            .Match(r => r, () => throw new Exception("Failed to add state"), ex => throw ex);

        // ACT - create exchange
        var exchange = CreateExchange();

        // ACT - start exchange
        await exchange.Start().Match(m => m, () => throw new System.Exception("Failed to start exchange"));
        await Task.Delay(5000);
        await exchange.End().Match(r => r, () => throw new System.Exception("Failed to start"));
        await Task.Delay(5000);

        // ASSERT - result in ES
        var esResultList = await _elasticSearchRepository
                .Search<TestEsOutputMessage>(
                    EsIndexName,
                    "{\"query\": {\"match\": {\"EnrichedData\": {\"query\": \"Transformed data\"}}}}")
                .Match(r => r, () => throw new Exception("Failed"), ex => throw ex);

        Assert.IsTrue(esResultList.Count >= 1, $"Count is: {esResultList.Count} ");

        // ASSERT - result in Mongodb
        var mongodbResultList = await _mongodbRepository.GetMany(Builders<BsonDocument>.Filter.Regex("EnrichedData", BsonRegularExpression.Create("Transformed data: bertrand_output"))).Match(r => r, () => throw new Exception("Failed to get Mongodb"));
        Assert.IsTrue(mongodbResultList.Count >= 1, $"Count is: {mongodbResultList.Count} ");
    }
}

public class ErrorTransformer() : IBertrandTransformer
{
    public string Name => "Failing transformer";

    public TryOptionAsync<Message<object>> Transform(Option<Message<object>> input)
    {
        return async () =>
        {
            await Task.CompletedTask;
            throw new Exception("SHould fail");
        };
    }
}

public class TestTransformer(string routingKey, bool isEs, string name) : IBertrandTransformer
{
    private readonly string routingKey = routingKey;
    private readonly bool isEs = isEs;

    public string Name { get; } = $"{name}_{isEs}";

    public TryOptionAsync<Message<object>> Transform(Option<Message<object>> input)
    {
        return input.ToTryOptionAsync().Bind(Transform);
    }

    private TryOptionAsync<Message<object>> Transform(Message<object> input)
    {
        return async () =>
        {

            var output = new Message<object>();
            output = input.CopyData(output);
            output.RoutingKey = routingKey;

            if (isEs)
            {
                var inputPayload = (TestOutputMessage)input.Payload.Match(p => p, () => throw new System.Exception("Payload is empty"));

                output.Payload = new TestEsOutputMessage
                {
                    Created = inputPayload.Created,
                    OriginalData = inputPayload.OriginalData,
                    EnrichedData = "Transformed data: " + routingKey + DateTime.Now.ToString(" yyyy.MM.dd: mm.ss.fff"),
                    Id = Guid.NewGuid()

                };

                return await Task.FromResult(output);
            }

            var payload = (TestOutputMessage)input.Payload.Match(p => p, () => throw new System.Exception("Payload is empty"));
            payload.EnrichedData = "Transformed data: " + routingKey + DateTime.Now.ToString(" yyyy.MM.dd: mm.ss.fff");
            output.Payload = payload;
            payload.Id = Guid.NewGuid();

            return await Task.FromResult(output);
        };
    }
}
