// See https://aka.ms/new-console-template for more information
using Crawler.Configuration.Core;
using Crawler.Configuration.Repository;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.Management.Core.RequestHandling.Core.Amqp;
using Crawler.Microservice.Core;
using Crawler.RequestHandling.Core;
using Crawler.Scheduler.Repository;
using Microservice.Amqp.Rabbitmq;
using Microservice.Mongodb.Repo;
using Microservice.TestHelper;
using Microsoft.Extensions.Configuration;
using Moq;

Console.WriteLine("Starting Ad-hoc crawl");
const string URI_COLLECTOR_FILE = "uricollector";


var databaseConfiguration = new DatabaseConfiguration
{
    DatabaseName = "Crawler",
    DocumentName = "crawl_request"
};

var configuration = TestHelper.GetConfiguration();
var jsonConverter = new JsonConverterProvider();



var configRepo = new MongoDbConfigurationRepository(
    new MongoDbRepository<CrawlRequestModel>(
        configuration,
        databaseConfiguration,
        jsonConverter
        ));

var schedulerRepo = new SchedulerRepository(configuration, jsonConverter);

ICrawlerConfigurationService _crawlerConfiguration = new CrawlerConfigurationService(configRepo, schedulerRepo);

var amqpProvider = new AmqpProvider(configuration, jsonConverter, new RabbitMqConnectionFactory());

IRequestPublisher _requestPublisher = new AmqpRequestPublisher(amqpProvider);

Console.WriteLine("Processing Collectors in File");

if (File.Exists(URI_COLLECTOR_FILE))
{
    foreach (var line in File.ReadLines(URI_COLLECTOR_FILE))
    {
        Console.WriteLine($"Processing: {line}");
	await PublishCollectorRequest(_crawlerConfiguration, _requestPublisher, line);
    }
}

return;

var collectors = await schedulerRepo.GetNewCollectorUris(100).Match(r => r, () => new List<UriDataModel>(), ex => throw ex);
if (collectors.Any())
{
    Console.WriteLine($"Found collector URIs: {collectors.Count()}");
    foreach (var uri in collectors)
    {
        var color  = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine(uri.Uri);
        Console.ForegroundColor = color;


        Console.WriteLine("Skip? (y/n)");
        if (IsYes())
            continue;

        Console.WriteLine();
        Console.WriteLine("Ignore? (y/n)");
        if (IsYes())
        {
            await Ignore(schedulerRepo, uri);
            continue;
        }

        Console.WriteLine();
        Console.WriteLine("Onetime? (y/n)?");
        if (IsYes())
        {
            await Onetime(schedulerRepo, _crawlerConfiguration, _requestPublisher, uri);

        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("Enter Cron: (e.g. '0 25 20/1 * * ?'");
            var cron = Console.ReadLine();
            uri.CronPeriod = cron;
            await UpdateUri(schedulerRepo, uri);
        }

    }
}

bool IsYes()
{
    var result = Console.ReadKey().KeyChar;
    return result == 'y' || result == 'Y';
}

static async Task UpdateUri(SchedulerRepository schedulerRepo, UriDataModel? uri)
{
    await schedulerRepo.AddOrUpdate(uri).Match(_ => { }, () => Console.WriteLine("Failed to update cron"));
}

static async Task PublishCollectorRequest(ICrawlerConfigurationService _crawlerConfiguration, IRequestPublisher _requestPublisher, string uri)
{
    var corrId = Guid.NewGuid();
    var crawlModel = await _crawlerConfiguration
                        .GetCollectorCrawlRequest(uri)
                        .Match(
                            r => r,
                            () => new CrawlRequestModel()
                            {
                                ContinuationStrategyDefinition = CrawlContinuationStrategy.TrackLinksOnly,
                                DocumentPartDefinition = new DocumentPartAutodetect(uri)
                            });

    await _requestPublisher.PublishRequest(crawlModel.Map(uri, correlationId: corrId, crawlId: corrId))
    .Match(_ => { }, () => Console.WriteLine("Failed to publish: " + uri));
}

static async Task Ignore(SchedulerRepository schedulerRepo, UriDataModel? uri)
{
    uri.IsCompleted = true;
    uri.IsSkipped = true;
    await UpdateUri(schedulerRepo, uri);
}

static async Task Onetime(SchedulerRepository schedulerRepo, ICrawlerConfigurationService _crawlerConfiguration, IRequestPublisher _requestPublisher, UriDataModel? uri)
{
    await PublishCollectorRequest(_crawlerConfiguration, _requestPublisher, uri.Uri);
    uri.IsCompleted = true;
    await UpdateUri(schedulerRepo, uri);
}
