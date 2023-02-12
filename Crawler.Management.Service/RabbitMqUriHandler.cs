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

namespace Crawler.Management.Service;

public class RabbitMqUriHandler : IMessageHandler<CrawlUri, CrawlUri>
{

    private readonly ILogger<RabbitMqUriHandler> _logger;
    private readonly ISchedulerRepository _schedulerRepository;

    public RabbitMqUriHandler(ILogger<RabbitMqUriHandler> logger, ISchedulerRepository repository)
    {
        _logger = logger;
        _schedulerRepository = repository;
    }

    public async Task<CrawlUri> HandleMessage(Option<CrawlUri> m)
    {
        var crawlUri = m.Match(mes => mes, () => throw new System.Exception("Empty message"));
        var uri = crawlUri.Uri.Match(u => u, () => throw new Exception("Uri is empty"));

        _logger.LogInformation($"Procesing Uri: {uri}: {crawlUri.UriTypeId}");

        await _schedulerRepository.UriLinkExists(uri.ToLowerInvariant())
        .Match(_ => { },
            async () =>
            {
                _logger.LogInformation($"Storing new Uri: {uri}");

                var id = await _schedulerRepository
                    .AddOrUpdate(new UriDataModel
                    {
                        UriTypeId = crawlUri.UriTypeId,
                        RoutingKey = "requests",
                        Uri = uri.ToLowerInvariant()
                    })
                    .Match(r => r, () => throw new Exception($"Failed to store: {uri}"), ex => throw ex);

            },
            ex => throw ex);

        return crawlUri;
    }
}

