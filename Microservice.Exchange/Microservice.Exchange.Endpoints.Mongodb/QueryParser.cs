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
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microservice.Exchange.Endpoints.Mongodb
{
    public static class QueryParser
    {
        public static TryOptionAsync<FilterDefinition<BsonDocument>> Parse(Option<IConfiguration> config)
        {
            return config.ToTryOptionAsync().Bind(configuration => configuration.ParseFilters());
        }

        private static TryOptionAsync<FilterDefinition<BsonDocument>> ParseFilters(this IConfiguration configuration)
        {
            return async () =>
            {
                FilterDefinition<BsonDocument> filterDefinition = Builders<BsonDocument>.Filter.Empty;

                foreach (var filterConfiguration in configuration.GetSection("DocumentFilters").GetChildren())
                {
                    filterDefinition = filterDefinition.Parse(filterConfiguration);
                }

                return await Task.FromResult(filterDefinition);
            };
        }
    }
}