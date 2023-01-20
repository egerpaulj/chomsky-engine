using System;
using System.Threading.Tasks;
using Crawler.Core.Requests;
using Crawler.Core.Strategy;
using Crawler.DataModel;
using Crawler.Stategies.Core;
using LanguageExt;
using Microservice.Amqp;
using Microsoft.Extensions.Logging;

namespace Crawler.Management.Service;

public class RabbitMqCrawlRequestHandler : IMessageHandler<CrawlRequest, CrawlEsResponseModel>
{
    private readonly ICrawlStrategyMapper _crawlStrategyMapper;

    private readonly ILogger<RabbitMqCrawlRequestHandler> _logger;
    public RabbitMqCrawlRequestHandler(ILogger<RabbitMqCrawlRequestHandler> logger, ICrawlStrategyMapper crawlStrategyMapper)

    {
        _crawlStrategyMapper = crawlStrategyMapper;
        _logger = logger;
    }

    public async Task<CrawlEsResponseModel> HandleMessage(Option<CrawlRequest> m)
    {
        var message = m.Match(mes => mes, () => throw new System.Exception("Empty message"));
        _logger.LogInformation($"Preparing to Crawl: {message.Id}");
        var strategy = await _crawlStrategyMapper.GetCrawlStrategy(message).Match(s => s, () => throw new Exception("Strategy missing"), ex => throw ex);
        var contStrategy = await _crawlStrategyMapper.GetContinuationStrategy(message).MatchUnsafe(s => s, () => null, ex => throw ex);
        var contStrategyOpt = contStrategy
        != null
        ? Option<ICrawlContinuationStrategy>.Some(contStrategy)
        : Option<ICrawlContinuationStrategy>.None;

        var request = new Request(Option<ICrawlStrategy>.Some(strategy), contStrategyOpt, message);

        _logger.LogInformation($"Starting Crawl: {message.Id}");
        var response = await strategy.Crawl(request).Match(r => r, () => throw new Exception("Empty result when crawling"), ex => throw ex);

        _logger.LogInformation($"Completed Crawl: {message.Id}");
        return new CrawlEsResponseModel(response.Map());
    }
}
