//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2022  Paul Eger                                                                                                                                                                     

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microservice.Core.Middlewear;
using Microservice.Grpc.Core;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Selenium.Firefox;
using Microservice.Serialization;

namespace Crawler.WebDriver.Grpc.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IGrpcMetrics, GrpcMetrics>();
            services.AddTransient<IWebDriverService, WebDriverServiceFirefox>();
            services.AddTransient<IWebDriverMetrics, WebDriverMetrics>();
            services.AddTransient<IJsonConverterProvider, EmptyJsonConverterProvider>();
            services.AddGrpc();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app
            .UseRouting()
            .UseCustomSerilogRequestLogging()
            .SetupMetrics()
            .UseMiddleware<RequestDurationMetricsMiddlewear>()
            .ConfigureGrpcService<WebDriverService>();
            //app.Conf
        }
    }
}
