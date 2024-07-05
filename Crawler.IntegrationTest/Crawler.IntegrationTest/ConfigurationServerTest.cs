using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Crawler.Configuration.Client;
using Microservice.Core.Http;
using System;
using Crawler.Core.Parser.DocumentParts.Serialilzation;
using Crawler.Core.Parser.DocumentParts;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microservice.TestHelper;
using Crawler.Microservice.Core;
using Microsoft.Extensions.Hosting;

namespace Crawler.IntegrationTest
{
    [TestClass]
    [TestCategory("IntegrationTest")]
    public class ConfigurationServerTest
    {
        [TestInitialize]
        public async Task Setup()
        {
            await new MongoDbConfigurationTest().StoreConfigurationIntegrationTest();
        }

        [TestCleanup]
        public async Task CleanUp()
        {
            await new MongoDbConfigurationTest().DeleteAll();
        }

        [TestMethod]
        public void MatchHostTets()
        {
            var uri = new Uri("https://www.test.com/society/2023/apr/17/junior-doctors-strike-led-to-195000-hospital-cancellations-last-week-nhs-england");
            Assert.AreEqual("www.test.com", uri.Host);
        }
        
        [TestMethod]
        [TestCategory("IntegrationTest")]
        
        public async Task GetCrawlerRequestTest()
        {
            CrawlerConfigurationRestClient testee = CreateTestee();

            var result = await testee.CreateRequest("https://test.com/asd", Guid.NewGuid(), Guid.NewGuid())
            .Match(r => r, () => throw new Exception("Empty result"), e => throw e);

            Assert.IsNotNull(result);
            var docPartType = result.RequestDocument.Bind(r => r.RequestDocumentPart).Bind(d => d.DocPartType).Match(dp => dp, () => throw new Exception("Invalid Configuration"));

            Assert.AreEqual(DocumentPartType.AutoDetect, docPartType);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public void GetDocumentPartTest()
        {
            CrawlerConfigurationRestClient testee = CreateTestee();

            var result = testee.GetExpectedDocumentPart("https://test.com/asd", Guid.NewGuid(), Guid.NewGuid())
            .Match(r => r, () => throw new Exception("Empty result"), e => throw e).Result;

            Assert.IsNotNull(result);

            Assert.AreEqual(DocumentPartType.AutoDetect, result.DocPartType);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task GetUiActionsTest()
        {
            CrawlerConfigurationRestClient testee = CreateTestee();

            var result = await testee.GetUiActions("https://test.com/asd", Guid.NewGuid(), Guid.NewGuid())
            .Match(r => r, () => throw new Exception("Empty result"), e => throw e);

            Assert.IsNotNull(result);
        }

        //[TestMethod]
        // ToDo add unscheduled crawl data
        [TestCategory("IntegrationTest")]
        public async Task GetUnscheduledCrawlData_ThenNotEmpty()
        {
            CrawlerConfigurationRestClient testee = CreateTestee();
            await StoreDetectedUri();

            var data = await testee.GetUnscheduledCrawlUriData().Match(r => r, () => throw new Exception("Failed to get unscheduled crawl data"));

            Assert.IsTrue(data.Count > 0);
        }

        // ToDo Add periodic data. See SchedulerRepositoryTest:CreateTestData
        // [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task GetPeriodicCrawlData_ThenNotEmpty()
        {
            var testee = CreateTestee();
            var data = await testee.GetPeriodicUri().Match(r => r, () => throw new Exception("Failed to get periodic crawl data"));

            Assert.IsTrue(data.Count > 0);
        }

        // ToDo Add Collector data. See SchedulerRepositoryTest:CreateTestData
        //[TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task GetCollectorCrawlData_ThenNotEmpty()
        {
            var testee = CreateTestee();
            var data = await testee.GetCollectorUri().Match(r => r, () => throw new Exception("Failed to get collector crawl data"));

            Assert.IsTrue(data.Count > 0);
        }

        // ToDO Test this
        public async Task StoreDetectedUri()
        {
            var testee = CreateTestee();
            var data = await testee.StoreDetectedUrls(new List<DocumentPartLink>
            {
                new DocumentPartLink("https://www.test2.com")
                {
                    Uri = "https://www.test2.com/somelink",
                    Text = "Test Link"
                }
            }, Guid.NewGuid()).Match(r => r, () => throw new Exception("Failed to save"), ex => throw ex);
        }

        private static CrawlerConfigurationRestClient CreateTestee()
        {
            var loggerFactory = LoggerFactory.Create(b =>
            {
                b.AddSimpleConsole();
            });

            var testee = new CrawlerConfigurationRestClient(
                new HttpClientService(
                    new System.Net.Http.HttpClient(),
                    loggerFactory.CreateLogger<HttpClientService>(),
                    new JsonConverterProvider()),
                TestHelper.GetConfiguration());
            return testee;
        }
    }
}
