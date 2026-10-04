// See https://aka.ms/new-console-template for more information
using System.Security.Permissions;
using System.Text.RegularExpressions;
using Crawler.Configuration.Core;
using Crawler.Configuration.Repository;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Results;
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

// await amqpBootstrapper
//     .CreateQueue("legacy", "crawler", "legacy")
//     .Match(r => r, () => throw new Exception("Failed"), ex => throw ex);



// await StartCrawling(
//     uri: "https://main.un.org/securitycouncil/en/content/resolutions-0",
//     baseUri: "https://main.un.org/securitycouncil/en",
//     skipList: [".jpg", "jpeg", ".png", "/ar/", "/zh/", "/fr/", "/ru/", "/es/"],
//     shouldProvideRawSource: true,
//     shouldDownload: true
// );

//await StartCrawling(
//    uri: "https://press.un.org/en",
//    baseUri: "https://press.un.org/en",
//    skipList: [".jpg", "jpeg", ".png", "/ar/", "/zh/", "/fr/", "/ru/", "/es/"],
//    shouldProvideRawSource: false,
//    shouldDownload: false
//);

// await StartCrawling(
//     uri: "https://chomsky.info",
//     baseUri: "https://chomsky.info",
//     skipList: [],
//     shouldProvideRawSource: false
// );
// await StartCrawling(
//     uri: "https://www.theguardian.com/world/2025/oct/02/vladimir-putin-dismisses-fears-moscow-plans-attack-nato",
//     baseUri: "https://www.theguardian.com",
//     skipList: [],
//     shouldProvideRawSource: false,
//     shouldDownload: true
// );

var keepList = new List<string>
{
    "DatapipelineCleanData",
    "DatapipelineCleanData_dlq",
    "request_queue",
    "request_queue_dlq",
    "uri_queue",
    "uri_queue_dlq",
    "www.cdc.gov",
    "www.cdc.gov_dlq",
    "www.theguardian.com",
    "www.theguardian.com_dlq",
    "www.amnesty.org",
    "www.amnesty.org_dlq",
    "chomsky.info",
    "chomsky.info_dlq",
    "legacy",
    "legacy_dlq",
};

// await PublishNormalUriFromFile(_requestPublisher, _crawlerConfiguration);

// keepList = keepList.Append(CrawlResponseData.WhiteList).ToList();

// var queueNames = File.ReadAllLines("/home/user/src/chomsky-engine-pri/queues.txt")
//     .Where(q => !keepList.Contains(q));

// await DeleteQueues(amqpBootstrapper, queueNames);

// await FilterOutGuardian(
//     amqpProvider,
//     _requestPublisher,
//     responseRepository,
//     queueName: "legacy",
//     amqpContextName: "Legacy",
//     max: 10795
// );

// await FilterOutGuardian(
//     amqpProvider,
//     _requestPublisher,
//     responseRepository,
//     queueName: "www.theguardian.com",
//     amqpContextName: "CrawlRequest",
//     max: 20967
// );

// await FilterOutExistingResults(
//     keepList: [],
//     dropMatchList: [],
//     queueName: "www.amnesty.org",
//     amqpContextName: "CrawlRequest",
//     1202
// );
// await FilterOutExistingResults(skipList: [], dropMatchList: [], "www.cdc.gov");

// await FilterOut(
//     [
//         "page=with%3ablock",
//         "/film/",
//         "/books/",D
//         "/tv-and-radio/",
//         "#img-",
//         "/football/",
//         "/music",
//         "#card-",
//     ],
//     "www.theguardian.com",
//     []
// );

// string pattern = @"^https:\/\/www\.cdc\.gov\/[a-zA-Z]{2}\d+[a-zA-Z]\d\.htm$";
// string pattern2 = @"^https:\/\/www\.cdc\.gov\/\d+\.htm$";
// await FilterOut(
//     [
//         "/npgd0",
//         "/spanish/",
//         "/n1",
//         "/es/",
//         "list1-",
//         "list1sec2",
//         ".pdf",
//         ".xlsx",
//         ".xls",
//         "cdc.gov/mmwrhtml/",
//         "www.cdc.gov/#collapse_",
//     ],
//     "www.cdc.gov",
//     new Regex(pattern),
//     new Regex(pattern2)
// );



// await AddGuardianLinksCollection();
// await AddStockCollectionUris();

// await PublishTrackLinksOnlyAutoDetect(new Uri("https://chomsky.info"), "https://chomsky.info");

