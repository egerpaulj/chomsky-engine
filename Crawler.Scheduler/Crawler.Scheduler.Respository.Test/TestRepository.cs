using System;
using System.Linq;
using System.Threading.Tasks;
using Crawler.Scheduler.Repository;
using Microservice.Serialization;
using Microservice.TestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Crawler.Scheduler.Respository.Test
{
    [TestClass]
    public class TestRepository
    {
        private SchedulerRepository _testee;

        [TestInitialize]
        public void Setup()
        {
            _testee = new SchedulerRepository(
                TestHelper.GetConfiguration(),
                Mock.Of<IJsonConverterProvider>()
            );
        }

        [TestMethod]
        public async Task ListPeriodic()
        {
            var results = await _testee
                .GetPeriodicUriData()
                .Match(r => r, () => throw new Exception("Empty result"), ex => throw ex);

            if (results.Any())
            {
                foreach (var result in results)
                    Console.WriteLine(
                        $"{result.Uri}: {result.CronPeriod}, RoutingKey: {result.RoutingKey}"
                    );
            }
        }

        [TestMethod]
        public async Task ListAllCollectorSource()
        {
            var results = await _testee
                .GetCollectorUriData()
                .Match(r => r, () => throw new Exception("Empty result"), ex => throw ex);

            if (results.Any())
            {
                foreach (var result in results)
                    Console.WriteLine(
                        $"Id:{result.Id}, {result.Uri}: {result.CronPeriod}, UriType: {result.UriTypeId}"
                    );
            }
        }

        [TestMethod]
        public async Task ListUnscheduled()
        {
            var results = await _testee
                .GetUnscheduledCrawlUriData()
                .Match(r => r, () => throw new Exception("Empty result"), ex => throw ex);

            if (results.Any())
            {
                foreach (var result in results)
                {
                    Console.WriteLine($"{result.ScheduledTimestamp}: UriId: {result.UriId}");

                    var uriData = await _testee
                        .GetUriData(result.UriId)
                        .Match(r => r, () => throw new Exception("Empty result"), ex => throw ex);
                    Console.WriteLine(
                        $"{uriData.Uri}, {uriData.CronPeriod}, RoutingKey: {uriData.RoutingKey}"
                    );
                }
            }
        }
    }
}
