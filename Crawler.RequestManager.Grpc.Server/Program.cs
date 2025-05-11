using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microservice.Core.Middlewear;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Crawler.RequestManager.Grpc.Server
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
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrelHttps().UseStartup<Startup>();
                });
    }
}
