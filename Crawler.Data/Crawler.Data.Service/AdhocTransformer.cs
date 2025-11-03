using System;
using System.Runtime.CompilerServices;
using Crawler.Core.Requests;
using Crawler.Data.Repository;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microservice.Exchange;
using Microservice.Exchange.Core.Bertrand;
using Microsoft.Extensions.Logging;

namespace Crawler.Data.Adhoc;

public class AdhocTransformer<TIn>(
    ILogger<IBertrandTransformer> logger,
    IRequestPublisher requestPublisher,
    IConfigurationRepository configurationRepository,
    ISchedulerRepository schedulerRepository,
    ICrawlerResponseShardedRepository responseShardedRepository
) : IBertrandTransformer
    where TIn : CrawlRequest
{
    public string Name => "Adhoc data processing";

    public TryOptionAsync<Message<object>> Transform(Option<Message<object>> input)
    {
        return async () =>
        {
            var inputMessage = input.Match(m => m, () => throw new Exception("message is empty"));
            var message = (TIn)
                input
                    .Bind(mes => mes.Payload)
                    .Match(mes => mes, () => throw new System.Exception("Empty message"));

            var uri = message
                .LoadPageRequest.Bind(p => p.Uri)
                .Match(r => r, () => throw new Exception("Missing uri"));

            uri = uri.Replace(".com./", ".com/");

            logger.LogInformation($"Uri found for: {uri}");

            var output = new Message<object>();
            output = inputMessage.CopyDataInto(output);
            output.RoutingKey = "None";

            output.Payload = null;

            if (
                await configurationRepository
                    .IsCollectable(uri)
                    .Match(r => r, () => false, ex => throw ex)
            )
                return output;

            if (
                await responseShardedRepository
                    .HasResponse(uri)
                    .Match(r => r, () => false, ex => throw ex)
            )
                return output;

            if (await schedulerRepository.UriLinkExists(uri).Match(r => r, () => false))
                return output;

            var newRequest = await configurationRepository
                .GetCrawlRequest(uri)
                .Match(r => r, () => throw new Exception("Failed to get reuqest"), ex => throw ex);

            var request = newRequest.Map(
                uri,
                message.CorrelationCrawlId.Match(r => r, () => Guid.NewGuid()),
                Guid.NewGuid(),
                false
            );

            await requestPublisher
                .PublishRequest(request)
                .Match(r => r, () => throw new Exception("Failed to publish"), ex => throw ex);

            await schedulerRepository
                .AddOrUpdate(
                    new UriDataModel
                    {
                        Uri = uri,
                        UriTypeId = UriType.Found,
                        IsCompleted = true,
                    }
                )
                .Match(r => r, () => throw new Exception("Failed to add uri"), ex => throw ex);

            return output;
        };
    }
}
