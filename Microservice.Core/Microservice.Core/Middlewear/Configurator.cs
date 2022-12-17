//      Microservice Core Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2021  Paul Eger                                                                                                                                                                     

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

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

using Prometheus;
using Microsoft.Extensions.DependencyInjection;
using Microservice.Core.Http;
using Polly.Extensions.Http;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Microservice.Core.Middlewear
{
    public static class Configurator
    {
        /// <summary>
        /// All requests are logs with CorrelationId.
        /// </summary>
        public static IApplicationBuilder UseCustomSerilogRequestLogging(this IApplicationBuilder app)
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (econtext, hcontext) =>
                {
                    econtext.Set("CorrelationId", CorrelationIdMiddlware.GetRequestCorrId(hcontext));
                };
            });

            return app;
        }

        /// <summary>
        /// Configures SeriLog sinks (from appsettings) and Setup Dependency injection ILogger to use Serilog. 
        /// </summary>
        public static IHostBuilder SetupLogging(this IHostBuilder host)
        {
            var configuration = new ConfigurationBuilder()
                                .SetBasePath(Environment.CurrentDirectory)
                                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json")
                                .Build();

            Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(configuration)
                        .Enrich.WithProperty("Application", Assembly.GetCallingAssembly().GetName().Name)
                        .CreateLogger();


            host.UseSerilog(Log.Logger);

            return host;
        }

        ///<summary>
        /// Hosts all prometheus metrics collected.
        /// Collect HTTP Metrics for the application.
        /// 
        /// Ensure this is called after app.UseRouting()
        ///</summary>
        public static IApplicationBuilder SetupMetrics(this IApplicationBuilder app)
        {
            app.UseHttpMetrics();
            app.UseMetricServer();

            return app;
        }

        /// <summary>
        /// <see cref="IHttpClientService"/> implementation is configured with resiliency. Configures dependency injection for <see cref="IHttpClientService"/>.
        /// </summary>
        public static IServiceCollection SetupHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient<IHttpClientService, HttpClientService>()
            .AddPolicyHandler(
                HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 5),
                    (m, _) =>
                    {
                        Log.Logger.Warning($"Retrying request. StatusCode: {m.Result?.StatusCode}. Uri: {m.Result?.RequestMessage?.RequestUri}");
                    }));

            return services;
        }

        /// <summary>
        /// Listens using kestrel server on Port/Certificate defined in the configuration file (see ReadMe.md for details). The certificate is deleted by default.
        /// </summary>
        public static IWebHostBuilder UseKestrelHttps(this IWebHostBuilder builder)
        {
            builder.UseKestrel((context, options) =>
            {
                int.TryParse(context.Configuration["Port"], out var port);
                var certPath = context.Configuration["CertPath"];
                var passPath = context.Configuration["PassPath"];

                options.Listen(IPAddress.Any, port, listOptions =>
                        {
                            listOptions.UseHttps(certPath, File.ReadAllLines(passPath)[0]);
                            listOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                        });
#if DEBUG
#else
                    File.Delete(passPath);
                    File.Delete(certPath);
#endif
            });

            return builder;
        }
    }
}