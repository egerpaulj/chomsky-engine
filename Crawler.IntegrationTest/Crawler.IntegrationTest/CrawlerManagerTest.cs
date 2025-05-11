using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Caching.Redis;
using Crawler.Core.Cache;
using Crawler.Core.Management;
using Crawler.Core.Metrics;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.Core.Strategy;
using Crawler.Management.Core.RequestHandling.Core.Amqp;
using Crawler.Management.Core.RequestHandling.Core.FileBased;
using Crawler.Microservice.Core;
using Crawler.RequestHandling.Core;
using Crawler.Strategies.General;
using Crawler.WebDriver.Grpc.Client;
using Elasticsearch.Net.Specification.SecurityApi;
using Microservice.Amqp;
using Microservice.Amqp.Rabbitmq;
using Microservice.Grpc.Core;
using Microservice.Serialization;
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
        private IAmqpBootstrapper _amqpBootstrapper;
        private IAmqpProvider _amqpProvider;

        IRequestPublisher _requestPublisher;

        // ToDo test the exchange instead
        //[TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task StartCrawlTestIntegration()
        {
            // ARRANGE - Dependencies
            SetupIntegrationTest();

            // ARRANGE - Environment
            //await _amqpBootstrapper.Bootstrap().Match(_ => {}, () => throw new Exception("Failed to bootstrap AMQP"));

            // ARRANGE - Publish a crawl request
            var environment = TestHelper.GetEnvironment();
            var requestText = await File.ReadAllTextAsync(
                $"Resources/56bc3065-fc3c-4af6-acc0-dda71f70c35f_{environment}.json"
            );
            var crawlRequest = new JsonConverterProvider().Deserialize<CrawlRequest>(requestText);

            await _requestPublisher
                .PublishRequest(crawlRequest)
                .Match(_ => { }, () => throw new Exception("Arrange failed"), ex => throw ex);

            // ACT
            // ToDo Using Microservice.Exchange instead of CrawlerManager - delete CrawlerManager

            // var t = _testee.Start().Match(a => a, () => throw new Exception("Failed to start crawls"));
            var subscriberOpt = _amqpProvider.GetSubsriber(
                "CrawlResponse",
                MessageHandlerFactory.Create<CrawlResponse, CrawlResponse>(response =>
                    response.Payload.Match(p => p, () => throw new Exception("Fail"))
                )
            );

            // ASSERT - Consume response
            var subscriber = await subscriberOpt.Match(
                s => s,
                () => throw new Exception("missing subscriber")
            );
            CrawlResponse response = null;

            var result = subscriber
                .GetObservable()
                .Subscribe(r =>
                    response = r.Match(
                        ex => throw ex,
                        r => response = r,
                        () => throw new Exception("No messages")
                    )
                );
            subscriber.Start();

            await Task.Delay(10000);

            // if(response == null)
            //     await Task.Delay(1000000);

            result.Dispose();
            subscriber.Dispose();

            Assert.IsNotNull(response);
            // await _amqpBootstrapper.Purge().Match(_ => {}, () => {});
        }

        private void SetupIntegrationTest()
        {
            _metricRegisterMock = new Mock<IMetricRegister>();

            var testRepository = new FileBasedRequestRepository(
                new DirectoryInfo("Requests"),
                100,
                new JsonConverterProvider()
            );
            _grpcMetricsMock = new Mock<IGrpcMetrics>();

            _appConfig = TestHelper.GetConfiguration();

            _loggerFactory = LoggerFactory.Create(b =>
            {
                b.AddSimpleConsole();
            });

            _requestRepository = new RequestRepository(
                testRepository,
                testRepository,
                testRepository
            );

            var webDriver = new GrpcWebDriverService(
                _appConfig,
                _loggerFactory.CreateLogger<GrpcWebDriverService>(),
                new GrpcMetrics(),
                new JsonConverterProvider()
            );

            var metricRegister = new MetricRegister();

            var crawlConfiguration = new CrawlerConfigurationGeneric(
                webDriver,
                metricRegister,
                _loggerFactory.CreateLogger<CrawlerConfigurationGeneric>()
            );

            var redisCache = new RedisCacheProvider(
                Mock.Of<ILogger<RedisCacheProvider>>(),
                new RedisConfiguration(_appConfig),
                new JsonConverterProvider()
            );
            var crawlerCache = new CrawlerCache(redisCache);

            var strategyMapper = new CrawlStrategiesMapper(
                _loggerFactory.CreateLogger<ICrawlContinuationStrategy>(),
                testRepository,
                webDriver,
                metricRegister
            );

            _amqpBootstrapper = new AmqpBootstrapper(_appConfig);

            _amqpProvider = new AmqpProvider(
                _appConfig,
                new JsonConverterProvider(),
                new RabbitMqConnectionFactory()
            );

            _requestPublisher = new AmqpRequestPublisher(_amqpProvider, _amqpBootstrapper);

            _testee = new CrawlerManager(
                _loggerFactory.CreateLogger<CrawlerManager>(),
                crawlConfiguration,
                crawlerCache,
                _metricRegisterMock.Object,
                _requestRepository,
                strategyMapper
            );
        }
    }
}
