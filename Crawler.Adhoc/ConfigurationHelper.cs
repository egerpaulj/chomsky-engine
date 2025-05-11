using Crawler.Configuration.Core;
using Crawler.Configuration.Repository;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.UserActions;
using Crawler.DataModel;
using Crawler.RequestHandling.Core;

public class ConfigurationHelper
{
    public static async Task SaveDefaultInDatabaseAndCrawl(
        string baseUri,
        string uri,
        string waitXpath,
        CrawlContinuationStrategy continuationStrategy,
        MongoDbConfigurationRepository configurationService,
        IRequestPublisher requestPublisher,
        params string[] skiplist
    )
    {
        var uriQualified = new Uri(uri);
        var crawlModel = new CrawlRequestModel
        {
            ContinuationStrategyDefinition = continuationStrategy,
            DocumentPartDefinition = new DocumentPartAutodetect(baseUri),
            Uri = uri,
            Host = uriQualified.Host,
            ShouldProvideRawSource = true,
            UiActions = new List<UiAction>
            {
                new UiAction { Type = UiAction.ActionType.Wait, ActionData = waitXpath },
                new UiAction { Type = UiAction.ActionType.Scroll, ActionData = "10" },
            },
            UrlSkipList = skiplist.ToList(),
        };
        var guid = Guid.NewGuid();
        await configurationService
            .AddOrUpdate(crawlModel)
            .Match(r => { }, () => new Exception("Failed to add new Configuraiton"), e => throw e);
        await requestPublisher
            .PublishRequest(crawlModel.Map(uri, correlationId: guid, crawlId: guid, isAdhoc: true))
            .Match(_ => { }, () => Console.WriteLine("Failed to publish: " + uri));
    }
}
