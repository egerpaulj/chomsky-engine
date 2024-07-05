using Crawler.Configuration.Core;
using Crawler.Configuration.Repository;
using Crawler.Core.Parser.DocumentParts.Serialilzation;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.Microservice.Core;
using Crawler.Scheduler.Repository;
using Microservice.Core.Middlewear;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Crawler.Configuration.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
            .AddControllers()
            .AddNewtonsoftJson(options => options.SerializerSettings.Converters.Add(new BaseClassConverter()));
            
            services.AddTransient<ICrawlerConfigurationService, CrawlerConfigurationService>();
            services.AddTransient<ISchedulerRepository, SchedulerRepository>();
            services.AddTransient<IConfigurationRepository, MongoDbConfigurationRepository>();
            services.AddTransient<IMongoDbRepository<CrawlRequestModel>, MongoDbRepository<CrawlRequestModel>>();
            services.AddTransient<IJsonConverterProvider, JsonConverterProvider>();
            services.AddTransient<IDatabaseConfiguration, DatabaseConfiguration>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app
            .UseHttpsRedirection()
            .UseRouting()
            .SetupMetrics()
            .UseAuthorization()
            .UseMiddleware<CorrelationIdMiddlware>()
            .UseMiddleware<RequestDurationMetricsMiddlewear>()
            .UseCustomSerilogRequestLogging()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
