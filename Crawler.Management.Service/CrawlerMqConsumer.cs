using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.DataModel;
using Microservice.Amqp;
using Microservice.Amqp.Rabbitmq;
using Microservice.Exchange.Endpoints.Rabbitmq;
using Microservice.Serialization;
using Microsoft.Extensions.Logging;

namespace Crawler.Management.Service;

public class CrawlerMqConsumer : RabbitMqConsumer<CrawlRequest, CrawlResponse>
{
    public CrawlerMqConsumer(ILogger<RabbitMqConsumer<CrawlRequest, CrawlResponse>> logger, IJsonConverterProvider converterProvider, IRabbitMqConnectionFactory rabbitMqConnectionFactory, IMessageHandler<CrawlRequest, CrawlResponse> messageHandler) : base(logger, converterProvider, rabbitMqConnectionFactory, messageHandler)
    {
    }
}

public class UriConsumer : RabbitMqConsumer<CrawlUri, CrawlUri>
{
    public UriConsumer(ILogger<RabbitMqConsumer<CrawlUri, CrawlUri>> logger, IJsonConverterProvider converterProvider, IRabbitMqConnectionFactory rabbitMqConnectionFactory, IMessageHandler<CrawlUri, CrawlUri> messageHandler) : base(logger, converterProvider, rabbitMqConnectionFactory, messageHandler)
    {
    }
}