// await AddConfiguration(
//     host: "main.un.org",
//     uri: "*",
//     baseUri: "https://main.un.org/securitycouncil/en",
//     contentXpath: "//div[contains(@class, 'field--type-text-with-summary')]",
//     titleXpath: "//h1[contains(@class, 'node-block-title')]",
//     shouldIndex: false,
//     shouldProvideRawSource: false,
//     publishRequestToQueue: true,
//     storeConfigInDb: true,
//     shouldDownload: true,
//     publishUri: "https://main.un.org/securitycouncil/en",
//     skipList: [".jpg", "jpeg", ".png", "/ar/", "/zh/", "/fr/", "/ru/", "/es/"]
// );

// await AddArticleConfiguration(
//     host: "press.un.org",
//     uri: "*",
//     baseUri: "https://press.un.org",
//     timestampXpath: "//time[contains(@class, 'datetime')]",
//     contentXpath: "//div[contains(@class, 'field--type-text-with-summary')]",
//     titleXpath: "//h1[contains(@class, 'page-header')]",
//     shouldIndex: true,
//     shouldProvideRawSource: false,
//     publishRequestToQueue: true,
//     storeConfigInDb: true,
//     publishUri: "https://press.un.org",
//     skipList: [".jpg", "jpeg", ".png", "/ar/", "/zh/", "/fr/", "/ru/", "/es/"]
// );


// await AddUnDocs();

// https://press.un.org


// Command-line dispatcher
var helpText =
    @"Usage: dotnet run -- <command> [args]

Commands and examples:

    help
        Show this help text.

    publish-collector-request <uri>
        Example: dotnet run -- publish-single https://chomsky.info
        Calls: PublishCollectorRequest

    publish-file [filePath]
        Example: dotnet run -- publish-file uricollector
        Calls: PublishCollectorsFromFile (defaults to 'uricollector')

    save-and-crawl-track-links-only <host> <uri> <baseUri>
        Example: dotnet run -- save-and-crawl example.com ""* https://example.com""
        Calls: ConfigurationHelper.SaveDefaultInDatabaseAndCrawl (alias for -n)

    process-collectors
        Example: dotnet run -- process-collectors
        Calls: ProcessCollectors (interactive scheduler processing)

    publish-normal-file
        Example: dotnet run -- publish-normal-file
        Calls: PublishNormalUriFromFile

    add-stock-collection
        Example: dotnet run -- add-stock-collection
        Calls: AddStockCollectionUris

    add-undocs
        Example: dotnet run -- add-undocs
        Calls: AddUnDocs

    start-crawl <uri> <baseUri> <provideRaw(true|false)> <download(true|false)> [--skip ""value1,value2,...""]
        Example: dotnet run -- start-crawl https://chomsky.info https://chomsky.info true false --skip "".jpg,.png,/ar/""
        Calls: StartCrawling

    add-config <host> <uri> <baseUri> <contentXpath> <titleXpath> [--store] [--publish] [--download] [--skip ""value1,value2,...""]
        Example: dotnet run -- add-config main.un.org ""*"" https://main.un.org ""//div[...]"" ""//h1[...]"" --store --publish --download --skip "".jpg,.png,/ar/""
        Calls: AddConfiguration

    add-article-config <host> <uri> <baseUri> <timestampXpath> <contentXpath> <titleXpath> <fileXpath> [--store] [--publish] [--skip ""value1,value2,...""]
        Example: dotnet run -- add-article-config press.un.org ""*"" https://press.un.org ""//time[...]"" ""//div[...]"" ""//h1[...]"" --store --publish --skip "".jpg,.png,/ar/""
        Calls: AddArticleConfiguration

    filter-out-guardian <queueName> <amqpContextName> <max>
        Example: dotnet run -- filter-out-guardian legacy Legacy 10000
        Calls: FilterOutGuardian

    delete-queues <filePath>
        Example: dotnet run -- delete-queues queues.txt
        Calls: DeleteQueues using names in file

Notes:
    - Quotes are required for arguments that contain spaces.
    - Many commands publish to AMQP and require your local configuration to be valid.
";

if (args == null || args.Length == 0)
{
    Console.WriteLine(helpText);
    return;
}

