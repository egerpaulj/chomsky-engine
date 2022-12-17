//      Microservice Grpc Libraries for .Net C#                                                                                                                                       
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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microservice.Grpc.Core
{
    public static class GrpcMiddleware
    {
        /// <summary>
        /// Map an endpoint to a Service that implements RpcServiceBase<>.
        /// </summary>
        public static IApplicationBuilder ConfigureGrpcService<T>(this IApplicationBuilder app) where T : class
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<T>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Invalid request. Context: remote procedural calls only");
                });
            });

            return app;
        }

        public static void ConfigureRpc(this IServiceCollection services)
        {
            services.AddGrpc();
        }
    }
}