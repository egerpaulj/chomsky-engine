using Crawler.Configuration.Core;
using Caching.Core;
using Crawler.Core.Cache;
using Crawler.Core.Metrics;
using Caching.Redis;
using Crawler.Stategies.Core;
using Crawler.Strategies.General;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Grpc.Client;
using Microservice.Core.Middlewear;
using Microservice.Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microservice.Amqp.Rabbitmq;
using Microservice.Exchange.Core;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using Crawler.DataModel;
using Crawler.Configuration.Repository;
using Crawler.Scheduler.Repository;
using Crawler.DataModel.Scheduler;
using Microservice.Mongodb.Repo;
using Microservice.Exchange;
using Microservice.Serialization;
using Crawler.Microservice.Core;
using Microservice.Amqp;
using Crawler.Core.Requests;
using Crawler.RequestHandling.Core;
using Crawler.Management.Core.RequestHandling.Core.Amqp;

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
                .UseAppConfig()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddTransient<IExchangeFactory, ExchangeFactory>();
                    services.AddTransient<IExchangeMetrics, ExchangeMetrics>();
                    services.AddTransient<IJsonConverterProvider, JsonConverterProvider>();

                    var databaseConfiguration = new DatabaseConfiguration
                    {
                        DatabaseName = "Crawler",
                        DocumentName = "crawl_request"
                    };
                    services.AddSingleton<IDatabaseConfiguration>(databaseConfiguration);
                    services.AddSingleton<IMongoDbRepository<CrawlRequestModel>, MongoDbRepository<CrawlRequestModel>>();
                    services.AddSingleton<ISchedulerRepository, SchedulerRepository>();
                    services.AddSingleton<IConfigurationRepository, MongoDbConfigurationRepository>();
                    services.AddSingleton<ICrawlerConfigurationService, CrawlerConfigurationService>();
                    
                    // Cache Data
                    services.AddTransient<ICache, CrawlerCache>();
                    services.AddTransient<ICacheProvider, RedisCacheProvider>();
                    services.AddTransient<IRedisConfiguration, RedisConfiguration>();

                    // Metrics for Prometheus
                    services.AddTransient<IMetricRegister, MetricRegister>();

                    // Web Driver via Grpc Service (Switch to Firefox provider to run locally)
                    // Point to Request Manager in URI config to throttle requests (avoid getting blacklisted)
                    services.AddTransient<IWebDriverService, GrpcWebDriverService>();
                    services.AddTransient<IGrpcMetrics, GrpcMetrics>();

                    // Crawl Strategies mapped using URI
                    services.AddTransient<ICrawlStrategyMapper, CrawlStrategiesMapper>();
                    services.AddTransient<IRequestPublisher, AmqpRequestPublisher>();
                    services.AddSingleton<IAmqpProvider, AmqpProvider>();
                    services.AddTransient<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
                    services.AddTransient<IAmqpBootstrapper, AmqpBootstrapper>();
                    services.AddTransient<IMessageHandler<CrawlRequest, CrawlEsResponseModel>, RabbitMqCrawlRequestHandler>();
                    services.AddTransient<IMessageHandler<CrawlUri, CrawlUri>, RabbitMqUriHandler>();

                    // Console.WriteLine(typeof(CrawlRequestTransformer).AssemblyQualifiedName);
                    // Console.ReadLine();

                });
    }
}
