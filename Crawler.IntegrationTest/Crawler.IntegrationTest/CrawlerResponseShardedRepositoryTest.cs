using System;
using System.Threading.Tasks;
using Crawler.Data.Repository;
using Crawler.DataModel;
using Crawler.Microservice.Core;
using Microservice.Mongodb.Repo;
using Microservice.TestHelper;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

public class CrawlerResponseShardedRepositoryTest
{
    private IConfigurationRoot configuration;

    public CrawlerResponseShardedRepositoryTest()
    {
        configuration = TestHelper.GetConfiguration();
    }

    [Fact]
    public async Task TestRepositoryInsert()
    {
        var testee = new CrawlerResponseShardedRepository(
            new RepositoryFactory(configuration, new JsonConverterProvider()),
            configuration,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<CrawlerResponseShardedRepository>>()
        );

        await testee
            .AddOrUpdate(
                new CrawlResponseModel
                {
                    UriText = "https://www.theguardian.com/world/2025/WTFFFFFFFFFFFFF",
                    Uri = "https://www.theguardian.com/world/2025/WTFFFFFFFFFFFFFFFFFFFFF",
                }
            )
            .Match(r => r, () => throw new Exception("Failed to add document"));
    }
}
