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

namespace Caching.Redis
{
    public interface IRedisConfiguration
    {
        internal const string HostDefault = "localhost";
        internal const string HostnameKey = "RedisHostName";

        internal const string PortDefault = "6379";
        internal const string PortKey = "RedisPort";

        internal const string DatabaseIndexDefault = "0";
        internal const string DatabaseIndexKey = "RedisDatabaseIndex";
        internal const string ResiliencyKey = "RedisDatabaseIndex";

        string RedisHostname { get; }
        int RedisPort { get; }

        string Uri => $"{RedisHostname}:{RedisPort}";

        int RedisDatabaseNumber { get; }

        bool UseResiliency {get;}
    }
}