var cmd = args[0].ToLowerInvariant();
try
{
    switch (cmd)
    {
        case "help":
            Console.WriteLine(helpText);
            break;
        case "publish-collector-request":
            if (args.Length < 2)
            {
                Console.WriteLine("publish-single requires <uri>");
                break;
            }
            await PublishCollectorRequest(_crawlerConfiguration, _requestPublisher, args[1]);
            break;
        case "publish-file":
            var file = args.Length >= 2 ? args[1] : URI_COLLECTOR_FILE;
            await PublishCollectorsFromFile(file, _crawlerConfiguration, _requestPublisher);
            break;
        case "save-and-crawl-track-links-only":
        case "-n":
            if (args.Length < 4)
            {
                Console.WriteLine("save-and-crawl requires <host> <uri> <baseUri>");
                break;
            }
            await ConfigurationHelper.SaveDefaultInDatabaseAndCrawl(
                args[1],
                args[2],
                args[3],
                CrawlContinuationStrategy.TrackLinksOnly,
                configRepo,
                _requestPublisher
            );
            Console.WriteLine("Published request, Domain only, AutoDetect: " + args[2]);
            break;
        case "process-collectors":
            await ProcessCollectors(schedulerRepo, _crawlerConfiguration, _requestPublisher);
            break;
        case "publish-normal-file":
            await PublishNormalUriFromFile(_requestPublisher, _crawlerConfiguration);
            break;
        case "add-stock-collection":
            await AddStockCollectionUris();
            break;
        case "add-undocs":
            await AddUnDocs();
            break;
        case "start-crawl":
            if (args.Length < 5)
            {
                Console.WriteLine(
                    "start-crawl requires <uri> <baseUri> <provideRaw(true|false)> <download(true|false)>"
                );
                break;
            }
            var shouldProvide = bool.Parse(args[3]);
            var shouldDownload = bool.Parse(args[4]);
            var skipIdx = Array.IndexOf(args, "--skip");
            var skips =
                skipIdx >= 0 && skipIdx + 1 < args.Length
                    ? args[skipIdx + 1].Split(',').Select(s => s.Trim()).ToList()
                    : new List<string>();
            await StartCrawling(args[1], args[2], skips, shouldProvide, shouldDownload);
            break;
        case "add-config":
            if (args.Length < 6)
            {
                Console.WriteLine(
                    "add-config requires <host> <uri> <baseUri> <contentXpath> <titleXpath>"
                );
                break;
            }
            var store = args.Contains("--store");
            var publish = args.Contains("--publish");
            var downloadFlag = args.Contains("--download");
            var skipIdxConfig = Array.IndexOf(args, "--skip");
            var skipListConfig =
                skipIdxConfig >= 0 && skipIdxConfig + 1 < args.Length
                    ? args[skipIdxConfig + 1].Split(',').Select(s => s.Trim()).ToArray()
                    : new string[] { };
            await AddConfiguration(
                host: args[1],
                uri: args[2],
                baseUri: args[3],
                contentXpath: args[4],
                titleXpath: args[5],
                shouldIndex: false,
                shouldProvideRawSource: false,
                publishRequestToQueue: publish,
                storeConfigInDb: store,
                publishUri: args[3],
                shouldDownload: downloadFlag,
                skipList: skipListConfig
            );
            break;
        case "add-article-config":
            if (args.Length < 8)
            {
                Console.WriteLine(
                    "add-article-config requires <host> <uri> <baseUri> <timestampXpath> <contentXpath> <titleXpath> <fileXpath>"
                );
                break;
            }
            var skipIdxArticle = Array.IndexOf(args, "--skip");
            var skipListArticle =
                skipIdxArticle >= 0 && skipIdxArticle + 1 < args.Length
                    ? args[skipIdxArticle + 1].Split(',').Select(s => s.Trim()).ToArray()
                    : [];
            await AddArticleConfiguration(
                host: args[1],
                uri: args[2],
                baseUri: args[3],
                timestampXpath: args[4],
                contentXpath: args[5],
                titleXpath: args[6],
                fileXpath: args[7],
                shouldIndex: false,
                shouldProvideRawSource: false,
                publishRequestToQueue: args.Contains("--publish"),
                storeConfigInDb: args.Contains("--store"),
                publishUri: args[1],
                skipList: skipListArticle
            );
            break;
        case "filter-out-guardian":
            if (args.Length < 4)
            {
                Console.WriteLine(
                    "filter-out-guardian requires <queueName> <amqpContextName> <max>"
                );
                break;
            }
            await FilterOutGuardian(
                amqpProvider,
                _requestPublisher,
                responseRepository,
                args[1],
                args[2],
                int.Parse(args[3])
            );
            break;
        case "delete-queues":
            if (args.Length < 2)
            {
                Console.WriteLine("delete-queues requires <filePath>");
                break;
            }
            var queueNames = File.ReadAllLines(args[1]).Where(q => !string.IsNullOrEmpty(q));
            await DeleteQueues(amqpBootstrapper, queueNames);
            break;
        default:
            Console.WriteLine("Unknown command: " + cmd);
            Console.WriteLine(helpText);
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error while executing command: " + ex.Message);
    Console.WriteLine(ex.ToString());
}

bool IsYes()
{
    var result = Console.ReadKey().KeyChar;
    return result == 'y' || result == 'Y';
}

static async Task UpdateUri(SchedulerRepository schedulerRepo, UriDataModel? uri)
{
    await schedulerRepo
        .AddOrUpdate(uri)
        .Match(_ => { }, () => Console.WriteLine("Failed to update cron"));
}

async Task FilterOut(List<string> dropMatchList, string queueName, params Regex[] dropExpressions)
{
    int counter = 0;
    var subsriber = await amqpProvider
        .GetSubsriber(
            "CrawlRequest",
            queueName,
            MessageHandlerFactory.Create<CrawlRequest>(async message =>
            {
                var uri = message
                    .Payload.Bind(p => p.LoadPageRequest)
                    .Bind(p => p.Uri)
                    .Match(u => u, () => "unknown uri");

                var crawlRequest = message.Payload.Match(r => r, () => throw new Exception("WTF"));

                if (
                    dropExpressions != null
                    && dropExpressions.Any(ex => ex.IsMatch(uri.ToLowerInvariant()))
                )
                {
                    Console.WriteLine("*********************** dropping: " + uri);
                }
                else if (dropMatchList.Any(m => uri.ToLowerInvariant().Contains(m)))
                {
                    Console.WriteLine("*********************** dropping: " + uri);
                }
                else
                {
                    Console.WriteLine($"{counter}:{uri}");
                    await _requestPublisher
                        .PublishRequest(crawlRequest)
                        .Match(r => r, () => throw new Exception("Failed re-publish request"));
                }

                counter++;

                return crawlRequest;
            })
        )
        .Match(r => r, () => throw new Exception("Failed to Create Subscriber"), ex => throw ex);

    subsriber.Start();
}

async Task FilterOutExistingResults(
    List<string> keepList,
    List<string> dropMatchList,
    string queueName,
    string amqpContextName,
    int max
)
{
    int counter = 0;

    List<string> processed = [];
    var subsriber = await amqpProvider
        .GetSubsriber(
            "Legacy",
            queueName,
            MessageHandlerFactory.Create<CrawlRequest>(async message =>
            {
                if ((counter + 1) > max)
                    Environment.Exit(0);
                var uri = message
                    .Payload.Bind(p => p.LoadPageRequest)
                    .Bind(p => p.Uri)
                    .Match(u => u, () => "unknown uri");

                var crawlRequest = message.Payload.Match(r => r, () => throw new Exception("WTF"));
                if (dropMatchList.Any(m => uri.ToLowerInvariant().Contains(m)))
                {
                    Console.WriteLine("*********************** dropping (drop match): " + uri);
                }
                else if (
                    !keepList.Any(keep => uri.ToLowerInvariant().Contains(keep))
                    && await responseRepository
                        .HasResponse(uri)
                        .Match(r => r, () => false, ex => throw ex)
                )
                {
                    Console.WriteLine("*********************** dropping: " + uri);
                }
                else if (processed.Contains(uri))
                {
                    Console.WriteLine("*********************** dropping duplicate: " + uri);
                }
                else if (keepList.Any(keep => uri.ToLowerInvariant().Contains(keep)))
                {
                    Console.WriteLine($"Publish back to queue: {uri}");
                    processed.Add(uri);
                    await _requestPublisher
                        .PublishRequest(crawlRequest)
                        .Match(r => r, () => throw new Exception("Failed re-publish request"));
                }
                else
                    Console.WriteLine("*********************** dropping: " + uri);

                counter++;
                Console.WriteLine($"{counter}:{uri}");

                return crawlRequest;
            })
        )
        .Match(r => r, () => throw new Exception("Failed to Create Subscriber"), ex => throw ex);

    subsriber.Start();
}

async Task MoveUriToDlq(List<string> list, string queueName)
{
    var subsriber = await amqpProvider
        .GetSubsriber(
            "CrawlUri",
            queueName,
            MessageHandlerFactory.Create<CrawlUri>(async message =>
            {
                var uri = message.Payload.Bind(p => p.Uri).Match(u => u, () => "unknown uri");

                var crawlUri = message.Payload.Match(r => r, () => throw new Exception("WTF"));
                if (list.Any(m => uri.ToLowerInvariant().Contains(m)))
                {
                    Console.WriteLine("Moving: " + uri);
                    throw new Exception(uri);
                }
                else
                {
                    Console.WriteLine("************************** dropping: " + uri);
                }

                return crawlUri;
            })
        )
        .Match(r => r, () => throw new Exception("Failed to Create Subscriber"), ex => throw ex);

    subsriber.Start();
    await Task.Delay(100000000);
}

async Task AddStockCollectionUris()
{
    foreach (var page in Enumerable.Range(1, 1742))
    {
        var uri =
            $"https://www.londonstockexchange.com/live-markets/market-data-dashboard/price-explorer?page={page}";
        Console.WriteLine($"Processing: {uri}");

        var request = await configRepo
            .GetCrawlRequest(uri)
            .Match(r => r, () => throw new Exception("wtf"), ex => throw ex);
        var corrId = Guid.NewGuid();
        await _requestPublisher
            .PublishRequest(request.Map(uri, correlationId: corrId, crawlId: corrId, isAdhoc: true))
            .Match(_ => { }, () => throw new Exception("Failed at: " + page));
    }
}

async Task PublishCollectorRequest(
    ICrawlerConfigurationService _crawlerConfiguration,
    IRequestPublisher _requestPublisher,
    string uri
)
{
    var corrId = Guid.NewGuid();
    var crawlModel = new CrawlRequestModel()
    {
        Uri = uri,
        UrlSkipList = ["https://id.rlcdn.com", "oeeowe/docs"],
        ContinuationStrategyDefinition = CrawlContinuationStrategy.None,
        IsUrlCollector = false,
        DocumentPartDefinition = new DocumentPartArticle(uri)
        {
            SubParts = new List<DocumentPart>
            {
                new DocumentPartLink(uri)
                {
                    Selector = new DocumentPartSelector
                    {
                        Xpath = "//a[contains(@class, 'page-number')]",
                    },
                },
                new DocumentPartLink(uri)
                {
                    Selector = new DocumentPartSelector
                    {
                        Xpath = "//a[contains(@href, 'stock/')]",
                    },
                },
                new DocumentPartTable(uri),
            },
        },
        ShouldProvideRawSource = true,
        ShouldIndex = false,
        UiActions =
        [
            new UiAction { ActionData = "22", Type = UiAction.ActionType.Scroll },
            new UiAction { ActionData = "37", Type = UiAction.ActionType.Wait },
            new UiAction { ActionData = "22", Type = UiAction.ActionType.Scroll },
        ],
    };

    // await configRepo!.AddOrUpdate(crawlModel).Match(r => r, () => throw new Exception("Failed"));

    await _requestPublisher
        .PublishRequest(crawlModel.Map(uri, correlationId: corrId, crawlId: corrId, isAdhoc: true))
        .Match(_ => { }, () => Console.WriteLine("Failed to publish: " + uri));
}

static async Task Ignore(SchedulerRepository schedulerRepo, UriDataModel? uri)
{
    uri.IsCompleted = true;
    uri.IsSkipped = true;
    await UpdateUri(schedulerRepo, uri);
}

async Task Onetime(
    SchedulerRepository schedulerRepo,
    ICrawlerConfigurationService _crawlerConfiguration,
    IRequestPublisher _requestPublisher,
    UriDataModel? uri
)
{
    await PublishCollectorRequest(_crawlerConfiguration, _requestPublisher, uri.Uri);
    uri.IsCompleted = true;
    await UpdateUri(schedulerRepo, uri);
}

async Task PublishCollectorsFromFile(
    string URI_COLLECTOR_FILE,
    ICrawlerConfigurationService _crawlerConfiguration,
    IRequestPublisher _requestPublisher
)
{
    if (File.Exists(URI_COLLECTOR_FILE))
    {
        Console.WriteLine("Processing Collectors in File");

        foreach (var line in File.ReadLines(URI_COLLECTOR_FILE))
        {
            Console.WriteLine($"Processing: {line}");
            await PublishCollectorRequest(_crawlerConfiguration, _requestPublisher, line);
        }
    }
}

async Task AddArticleConfiguration(
    string host,
    string uri,
    string baseUri,
    string timestampXpath,
    string contentXpath,
    string titleXpath,
    string fileXpath,
    bool shouldIndex = false,
    bool shouldProvideRawSource = false,
    bool publishRequestToQueue = true,
    bool storeConfigInDb = false,
    string publishUri = "",
    params string[] skipList
)
{
    var crawlModel = new CrawlRequestModel()
    {
        Host = host,
        Uri = uri,
        UrlSkipList = [.. skipList],
        ContinuationStrategyDefinition = CrawlContinuationStrategy.TrackLinksOnly,
        IsUrlCollector = false,
        DocumentPartDefinition = new DocumentPartArticle(baseUri)
        {
            Timestamp = new DocumentPartText(baseUri)
            {
                Selector = new DocumentPartSelector { Xpath = timestampXpath },
            },
            Content = new DocumentPartText(baseUri)
            {
                Selector = new DocumentPartSelector { Xpath = contentXpath },
            },
            Title = new DocumentPartText(baseUri)
            {
                Selector = new DocumentPartSelector { Xpath = titleXpath },
            },
            SubParts = new List<DocumentPart>
            {
                new DocumentPartTable(baseUri),
                new DocumentPartLink(baseUri),
                fileXpath != "None"
                    ? new DocumentPartFile(baseUri)
                    {
                        Selector = new DocumentPartSelector { Xpath = fileXpath },
                    }
                    : new DocumentPartFile(baseUri),
            },
        },
        ShouldProvideRawSource = shouldProvideRawSource,
        ShouldIndex = shouldIndex,
        ShouldDynamicBootstrap = true,
        ShouldDownloadContent = !string.IsNullOrEmpty(fileXpath),
        UiActions =
        [
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Wait },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Wait },
            //new UiAction { XPath = contentXpath, Type = UiAction.ActionType.Wait },
        ],
    };

    if (storeConfigInDb)
        await configRepo!
            .AddOrUpdate(crawlModel)
            .Match(r => r, () => throw new Exception("Failed"));

    if (publishRequestToQueue)
        await _requestPublisher
            .PublishRequest(
                crawlModel.Map(
                    publishUri,
                    correlationId: Guid.NewGuid(),
                    crawlId: Guid.NewGuid(),
                    isAdhoc: true
                )
            )
            .Match(
                _ => { },
                () => Console.WriteLine("Failed to publish: " + baseUri),
                ex => throw ex
            );
}

