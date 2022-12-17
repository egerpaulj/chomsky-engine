//      Microservice Cache Libraries for .Net C#                                                                                                                                       
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

using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Microservice.TestHelper
{
    public class TestHelper
    {
        public const string TestUri = "http://test.com";
        
        /// <summary>
        /// Loads the JSON configuration file based on the ASPNETCORE_ENVIRONMENT. E.g. appsettings.{environment}.json
        /// </summary>
        public static IConfigurationRoot GetConfiguration()
        {
            string environment = GetEnvironment();

            return GetConfiguration(environment);
        }

        /// <summary>
        /// Loads the JSON configuration file based on the environment. E.g. appsettings.{environment}.json
        /// </summary>
        public static IConfigurationRoot GetConfiguration(string environment)
        {
            return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.{environment}.json")
            .Build();
        }

        /// <summary>
        /// Allows unsage HTTP communication for tests.
        /// </summary>
        public static void AllowUnsafeHttpCommunication()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }


        /// <summary>
        /// Gets the value stored in the Environment Variable: ASPNETCORE_ENVIRONMENT.
        /// </summary>
        public static string GetEnvironment()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Console.WriteLine($"Configured environment ASPNETCORE_ENVIRONMENT: {environment}");
            return environment ?? "Development";
        }


    }
}