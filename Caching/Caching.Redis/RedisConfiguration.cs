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

using Microsoft.Extensions.Configuration;

namespace Caching.Redis
{
    public class RedisConfiguration : IRedisConfiguration
    {
        public string RedisHostname { get; }

        public int RedisPort { get; }

        public int RedisDatabaseNumber { get; }

        public bool UseResiliency { get; }

        public RedisConfiguration(IConfiguration configuration)
        {
            RedisHostname = configuration[IRedisConfiguration.HostnameKey] ?? IRedisConfiguration.HostDefault;
            RedisPort = int.Parse(configuration[IRedisConfiguration.PortKey] ?? IRedisConfiguration.PortDefault);
            RedisDatabaseNumber = int.Parse(configuration[IRedisConfiguration.DatabaseIndexKey] ?? IRedisConfiguration.DatabaseIndexDefault);
            UseResiliency = bool.Parse(configuration[IRedisConfiguration.ResiliencyKey] ?? "False");
        }
    }
}