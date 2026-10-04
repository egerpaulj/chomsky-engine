using System.Collections.Concurrent;
using Crawler.Core.Results;
using DnsClient.Internal;
using LanguageExt;
using Microservice.Mongodb.Repo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Crawler.Data.Repository;

public class CleanTextShardedRepository : ICleanTextShardedRepository
{
    private static ConcurrentDictionary<
        string,
        IMongoDbRepository<DataPipelineCleanText>
    > domainToRepoMap =
        new ConcurrentDictionary<string, IMongoDbRepository<DataPipelineCleanText>>();

    private const string defaultRepositoryKey = "default";
    private const string collectionPrefix = "crawler_responses_cleaned";
    private readonly IRepositoryFactory repositoryFactory;
    private readonly IConfiguration configuration;
    private readonly ILogger<CleanTextShardedRepository> logger;

    public CleanTextShardedRepository(
        IRepositoryFactory repositoryFactory,
        IConfiguration configuration,
        ILogger<CleanTextShardedRepository> logger
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
            .CreateRepositoryAsync<DataPipelineCleanText>(databaseConfiguration)
            .Result;

        domainToRepoMap.TryAdd(defaultRepositoryKey, repository);
    }

    public TryOptionAsync<Guid> AddOrUpdate(Option<DataPipelineCleanText> document)
    {
        return async () =>
        {
            var repository = await GetRepositoryAsync(
                document.Match(d => d.Uri ?? string.Empty, () => string.Empty)
            );

            return await repository
                .AddOrUpdate(document)
                .Match(r => r, () => throw new Exception("Empty result"), ex => throw ex);
        };
    }

    private async Task<IMongoDbRepository<DataPipelineCleanText>> GetRepositoryAsync(string uriStr)
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

            var repository = await repositoryFactory.CreateRepositoryAsync<DataPipelineCleanText>(
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
