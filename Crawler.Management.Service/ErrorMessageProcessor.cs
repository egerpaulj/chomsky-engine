using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.DataModel;
using Crawler.RequestHandling.Core;
using DnsClient.Internal;
using Microservice.Exchange;
using Microservice.Exchange.Bertrand;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using IMongRepositoryFactory = Microservice.Mongodb.Repo.IRepositoryFactory;

namespace Crawler.Management.Service;

public interface IErrorMessageProcessor
{
    Task ProcessMessages(
        IMongoDbRepository<BertrandStateDataModel> fromRepository,
        CancellationToken stoppingToken
    );
}

public class ErrorMessageProcessor : IErrorMessageProcessor
{
    private readonly IMongoDbRepository<CrawlResponseModel> _responseRepository;
    private readonly ILogger<ErrorMessageProcessor> logger;
    private readonly IJsonConverterProvider jsonConverterProvider;
    private readonly IRequestPublisher requestPublisher;

    public ErrorMessageProcessor(
        ILogger<ErrorMessageProcessor> logger,
        IJsonConverterProvider jsonConverterProvider,
        IRequestPublisher requestPublisher,
        IMongRepositoryFactory mongodbFactory,
        IConfiguration configuration
    )
    {
        DatabaseConfiguration databaseConfiguration = new DatabaseConfiguration(
            "crawler_responses",
            configuration
        );

        _responseRepository = mongodbFactory.CreateRepository<CrawlResponseModel>(
            databaseConfiguration
        );
        this.logger = logger;
        this.jsonConverterProvider = jsonConverterProvider;
        this.requestPublisher = requestPublisher;
    }

    public async Task ProcessMessages(
        IMongoDbRepository<BertrandStateDataModel> fromRepository,
        CancellationToken stoppingToken
    )
    {
        await foreach (
            var model in fromRepository.GetBatches(
                Builders<BsonDocument>.Filter.Empty,
                stoppingToken
            )
        )
        {
            bool shouldDelete = await ProcessModelShouldDelete(model);
            if (!shouldDelete)
            {
                continue;
            }

            await fromRepository
                .Delete(model.Id)
                .Match(
                    r => r,
                    () => throw new Exception("Failed to delete processed deadletter crawl message")
                );
        }
    }

    private async Task<bool> ProcessModelShouldDelete(BertrandStateDataModel model)
    {
        var type = System.Type.GetType(model.AssemblyQualifiedTypeName);
        var message = new Message<object>
        {
            CorrelationId = model.CorrelationId,
            Id = model.Id,
            Properties = model.Properties,
            RoutingKey = model.RoutingKey,
            Payload = jsonConverterProvider.Deserialize(model.Payload, type),
        };

        if (type == typeof(CrawlRequest))
        {
            var crawlRequest = (CrawlRequest)message.Payload;
            if (
                await HasResult(
                    crawlRequest.LoadPageRequest.Bind(r => r.Uri).Match(r => r, () => string.Empty)
                )
            )
            {
                // Delete
                return true;
            }

            if (!await requestPublisher.PublishRequest(crawlRequest).Match(r => true, () => false))
            {
                logger.LogWarning("Failed to process Crawl Request: " + message.CorrelationId);
                // Don't delete can't publish
                return false;
            }
        }

        if (type == typeof(CrawlUri))
        {
            var crawlUri = (CrawlUri)message.Payload;
            var uriType = crawlUri.UriTypeId;
            var baseUri = crawlUri.BaseUri.Match(
                r => r,
                () => throw new Exception("Base uri can't be empty")
            );
            var uri = crawlUri.Uri.Match(r => r, () => "");

            if (await HasResult(uri))
            {
                // Delete
                return true;
            }

            DocumentPartLink link = new DocumentPartLink(baseUri) { BaseUri = baseUri, Uri = uri };
            List<DocumentPartLink> linkList = [link];

            if (!string.IsNullOrEmpty(uri))
            {
                if (
                    await requestPublisher
                        .PublishUri(crawlUri.BaseUri, linkList, crawlUri.UriTypeId)
                        .Match(
                            r => true,
                            () => false,
                            ex =>
                            {
                                logger.LogError(ex, "Failed to publish uri: {Uri}", uri);
                                return false;
                            }
                        )
                )
                {
                    logger.LogWarning("Failed to process Crawl Uri: " + uri);
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<bool> HasResult(string uri)
    {
        var result = await _responseRepository
            .Get(Builders<BsonDocument>.Filter.Eq("Uri", uri))
            .Match(r => r != null, () => false);

        logger.LogInformation($"Result exists for: {uri}. {result}");

        return result;
    }
}
