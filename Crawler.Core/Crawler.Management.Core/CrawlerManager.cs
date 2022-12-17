//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2022  Paul Eger                                                                                                                                                                     

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
using System.Threading;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Core.Cache;
using Crawler.Core.Metrics;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.Core.Strategy;
using Crawler.Stategies.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using Crawler.RequestHandling.Core;

namespace Crawler.Core.Management
{
    public class CrawlerManager : ICrawlerManager
    {
        private readonly ICrawlerConfigurationService _crawlerConfiguration;
        private readonly ICache _cache;
        private readonly IMetricRegister _metrics;
        private readonly IRequestRepository _requestRepository;
        private readonly ILogger<CrawlerManager> _logger;

        private readonly ICrawlStrategyMapper _crawlStrategyMapper;
        private readonly SemaphoreSlim _startStopSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _crawlCancellationTokenSource;

        public CrawlerManager(ILogger<CrawlerManager> logger, ICrawlerConfigurationService crawlerConfiguration, ICache cache, IMetricRegister metrics, IRequestRepository requestRepository, ICrawlStrategyMapper crawlStrategyMapper)
        {
            _crawlerConfiguration = crawlerConfiguration;
            _cache = cache;
            _metrics = metrics;
            _requestRepository = requestRepository;
            _logger = logger;
            _crawlStrategyMapper = crawlStrategyMapper;
        }

        public TryOptionAsync<Unit> Start()
        {
            return async () =>
            {
                await _startStopSemaphore.WaitAsync();

                _crawlCancellationTokenSource?.Cancel(false);

                _crawlCancellationTokenSource = new CancellationTokenSource();

                var observable =
                    _requestRepository
                    .GetRequestObservable(_crawlCancellationTokenSource.Token, request =>
                    {
                        return StartCrawl(request).Match(u => u, () => throw new Exception("Empty Crawl Result"), ex => throw ex);
                    });

                observable.Subscribe(reqEither =>
                {
                    reqEither.MatchAsync<Unit>(async ex =>
                       {
                        if (ex.Request != null)
                        {
                            _logger.LogError(ex, CreateLogMessage("Failed Crawl", ex.Request));
                            return await _requestRepository.PublishFailure(ex.Request).Match(_ => { }, () => _logger.LogWarning("Failed to publish crawl failure"));
                        }
                        else
                        {
                            _logger.LogError(ex, $"Failed to Read/Process Crawl Request - {ex.InnerException?.Message}");
                            _metrics.IncrementCrawlFailedCount();
                            return Unit.Default;
                        }
                    }, req =>
                    {
                        _metrics.IncrementCrawlCompletedCount();
                        _logger.LogInformation(CreateLogMessage("Finished Crawl", req));
                        return Unit.Default;
                    });

                });

                _startStopSemaphore.Release();

                return await Task.FromResult(Unit.Default);
            };
        }


        public TryOptionAsync<Unit> Stop()
        {
            return async () =>
            {
                _crawlCancellationTokenSource.Cancel();

                return await Task.FromResult(Unit.Default);
            };
        }


        private TryOptionAsync<Unit> StartCrawl(Option<CrawlRequest> crawlRequest)
        {
            _metrics.IncrementCrawlRequestCount();

            var crawl = new Func<Request, TryOptionAsync<CrawlResponse>>(r =>
               r.CrawlStrategy
                       .ToTryOptionAsync()
                       .Bind(
                           c => c.Crawl(r)));


            var request = _crawlStrategyMapper.GetCrawlStrategy(crawlRequest).Bind<ICrawlStrategy, Request>(s => async () => await Task.FromResult(Option<Request>.Some(new Request(Option<ICrawlStrategy>.Some(s), null, crawlRequest))));

            return
                request
            .SelectMany(req => StoreInCache(req.CrawlRequest), (req, unit) => req)
            .Bind(r => crawl(r))
            .SelectMany(req => HandleResponse(req), (req, unit) => req)
            .SelectMany(response => UpdateCompletedInCache(response.CrawlerId), (res, _) => res)
            .BindAsync(async response =>
            {
                var crawlContinuationStrategy = await _crawlStrategyMapper.GetContinuationStrategy(crawlRequest).MatchUnsafe(s => s, () => null, ex => throw ex);
                return crawlContinuationStrategy != null ? crawlContinuationStrategy.Apply(response) : async () => await Task.FromResult(Unit.Default);
            });
        }

        private TryOptionAsync<Unit> HandleResponse(Option<CrawlResponse> response) => _requestRepository.PublishResponse(response);
        private TryOptionAsync<Unit> HandleFailure(Option<CrawlRequest> request) => _requestRepository.PublishFailure(request);

        private TryOptionAsync<Unit> UpdateCompletedInCache(Option<Guid> guid) => _cache.UpdateCrawlCompleted(guid);

        private TryOptionAsync<Unit> StoreInCache(Option<CrawlRequest> request)
        {
            return _cache.StoreCrawlEnded(new Crawl()
            {
                CrawlRequest = request,
                IsComplete = false,
                Timestamp = DateTime.UtcNow
            });
        }
        private string CreateLogMessage(string message, CrawlRequest request)
        {
            var crawlId = request?.CrawlId.Match(id => id.ToString(), () => "Unknown Id");
            var uri = request?.LoadPageRequest.Bind(r => r.Uri).Match(u => u, () => "Unknown Uri");
            return $"{message}, CrawlId: {crawlId}, Uri: {uri}";
        }
    }
}