async Task AddConfiguration(
    string host,
    string uri,
    string baseUri,
    string contentXpath,
    string titleXpath,
    bool shouldIndex = false,
    bool shouldProvideRawSource = true,
    bool publishRequestToQueue = true,
    bool storeConfigInDb = false,
    string publishUri = "",
    bool shouldDownload = false,
    params string[] skipList
)
{
    //"//h1[contains(@class, 'node-block-title')]"
    var crawlModel = new CrawlRequestModel()
    {
        Host = host,
        Uri = uri,
        UrlSkipList = skipList.ToList(),
        ContinuationStrategyDefinition = CrawlContinuationStrategy.TrackLinksOnly,
        IsUrlCollector = false,
        DocumentPartDefinition = new DocumentPartText(baseUri)
        {
            Selector = new DocumentPartSelector { Xpath = titleXpath },
            SubParts = new List<DocumentPart>
            {
                new DocumentPartLink(baseUri),
                new DocumentPartText(baseUri)
                {
                    Selector = new DocumentPartSelector { Xpath = contentXpath },
                },
            },
        },
        ShouldProvideRawSource = shouldProvideRawSource,
        ShouldIndex = shouldIndex,
        ShouldDownloadContent = shouldDownload,
        UiActions =
        [
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Wait },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Wait },
            new UiAction { XPath = titleXpath, Type = UiAction.ActionType.Wait },
        ],
    };

    if (storeConfigInDb)
        await configRepo!
            .AddOrUpdate(crawlModel)
            .Match(r => r, () => throw new Exception("Failed"));

    if (publishRequestToQueue)
        await _requestPublisher
            .PublishRequest(
                crawlModel.Map(
                    publishUri,
                    correlationId: Guid.NewGuid(),
                    crawlId: Guid.NewGuid(),
                    isAdhoc: true
                )
            )
            .Match(
                _ => { },
                () => Console.WriteLine("Failed to publish: " + baseUri),
                ex => throw ex
            );
}

