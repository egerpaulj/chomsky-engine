// See https://aka.ms/new-console-template for more information
using System.Security.Permissions;
using System.Text.RegularExpressions;
using Crawler.Configuration.Core;
using Crawler.Configuration.Repository;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.UserActions;
using Crawler.Data.Repository;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.Management.Core.RequestHandling.Core.Amqp;
using Crawler.Microservice.Core;
using Crawler.RequestHandling.Core;
using Crawler.Scheduler.Repository;
using LanguageExt;
using Microservice.Amqp;
using Microservice.Amqp.Rabbitmq;
using Microservice.Mongodb.Repo;
using Microservice.TestHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

Console.WriteLine("Starting Ad-hoc crawl");
const string URI_COLLECTOR_FILE = "uricollector";

var configuration = TestHelper.GetConfiguration();
var jsonConverter = new JsonConverterProvider();

var databaseConfiguration = new DatabaseConfiguration("crawl_request", configuration);

var configRepo = new MongoDbConfigurationRepository(
    new MongoDbRepository<CrawlRequestModel>(configuration, databaseConfiguration, jsonConverter)
);

var schedulerRepo = new SchedulerRepository(configuration, jsonConverter);

ICrawlerConfigurationService _crawlerConfiguration = new CrawlerConfigurationService(
    configRepo,
    schedulerRepo
);

var amqpBootstrapper = new AmqpBootstrapper(configuration);
var amqpProvider = new AmqpProvider(configuration, jsonConverter, new RabbitMqConnectionFactory());

IRequestPublisher _requestPublisher = new AmqpRequestPublisher(
    amqpProvider,
    amqpBootstrapper,
    configRepo
);

var mongodbFactory = new RepositoryFactory(configuration, jsonConverter);
DatabaseConfiguration crawlResponseDbConfig = new DatabaseConfiguration(
    "crawler_responses",
    configuration
);

var responseRepository = new CrawlerResponseShardedRepository(
    mongodbFactory,
    configuration,
    Mock.Of<ILogger<CrawlerResponseShardedRepository>>()
);
