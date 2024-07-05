using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.Core.Strategy;
using Crawler.DataModel;
using Crawler.Stategies.Core;
using LanguageExt;
using Microservice.Amqp;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Crawler.Management.Service;

public class RabbitMqCrawlRequestHandler : IMessageHandler<CrawlRequest, CrawlResponse>
{
    private readonly ICrawlStrategyMapper _crawlStrategyMapper;
    private readonly ILogger<RabbitMqCrawlRequestHandler> _logger;
    private static Counter _counter = Prometheus.Metrics.CreateCounter("crawls", "Processing crawls", "context");
    public RabbitMqCrawlRequestHandler(ILogger<RabbitMqCrawlRequestHandler> logger, ICrawlStrategyMapper crawlStrategyMapper)

    {
        _crawlStrategyMapper = crawlStrategyMapper;
        _logger = logger;
    }

    public async Task<CrawlResponse> HandleMessage(Option<Message<CrawlRequest>> m)
    {
        var message = m.Bind(mes => mes.Payload).Match(mes => mes, () => throw new System.Exception("Empty message"));
        _logger.LogInformation($"Preparing to Crawl: crawlRequestId: {message.Id}, Cont.: {message.ContinuationStrategy}");
        var strategy = await _crawlStrategyMapper.GetCrawlStrategy(message).Match(s => s, () => throw new Exception("Strategy missing"), ex => throw ex);
        var contStrategy = await _crawlStrategyMapper.GetContinuationStrategy(message).MatchUnsafe(s => s, () => null, ex => throw ex);
        var contStrategyOpt = contStrategy
        != null
        ? Option<ICrawlContinuationStrategy>.Some(contStrategy)
        : Option<ICrawlContinuationStrategy>.None;

        var request = new Request(Option<ICrawlStrategy>.Some(strategy), contStrategyOpt, message);

        _logger.LogInformation($"Starting Crawl: {message.Id}");
        _counter.WithLabels($"started").Inc();
        var response = await strategy.Crawl(request).Match(
            r => r, 
            () => { _counter.WithLabels($"failed").Inc(); throw new Exception("Empty result when crawling");}, 
            ex => { _counter.WithLabels($"failed").Inc(); throw ex;});
        _counter.WithLabels($"completed").Inc();

        _logger.LogInformation($"Completed Crawl: {message.Id}");
        return response;
    }

    private static CrawlEsResponseModel Map(CrawlResponse response)
    {
        var documentPart = response.Result.Bind(r => r.RequestDocumentPart).Match(d => d, () => throw new Exception("Empty result"));

        if(documentPart is DocumentPartArticle)
        {
            return CreateResponseModel(response, (DocumentPartArticle)documentPart);
        }

        var article = documentPart.GetAllParts<DocumentPartArticle>().FirstOrDefault();
        if(article == null)
        {
            return new CrawlEsResponseModel
            {
                Content = GetText(documentPart),
                CorrelationId = response.CorrelationId.Match(c => c.ToString(), () => string.Empty),
                CrawlerId = response.CrawlerId.Match(c => c.ToString(), () => string.Empty),
                Uri = response.Uri,
                Timestamp = response.Timestamp.Match( t => t.ToString(DateStrFormat), () => DateTime.UtcNow.ToString(DateStrFormat))
            };
        }

        return CreateResponseModel(response, article);
    }
    private const string DateStrFormat = "yyyy-MM-dd'T'HH:mm:ss.fff";
    private static CrawlEsResponseModel CreateResponseModel(CrawlResponse response, DocumentPartArticle article)
    {
        var title = article.Title.Bind(t => t.Text).Match(r => r, () => string.Empty);
        var contentDocPart = article.Content.Match(c => c, () => throw new Exception("Empty content"));
        var content = GetText(contentDocPart);
        var heading = GetText(article.GetAllParts("Heading").FirstOrDefault());

        if(string.IsNullOrEmpty(content))
            throw new Exception("Content empty - avoid indexing");

        return new CrawlEsResponseModel
        {
            Heading = heading,
            Title = title,
            Content = content,
            CorrelationId = response.CorrelationId.Match(c => c.ToString(), () => string.Empty),
            CrawlerId = response.CrawlerId.Match(c => c.ToString(), () => string.Empty),
            Uri = response.Uri,
            Timestamp = response.Timestamp.Match( t => t.ToString(DateStrFormat), () => DateTime.UtcNow.ToString(DateStrFormat))
        };
    }

    private static string GetText(DocumentPart docPart)
    {
        string content = string.Empty;
        if(docPart == null)
            return content;

        if (docPart is DocumentPartText)
        {
            content = ((DocumentPartText)docPart).Text.Match(t => t, () => string.Empty);
        }
        else
        {
            content = docPart
            .GetAllParts<DocumentPartText>()
            .Select(p => p.Text.Match(t => t.Trim(), () => string.Empty))
            .Aggregate(new StringBuilder(), (sb, val) =>
            {
                if (!string.IsNullOrEmpty(val))
                    sb.AppendLine(val);

                return sb;
            }, sb => sb.ToString());
        }

        return content;
    }
}
