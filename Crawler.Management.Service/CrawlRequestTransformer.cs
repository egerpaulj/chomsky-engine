using System;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.Core.Strategy;
using Crawler.DataModel;
using Crawler.Stategies.Core;
using LanguageExt;
using Microservice.Exchange;
using Microsoft.Extensions.Logging;

namespace Crawler.Management.Service;

public class CrawlRequestTransformer : ITransformer<CrawlRequest, CrawlEsResponseModel>
{
    private readonly ICrawlStrategyMapper _crawlStrategyMapper;

    private readonly ILogger<CrawlRequestTransformer> _logger;

    public CrawlRequestTransformer(ICrawlStrategyMapper crawlStrategyMapper, ILogger<CrawlRequestTransformer> logger)
    {
        _crawlStrategyMapper = crawlStrategyMapper;
        _logger = logger;
    }

    public TryOptionAsync<Message<CrawlEsResponseModel>> Transform(Option<Message<CrawlRequest>> input)
    {
        return input.ToTryOptionAsync().Bind(m => Crawl(m));
    }

    private TryOptionAsync<Message<CrawlEsResponseModel>> Crawl(Message<CrawlRequest> message)
    {
        return async () => 
        {
            _logger.LogInformation($"Preparing to Crawl: {message.Id}");
            var strategy = await _crawlStrategyMapper.GetCrawlStrategy(message.Payload).Match(s =>s, () =>  throw new Exception("Strategy missing"), ex => throw ex);
            var contStrategy = await _crawlStrategyMapper.GetContinuationStrategy(message.Payload).MatchUnsafe(s => s, () => null, ex => throw ex);
            var contStrategyOpt = contStrategy
            != null 
            ? Option<ICrawlContinuationStrategy>.Some(contStrategy)
            : Option<ICrawlContinuationStrategy>.None;
            
            var request = new Request(Option<ICrawlStrategy>.Some(strategy), contStrategyOpt, message.Payload );

            _logger.LogInformation($"Starting Crawl: {message.Id}");
            var response = await strategy.Crawl(request).Match(r => r, () => throw new Exception("Empty result when crawling"), ex => throw ex);

            var responseMessage = new Message<CrawlEsResponseModel>(message);
            responseMessage.Payload = new CrawlEsResponseModel(response.Map());

            _logger.LogInformation($"Completed Crawl: {message.Id}");
            return responseMessage;
        };
    }
}
