using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Caching.Redis;
using Crawler.Core.Cache;
using Crawler.Core.Management;
using Crawler.Core.Metrics;
using Crawler.Core.Strategy;
using Crawler.Management.Core.RequestHandling.Core.FileBased;
using Crawler.Microservice.Core;
using Crawler.RequestHandling.Core;
using Crawler.Strategies.General;
using Crawler.WebDriver.Grpc.Client;
using Microservice.Grpc.Core;
using Microservice.TestHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Crawler.IntegrationTest
{
    [TestClass]
    public class CrawlerManagerTest
    {
        private CrawlerManager _testee;
        private IConfigurationRoot _appConfig;
        private ILoggerFactory _loggerFactory;

        private Mock<IMetricRegister> _metricRegisterMock;
        private Mock<IGrpcMetrics> _grpcMetricsMock;
        private IRequestRepository _requestRepository;

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task StartCrawlTestIntegration()
        {
            // ThreadPool.SetMaxThreads(128, 32);
            // ThreadPool.SetMinThreads(32, 32);
            if (Directory.Exists("Requests"))
                Directory.Delete("Requests", true);

            SetupIntegrationTest();


            var t = _testee.Start().Match(a => a, () => throw new Exception("Failed to start crawls"));

            // ACT
            var environment = TestHelper.GetEnvironment();
            File.Copy($"Resources/56bc3065-fc3c-4af6-acc0-dda71f70c35f_{environment}.json", "Requests/in/56bc3065-fc3c-4af6-acc0-dda71f70c35f.json");

            Task.Delay(25000).Wait();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (Directory.GetFiles("Requests/out").Length == 0)
            {
                if (stopWatch.ElapsedMilliseconds > 40000)
                    throw new TimeoutException("Failed to get result within 40s");

                await Task.Delay(3000);
            }

            stopWatch.Stop();
            _testee.Stop();
            var resultFiles = Directory.GetFiles("Requests/out").Length;

            Assert.AreEqual(1, resultFiles);
        }

        private void SetupIntegrationTest()
        {
           
            _metricRegisterMock = new Mock<IMetricRegister>();

            var testRepository = new FileBasedRequestRepository(new DirectoryInfo("Requests"), 100, new JsonConverterProvider());
            _grpcMetricsMock = new Mock<IGrpcMetrics>();

            _appConfig = TestHelper.GetConfiguration();

            _loggerFactory = LoggerFactory.Create(b =>
            {
                b.AddSimpleConsole();
            });

            _requestRepository = new RequestRepository(testRepository, testRepository, testRepository);

            var webDriver = new GrpcWebDriverService(
                _appConfig,
                _loggerFactory.CreateLogger<GrpcWebDriverService>(),
                new GrpcMetrics(),
                new JsonConverterProvider());

            var metricRegister = new MetricRegister();

            var crawlConfiguration = new CrawlerConfigurationGeneric(webDriver,
            metricRegister,
            _loggerFactory.CreateLogger<CrawlerConfigurationGeneric>());

            var redisCache = new RedisCacheProvider(Mock.Of<ILogger<RedisCacheProvider>>(), new RedisConfiguration(_appConfig), new JsonConverterProvider());
            var crawlerCache = new CrawlerCache(redisCache);

            var strategyMapper = new CrawlStrategiesMapper(_loggerFactory.CreateLogger<ICrawlContinuationStrategy>(), testRepository, webDriver, metricRegister);

            _testee = new CrawlerManager(_loggerFactory.CreateLogger<CrawlerManager>(), crawlConfiguration, crawlerCache, _metricRegisterMock.Object, _requestRepository, strategyMapper);
        }
    }
}