async Task AddUnDocs()
{
    var host = "undocs.org";
    var uri = "*";
    var baseUri = "https://undocs.org";
    var titleXpath = "//h1[contains(@class, 'node-block-title')]";
    List<string> skipList = [".jpg", "jpeg", ".png", "/ar/", "/zh/", "/fr/", "/ru/", "/es/"];
    var crawlModel = new CrawlRequestModel()
    {
        Host = host,
        Uri = uri,
        UrlSkipList = skipList.ToList(),
        ContinuationStrategyDefinition = CrawlContinuationStrategy.All,
        IsUrlCollector = false,
        DocumentPartDefinition = new DocumentPartLink(baseUri)
        {
            Selector = new DocumentPartSelector { Xpath = titleXpath },
            SubParts = new List<DocumentPart>
            {
                new DocumentPartFile("https://docs.un.org")
                {
                    Selector = new DocumentPartSelector { Xpath = "//iframe" },
                },
                new DocumentPartLink("https://docs.un.org")
                {
                    Selector = new DocumentPartSelector { Xpath = "//a[contains(@class, 'btn')]" },
                },
            },
        },
        ShouldProvideRawSource = false,
        ShouldIndex = false,
        ShouldDownloadContent = true,
        UiActions =
        [
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Wait },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Wait },
            new UiAction { XPath = titleXpath, Type = UiAction.ActionType.Wait },
        ],
    };

    await configRepo!.AddOrUpdate(crawlModel).Match(r => r, () => throw new Exception("Failed"));
}

