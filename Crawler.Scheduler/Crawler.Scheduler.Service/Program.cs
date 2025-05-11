using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Configuration.Repository;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.Management.Core.RequestHandling.Core.Amqp;
using Crawler.Microservice.Core;
using Crawler.RequestHandling.Core;
using Crawler.Scheduler.Core;
using Crawler.Scheduler.Repository;
using Microservice.Amqp;
using Microservice.Amqp.Rabbitmq;
using Microservice.Core;
using Microservice.Core.Middlewear;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Crawler.Scheduler.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseAppConfig()
                .SetupLogging()
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services.AddHostedService<Worker>();
                        services.AddTransient<ICrawlScheduler, CrawlerScheduler>();
                        services.AddTransient<
                            ICrawlerConfigurationService,
                            CrawlerConfigurationService
                        >();
                        services.AddTransient<IJobFactory, JobFactory>();
                        services.AddTransient<Quartz.Spi.IJobFactory, QuartzJobFactory>();

                        services.AddTransient<ISchedulerRepository, SchedulerRepository>();
                        services.AddTransient<IJsonConverterProvider, JsonConverterProvider>();
                        services.AddSingleton<IRequestPublisher, AmqpRequestPublisher>();
                        services.AddSingleton<IAmqpProvider, AmqpProvider>();
                        services.AddTransient<IAmqpBootstrapper, AmqpBootstrapper>();
                        services.AddTransient<
                            IRabbitMqConnectionFactory,
                            RabbitMqConnectionFactory
                        >();
                        services.AddTransient<UnscheduledUriCrawlJob>();
                        services.AddTransient<PeriodUriCrawlJob>();
                        services.AddTransient<UriCollectionJob>();
                        services.AddTransient<OnetimeUriJob>();
                        services.AddTransient<FoundUriJob>();

                        var databaseConfiguration = new DatabaseConfiguration(
                            "crawl_request",
                            hostContext.Configuration
                        );

                        services.AddTransient<
                            IConfigurationRepository,
                            MongoDbConfigurationRepository
                        >();
                        services.AddTransient<
                            IMongoDbRepository<CrawlRequestModel>,
                            MongoDbRepository<CrawlRequestModel>
                        >();

                        services.AddSingleton<IDatabaseConfiguration>(databaseConfiguration);
                    }
                );
    }
}
