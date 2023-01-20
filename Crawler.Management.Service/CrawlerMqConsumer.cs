using Crawler.Core.Requests;
using Crawler.DataModel;
using Microservice.Amqp;
using Microservice.Amqp.Rabbitmq;
using Microservice.Exchange.Endpoints.Rabbitmq;
using Microservice.Serialization;
using Microsoft.Extensions.Logging;

namespace Crawler.Management.Service;

public class CrawlerMqConsumer : RabbitMqConsumer<CrawlRequest, CrawlEsResponseModel>
{
    public CrawlerMqConsumer(ILogger<RabbitMqConsumer<CrawlRequest, CrawlEsResponseModel>> logger, IJsonConverterProvider converterProvider, IRabbitMqConnectionFactory rabbitMqConnectionFactory, IMessageHandler<CrawlRequest, CrawlEsResponseModel> messageHandler) : base(logger, converterProvider, rabbitMqConnectionFactory, messageHandler)
    {
    }
}
