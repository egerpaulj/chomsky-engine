using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Crawler.Core.Results;
using Crawler.DataModel;
using DnsClient.Internal;
using LanguageExt;
using Microservice.Mongodb.Repo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Crawler.Data.Repository;

public interface ICrawlerResponseShardedRepository : IMongoDbAddOrUpdate<CrawlResponseModel>
{
    TryOptionAsync<List<CrawlResponseModel>> GetMany(
        Option<string> uriOpt,
        Option<FilterDefinition<BsonDocument>> filter,
        int limit = 100,
        int skip = 0
    );

    TryOptionAsync<bool> HasResponse(Option<string> uri);
}

public class CrawlerResponseShardedRepository : ICrawlerResponseShardedRepository
{
    private static ConcurrentDictionary<
        string,
        IMongoDbRepository<CrawlResponseModel>
    > domainToRepoMap = new ConcurrentDictionary<string, IMongoDbRepository<CrawlResponseModel>>();

    private const string defaultRepositoryKey = "default";
    private const string collectionPrefix = "crawler_responses";
    private readonly IRepositoryFactory repositoryFactory;
    private readonly IConfiguration configuration;
    private readonly ILogger<CrawlerResponseShardedRepository> logger;

    public CrawlerResponseShardedRepository(
        IRepositoryFactory repositoryFactory,
        IConfiguration configuration,
        ILogger<CrawlerResponseShardedRepository> logger
    )
    {
        this.repositoryFactory = repositoryFactory;
        this.configuration = configuration;
        this.logger = logger;

        Init();
    }

    private void Init()
    {
        var databaseConfiguration = new DatabaseConfiguration(
            $"{collectionPrefix}_orphans",
            configuration
        );

        var repository = repositoryFactory
            .CreateRepositoryAsync<CrawlResponseModel>(databaseConfiguration)
            .Result;

        domainToRepoMap.TryAdd(defaultRepositoryKey, repository);
    }

    public TryOptionAsync<bool> HasResponse(Option<string> uri)
    {
        return uri.ToTryOptionAsync().Bind(HasResponseQuery);
    }

    private TryOptionAsync<bool> HasResponseQuery(string uriText)
    {
        return async () =>
        {
            var repository = await GetRepositoryAsync(uriText);

            return await repository
                .Get(Builders<BsonDocument>.Filter.Eq("UriText", uriText))
                .Match(_ => true, () => false, ex => throw ex);
        };
    }

    public TryOptionAsync<Guid> AddOrUpdate(Option<CrawlResponseModel> document)
    {
        return async () =>
        {
            var repository = await GetRepositoryAsync(
                document.Match(d => d.UriText ?? string.Empty, () => string.Empty)
            );

            return await repository
                .AddOrUpdate(document)
                .Match(r => r, () => throw new Exception("Empty result"), ex => throw ex);
        };
    }

    public TryOptionAsync<List<CrawlResponseModel>> GetMany(
        Option<string> uriOpt,
        Option<FilterDefinition<BsonDocument>> filter,
        int limit = 100,
        int skip = 0
    )
    {
        return async () =>
        {
            var repository = await GetRepositoryAsync(uriOpt.Match(d => d, () => string.Empty));

            return await repository
                .GetMany(filter, limit, skip)
                .Match(
                    r => r,
                    () => throw new Exception("Could not get crawler response"),
                    ex => throw ex
                );
        };
    }

    private async Task<IMongoDbRepository<CrawlResponseModel>> GetRepositoryAsync(string uriStr)
    {
        Uri uri;

        try
        {
            uri = new Uri(uriStr);

            if (domainToRepoMap.TryGetValue(uri.Host, out var mongoDbRepository))
            {
                return mongoDbRepository;
            }

            if (!CrawlResponseData.WhiteList.Any(a => a.StartsWith(uri.Host)))
                return domainToRepoMap[defaultRepositoryKey];

            var databaseConfiguration = new DatabaseConfiguration(
                $"{collectionPrefix}_{uri.Host}",
                configuration
            );

            var repository = await repositoryFactory.CreateRepositoryAsync<CrawlResponseModel>(
                databaseConfiguration
            );

            return repository;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to setup sharding for: {uriStr}", uriStr);
            return domainToRepoMap[defaultRepositoryKey];
        }
    }
}
