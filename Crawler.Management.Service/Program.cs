//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2024  Paul Eger

//      This program is free software: you can redistribute it and/or modify
//      it under the terms of the GNU General Public License as published by
//      the Free Software Foundation, either version 3 of the License, or
//      (at your option) any later version.

//      This program is distributed in the hope that it will be useful,
//      but WITHOUT ANY WARRANTY; without even the implied warranty of
//      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//      GNU General Public License for more details.

//      You should have received a copy of the GNU General Public License
//      along with this program.  If not, see <https://www.gnu.org/licenses/>.

using Caching.Core;
using Caching.Redis;
using Crawler.Configuration.Core;
using Crawler.Configuration.Repository;
using Crawler.Core.Cache;
using Crawler.Core.Metrics;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.Management.Core.RequestHandling.Core.Amqp;
using Crawler.Microservice.Core;
using Crawler.RequestHandling.Core;
using Crawler.Scheduler.Repository;
using Crawler.Stategies.Core;
using Crawler.Strategies.General;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Grpc.Client;
using Microservice.Amqp;
using Microservice.Amqp.Rabbitmq;
using Microservice.Core.Middlewear;
using Microservice.Exchange;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Grpc.Core;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IRepositoryFactory = Microservice.Elasticsearch.Repo.IRepositoryFactory;
using IRepositoryFactoryMongodb = Microservice.Mongodb.Repo.IRepositoryFactory;
using RepositoryFactory = Microservice.Elasticsearch.Repo.RepositoryFactory;
using RepositoryFactoryMongodb = Microservice.Mongodb.Repo.RepositoryFactory;

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
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services.AddHostedService<Worker>();
                        services.AddHostedService<WorkerDeadletter>();

                        services.AddTransient<IExchangeFactory, ExchangeFactory>();
                        services.AddTransient<IErrorMessageProcessor, ErrorMessageProcessor>();
                        services.AddTransient<IExchangeMetrics, ExchangeMetrics>();
                        services.AddTransient<IJsonConverterProvider, JsonConverterProvider>();

                        var databaseConfiguration = new DatabaseConfiguration(
                            "crawl_request",
                            hostContext.Configuration
                        );
                        services.AddSingleton<IDatabaseConfiguration>(databaseConfiguration);
                        services.AddSingleton<
                            IMongoDbRepository<CrawlRequestModel>,
                            MongoDbRepository<CrawlRequestModel>
                        >();
                        services.AddSingleton<ISchedulerRepository, SchedulerRepository>();
                        services.AddSingleton<
                            IConfigurationRepository,
                            MongoDbConfigurationRepository
                        >();
                        services.AddSingleton<
                            ICrawlerConfigurationService,
                            CrawlerConfigurationService
                        >();

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
                        services.AddTransient<
                            IRabbitMqConnectionFactory,
                            RabbitMqConnectionFactory
                        >();
                        services.AddTransient<IAmqpBootstrapper, AmqpBootstrapper>();

                        // Bertrand exchange
                        services.AddTransient<IBertrandExchangeFactory, BertrandExchangeFactory>();
                        services.AddTransient<IRepositoryFactory, RepositoryFactory>();
                        services.AddTransient<
                            IRepositoryFactoryMongodb,
                            RepositoryFactoryMongodb
                        >();
                        services.AddTransient<IBertrandMetrics, BertrandMetrics>();
                    }
                );
    }
}
