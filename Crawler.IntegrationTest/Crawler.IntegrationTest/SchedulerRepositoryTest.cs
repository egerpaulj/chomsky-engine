using System;
using System.Threading.Tasks;
using Crawler.DataModel.Scheduler;
using Crawler.Microservice.Core;
using Crawler.Scheduler.Repository;
using Microservice.TestHelper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Crawler.IntegrationTest
{
    [TestClass]
    [TestCategory("IntegrationTest")]
    public class SchedulerRepositoryTest
    {
        [TestInitialize]
        public async Task TestInit()
        {
            await CreateTestData();

        }

        [TestMethod]
        public async Task GetUnscheduledCrawlData_ThenNotEmpty()
        {
            var testee = CreateTestee();
            var data = await testee.GetUnscheduledCrawlUriData().Match(r => r, () => throw new Exception("Failed to get unscheduled crawl data"));

            Assert.IsTrue(data.Count > 0);
        }

        [TestMethod]
        public async Task GetPeriodicCrawlData_ThenNotEmpty()
        {
            var testee = CreateTestee();
            var data = await testee.GetPeriodicUriData().Match(r => r, () => throw new Exception("Failed to get periodic crawl data"));

            Assert.IsTrue(data.Count > 0);
        }

        [TestMethod]
        public async Task GetCollectorCrawlData_ThenNotEmpty()
        {
            var testee = CreateTestee();
            var data = await testee.GetCollectorUriData().Match(r => r, () => throw new Exception("Failed to get collector crawl data"));

            Assert.IsTrue(data.Count > 0);
        }

        [TestMethod]
        public async Task UriLinkExists_ThenTrue()
        {
            var testee = CreateTestee();
            var data = await testee.UriLinkExists("https://www.test.com/somewhereSpecific").Match(r => r, () => throw new Exception("Failed to check if uri exists"));

            Assert.IsTrue(data);
        }

        private async Task CreateTestData()
        {
            var testee = CreateTestee();

            var uriGuid = await testee.AddOrUpdate(new UriDataModel
            {
                CronPeriod = "* * * * * *",
                RoutingKey = "Request.Test*",
                Uri = "https://www.test.com/somewhereSpecific",
                UriTypeId = UriType.Periodic
            }).Match(r => r, () => throw new Exception("Failed to store uri data model"));

            await testee.AddOrUpdate(new CrawlUriDataModel
            {
                UriId = uriGuid,
            }).Match(r => r, () => throw new Exception("Failed to store crawl Uri data"));
        }

        private static SchedulerRepository CreateTestee()
        {
            var loggerFactory = LoggerFactory.Create(b =>
            {
                b.AddSimpleConsole();
            });

            return new SchedulerRepository(TestHelper.GetConfiguration(), new JsonConverterProvider());
        }
        
    }
}