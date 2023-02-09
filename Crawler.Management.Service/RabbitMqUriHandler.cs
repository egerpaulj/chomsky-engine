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
        var uri = m.Bind(m => m.Uri).Match(mes => mes, () => throw new System.Exception("Empty message"));
        _logger.LogInformation($"Procesing Uri: {uri}");

        await _schedulerRepository.UriLinkExists(uri.ToLowerInvariant())
        .Match(_ => {},
        async () => {
            _logger.LogInformation($"Storing new Uri: {uri}");
            
            var id = await _schedulerRepository
                .Add(new UriDataModel
                {
                    UriTypeId = UriType.Onetime,
                    RoutingKey = "requests",
                    Uri = uri.ToLowerInvariant()
                })
                .Match(r => r, () => throw new Exception($"Failed to store: {uri}"), ex => throw ex);

            _logger.LogInformation($"Adding Uri for scheduling: {uri}");

            await _schedulerRepository
            .Add(new CrawlUriDataModel
            {
                UriId = id
            })
            .Match(r => r, () => throw new Exception($"Failed to add for scheduling: {uri}"), ex => throw ex);

        },
        ex => throw ex);

        return m.Match(m => m, () => throw new Exception("Not possible"));
    }
}
