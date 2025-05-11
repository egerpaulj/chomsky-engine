using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caching.Core;
using Caching.Redis;
using Crawler.Core.Cache;
using Crawler.Core.Requests;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Grpc.Client;
using Microservice.Core.Middlewear;
using Microservice.Grpc.Core;
using Microservice.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Crawler.RequestManager.Grpc.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IGrpcMetrics, GrpcMetrics>();
            services.AddSingleton<IRequestManagerFactory, RequestManagerFactory>();
            services.AddSingleton<ICache, CrawlerCache>();
            services.AddSingleton<ICacheProvider, RedisCacheProvider>();
            services.AddSingleton<IRedisConfiguration, RedisConfiguration>();
            services.AddTransient<IWebDriverService, GrpcWebDriverService>();
            services.AddTransient<IJsonConverterProvider, EmptyJsonConverterProvider>();
            services.AddGrpc();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting()
                .UseCustomSerilogRequestLogging()
                .SetupMetrics()
                .UseMiddleware<RequestDurationMetricsMiddlewear>()
                .ConfigureGrpcService<RequestManagerService>();
            //app.Conf
        }
    }
}
