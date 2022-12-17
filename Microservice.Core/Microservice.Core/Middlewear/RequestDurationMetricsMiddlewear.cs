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

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace Microservice.Core.Middlewear
{
    /// <summary>
    /// Middlewear records the request duration in milliseconds and registers the duration in a Prometheus Histogram with label (request_duration_in_ms).
    /// </summary>
    public class RequestDurationMetricsMiddlewear
    {
        private readonly RequestDelegate _next;
        private readonly Histogram _histogram;

        public RequestDurationMetricsMiddlewear(RequestDelegate next)
        {
            _next = next;
            _histogram = Metrics.CreateHistogram("request_duration_in_ms", "Records the requests duration in milliseconds" );
        }

        public async Task Invoke(HttpContext context)
        {
            if(context.Response == null)
                return;

            var stopwatch = new Stopwatch();
            stopwatch.Start();


            context.Response.OnStarting( () => 
            {
                stopwatch.Stop();
                _histogram.Observe(stopwatch.ElapsedMilliseconds);
                
                return Task.CompletedTask;
            });


            await _next(context);
        }
    }
}