async Task StartCrawling(
    string uri,
    string baseUri,
    List<string> skipList,
    bool shouldProvideRawSource,
    bool shouldDownload = false
)
{
    List<UiAction> uiActions =
    [
        new UiAction { ActionData = "5", Type = UiAction.ActionType.Wait },
        new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
        new UiAction { ActionData = "5", Type = UiAction.ActionType.Wait },
    ];
    var corrId = Guid.NewGuid();
    var crawlRequest = new CrawlRequest
    {
        ContinuationStrategy = CrawlContinuationStrategy.TrackLinksOnly,
        CorrelationCrawlId = corrId,
        Id = Guid.NewGuid(),
        LoadPageRequest = new LoadPageRequest()
        {
            Uri = uri,
            UserActions = uiActions,
            CorrelationId = corrId,
        },
        RequestDocument = new Document
        {
            RequestDocumentPart = new DocumentPartLink(baseUri),

            DownloadContent = shouldDownload,
        },

        ProvideRaw = shouldProvideRawSource,
        CrawlId = Guid.NewGuid(),
        ShouldIndex = false,
    };

    await _requestPublisher
        .PublishRequest(crawlRequest)
        .Match(_ => { }, () => Console.WriteLine("Failed to publish: " + baseUri), ex => throw ex);
}

