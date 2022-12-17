using Crawler.Configuration.Client;
using Crawler.Configuration.Core;
using Caching.Core;
using Crawler.Core.Cache;
using Crawler.Core.Management;
using Crawler.Core.Metrics;
using Crawler.Microservice.Core;
using Crawler.Management.Core.RequestHandling.Core;
using Crawler.Management.Core.RequestHandling.Core.FileBased;
using Caching.Redis;
using Crawler.Stategies.Core;
using Crawler.Strategies.General;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Grpc.Client;
using Microservice.Core.Http;
using Microservice.Core.Middlewear;
using Microservice.Grpc.Core;
using Microservice.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Crawler.Management.Core.RequestHandling.Core.Elasticsearch;
using Microservice.Elasticsearch.Repo;
using Crawler.Management.Core.RequestHandling.Core.Amqp;
using Microservice.Amqp;
using Microservice.Amqp.Rabbitmq;
using Crawler.RequestHandling.Core;

namespace Crawler.Management.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .SetupLogging()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();

                    // Process one request at a time; polling - CrawlManager
                    // ToDo Amqp CrawlManager - Event Based - refactor to event based (Use Observable timer)
                    services.AddTransient<ICrawlerManager, CrawlerManager>();

                    // Configuration from Configuration WebApi Service
                    services.AddTransient<ICrawlerConfigurationService, CrawlerConfigurationRestClient>();
                    
                    // Communicate using an HTTP Client
                    services.SetupHttpClient();
                    services.AddTransient<IJsonConverterProvider, JsonConverterProvider>();
                    services.AddTransient<IHttpClientService, HttpClientService>();
                    
                    // Cache Data
                    services.AddTransient<ICache, CrawlerCache>();
                    services.AddTransient<ICacheProvider, RedisCacheProvider>();
                    services.AddTransient<IRedisConfiguration, RedisConfiguration>();

                    // Metrics for Prometheus
                    services.AddTransient<IMetricRegister, MetricRegister>();

                    // Crawl Strategies mapped using URI
                    services.AddTransient<ICrawlStrategyMapper, CrawlStrategiesMapper>();

                    // Web Driver via Grpc Service (Switch to Firefox provider to run locally)
                    // Point to Request Manager in URI config to throttle requests (avoid getting blacklisted)
                    services.AddTransient<IWebDriverService, GrpcWebDriverService>();
                    services.AddTransient<IGrpcMetrics, GrpcMetrics>();

                    
                    // Get requests and send to various repositories => defined by Provider and Publisher Injections
                    // ToDo Start different Request Managers based on configured paths. (can have multiple running)
                    services.AddTransient<IRequestRepository, RequestRepository>();
                    
                    // Currently all file based. Inject Elastic Search, MongoDb, Amqp as needed
                    var fileBasedEverything = new FileBasedRequestRepository(new JsonConverterProvider());
                    services.AddSingleton<IRequestPublisher>(fileBasedEverything);
                    services.AddTransient<IAmqpProvider, AmqpProvider>();
                    services.AddTransient<IAmqpBootstrapper, AmqpBootstrapper>();
                    services.AddTransient<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
                    services.AddTransient<IRequestProvider, AmqpRequestProvider>();
                    services.AddSingleton<IFailurePublisher>(fileBasedEverything);
                    //services.AddSingleton<IResponsePublisher>(fileBasedEverything);

                    services.AddTransient<IResponsePublisher, ElasticsearchPublisher>();
                    services.AddTransient<IElasticsearchRepository, ElasticsearchRepository>();
                });
    }
}
