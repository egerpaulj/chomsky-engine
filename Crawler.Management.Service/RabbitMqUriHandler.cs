using System;
using System.Threading.Tasks;
using Crawler.Core.Requests;
using Crawler.Core.Strategy;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.Stategies.Core;
using LanguageExt;
using Microservice.Amqp;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Crawler.Management.Service;

public class RabbitMqUriHandler : IMessageHandler<CrawlUri, CrawlUri>
{

    private readonly ILogger<RabbitMqUriHandler> _logger;
    private readonly ISchedulerRepository _schedulerRepository;
    private readonly IConfigurationRepository _configurationRepository;

    private static Counter _counter = Prometheus.Metrics.CreateCounter("crawl_uri_found", "Processing found uris", "context");

    public RabbitMqUriHandler(ILogger<RabbitMqUriHandler> logger, ISchedulerRepository repository, IConfigurationRepository configurationRepository)
    {
        _logger = logger;
        _schedulerRepository = repository;
        _configurationRepository = configurationRepository;
    }

    public async Task<CrawlUri> HandleMessage(Option<CrawlUri> m)
    {
        var crawlUri = m.Match(mes => mes, () => throw new System.Exception("Empty message"));
        var uri = crawlUri.Uri.Match(u => u, () => throw new Exception("Uri is empty"));

        _logger.LogInformation($"Procesing Uri: {uri}: {crawlUri.UriTypeId}");
        _counter.WithLabels("processing").Inc();

        await _schedulerRepository.UriLinkExists(uri.ToLowerInvariant())
        .Match(_ => { _counter.WithLabels("duplicate_uri_found").Inc();},
            async () =>
            {
                _logger.LogInformation($"Storing new Uri: {uri}");
                _counter.WithLabels("new_uri_found").Inc();

                var uriDataModel = new UriDataModel
                    {
                        UriTypeId = crawlUri.UriTypeId,
                        RoutingKey = "requests",
                        Uri = uri.ToLowerInvariant(),
                        BaseUri = crawlUri.BaseUri.Match(r => r, () => uri.ToLowerInvariant())
                    };
                var shouldSkip = await _configurationRepository.ShouldSkip(crawlUri.BaseUri, crawlUri.Uri).Match(
                    r => r, 
                    () => true, 
                    ex => throw new Exception("Can't determine skip uri"));

                if(shouldSkip)
                {
                    uriDataModel.IsSkipped = true;
                    uriDataModel.IsCompleted = true;
                    _counter.WithLabels("skipped").Inc();
                }

                var id = await _schedulerRepository
                    .AddOrUpdate(uriDataModel)
                    .Match(r => r, () => throw new Exception($"Failed to store: {uri}"), ex => throw ex);
                
                _counter.WithLabels("new_uri_stored").Inc();

            },
            ex => throw ex);

        return crawlUri;
    }
}