async Task PublishTrackLinksOnlyAutoDetect(Uri uri, string baseUri)
{
    var corrId = Guid.NewGuid();
    var crawlModel = new CrawlRequestModel()
    {
        ContinuationStrategyDefinition = CrawlContinuationStrategy.TrackLinksOnly,
        DocumentPartDefinition = new DocumentPartLink(baseUri),
        UiActions = new List<UiAction>
        {
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
            new UiAction { ActionData = "5", Type = UiAction.ActionType.Scroll },
        },
    };

    await _requestPublisher
        .PublishRequest(
            crawlModel.Map(uri.AbsoluteUri, correlationId: corrId, crawlId: corrId, isAdhoc: true)
        )
        .Match(_ => { }, () => Console.WriteLine("Failed to publish: " + uri));
}

async Task ProcessCollectors(
    SchedulerRepository schedulerRepo,
    ICrawlerConfigurationService _crawlerConfiguration,
    IRequestPublisher _requestPublisher
)
{
    var collectors = await schedulerRepo
        .GetNewCollectorUris(100)
        .Match(r => r, () => new List<UriDataModel>(), ex => throw ex);
    if (collectors.Count != 0)
    {
        Console.WriteLine($"Found collector URIs: {collectors.Count}");
        foreach (var uri in collectors)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine(uri.Uri);
            Console.ForegroundColor = color;

            if (
                uri.Uri.Contains("/film")
                || uri.Uri.Contains("/sport")
                || uri.Uri.Contains("/stage")
                || uri.Uri.Contains("/commentisfree")
                || uri.Uri.Contains("/lifeandstyle")
                || uri.Uri.Contains("/artanddesign")
                || uri.Uri.Contains("/music")
                || uri.Uri.Contains("/travel")
                || uri.Uri.Contains("/football")
                || uri.Uri.Contains("/books")
                || uri.Uri.Contains("/help")
            )
            {
                Console.WriteLine("Auto-ignored");
                await Ignore(schedulerRepo, uri);
                continue;
            }

            if (
                uri.Uri.Contains("/science")
                || uri.Uri.Contains("/culture")
                || uri.Uri.Contains("/food")
                || uri.Uri.Contains("/fashion")
                || uri.Uri.Contains("/cardiff")
                || uri.Uri.Contains("/australia-news")
                || uri.Uri.Contains("/society")
                || uri.Uri.Contains("/environment")
                || uri.Uri.Contains("/politics")
                || uri.Uri.Contains("/world")
                || uri.Uri.Contains("/money")
                || uri.Uri.Contains("/media")
                || uri.Uri.Contains("/us-news")
                || uri.Uri.Contains("/business")
                || uri.Uri.Contains("/uk")
                || uri.Uri.Contains("/technology")
                || uri.Uri.Contains("/info")
                || uri.Uri.Contains("/government-computing-network")
                || uri.Uri.Contains("/education")
                || uri.Uri.Contains("/global-development")
                || uri.Uri.Contains("/tv-and-radio")
            )
            {
                Console.WriteLine("Auto-onetime");
                await Onetime(schedulerRepo, _crawlerConfiguration, _requestPublisher, uri);
                continue;
            }

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
}

static async Task DeleteQueues(AmqpBootstrapper amqpBootstrapper, IEnumerable<string> queueNames)
{
    await amqpBootstrapper
        .DeleteQueues(queueNames)
        .Match(r => r, () => throw new Exception("Failed to delete queues"), ex => throw ex);
}

async Task FilterOutGuardian(
    AmqpProvider amqpProvider,
    IRequestPublisher _requestPublisher,
    ICrawlerResponseShardedRepository responseRepository,
    string queueName,
    string amqpContextName,
    int max
)
{
    await FilterOutExistingResults(
        keepList: ["theguardian.com"],
        dropMatchList:
        [
            "page=with%3ablock",
            "/music/",
            "/downloads/",
            "/info/",
            "www.theguardian.com.au",
            "support.theguardian.com",
            "/tone/",
            "/film+tone/",
            "/games+tone/",
            "/picture/",
            "#top",
            "#navigation",
            "/cartoon/",
            "/help/",
            "/contact-us/",
            "/games/",
            "/gallery/",
            "/subscribe/",
            "/weblog/",
            "/stage/",
            "archer.politics",
            "/football/",
            "www.theguardian.com/#item-",
            "/interactive/",
            "/travel/",
            "/profile/",
            "/lifeandstyle/",
            "/race.",
            "/food/",
            "/artanddesign",
            "/culture/",
            "/contactus/",
            "/share/",
            "/discussion/",
            "/user/id/",
            "/crosswords/",
            "/fashion/",
            "/australia-news/",
            "/sport",
            "/video",
            "/film/",
            "/books/",
            "/tv-and-radio/",
            "#img-",
            "#block-",
            "mailto:?",
            "/countdown/story/",
            "syndication.theguardian.com",
            "film-and-music",
            "/media/",
            "/consumer-champions/",
            "/sendarticle/",
            "www.uscis.gov",
        ],
        queueName: queueName,
        amqpContextName: amqpContextName,
        max: max
    );
}

async Task PublishNormalUriFromFile(
    IRequestPublisher requestPublisher,
    ICrawlerConfigurationService crawlerConfigurationService
)
{
    foreach (
        var uri in File.ReadAllLines("uri_publish_normal")
            .Where(t => !string.IsNullOrEmpty(t) && !string.IsNullOrWhiteSpace(t))
    )
    {
        var request = await crawlerConfigurationService
            .CreateRequest(uri, Guid.NewGuid(), Guid.NewGuid())
            .Match(r => r, () => throw new Exception("Failed to create request"), ex => throw ex);

        await requestPublisher
            .PublishRequest(request)
            .Match(_ => { }, () => Console.WriteLine("Failed to publish: " + uri), ex => throw ex);
    }
}
