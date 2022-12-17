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
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microservice.Exchange.Endpoints.Mongodb
{
    public static class FilterParser
    {
        public enum LogicalOperator
        {
            And,
            Or
        };

        public static FilterDefinition<BsonDocument> Parse(this FilterDefinition<BsonDocument> filterDefinition, IConfigurationSection configuration)
        {
            var logicalOperator = configuration.GetValue<string>("LogicalOperator") == "Or" ? LogicalOperator.Or : LogicalOperator.And;
            var fieldName = configuration.GetValue<string>("FieldName");
            var filterValue = configuration.GetValue<string>("FilterValue");

            var filter = GetFilterDefinition(configuration, fieldName, filterValue);
            if(logicalOperator == LogicalOperator.And)
                filterDefinition &= filter;
            else
                filterDefinition |= filter;

            return filterDefinition;
        }

        private static FilterDefinition<BsonDocument> GetFilterDefinition(
            IConfigurationSection configuration, 
            string fieldName, 
            string filterValue)
        {
            FilterDefinition<BsonDocument> filter;
            switch (configuration.Key)
            {

                case "Eq":
                    filter = Builders<BsonDocument>.Filter.Eq(fieldName, filterValue);
                    break;
                case "Gt":
                    filter = Builders<BsonDocument>.Filter.Gt(fieldName, filterValue);
                    break;
                case "Gte":
                    filter = Builders<BsonDocument>.Filter.Gte(fieldName, filterValue);
                    break;
                case "Lt":
                    filter = Builders<BsonDocument>.Filter.Lt(fieldName, filterValue);
                    break;
                case "Lte":
                    filter = Builders<BsonDocument>.Filter.Lte(fieldName, filterValue);
                    break;
                case "In":
                    filter = Builders<BsonDocument>.Filter.In(fieldName, filterValue);
                    break;
                case "Regex":
                    filter = Builders<BsonDocument>.Filter.Regex(fieldName, filterValue);
                    break;
                case "Text":
                    filter = Builders<BsonDocument>.Filter.Text(fieldName, filterValue);
                    break;
                default:
                    throw new System.Exception($"Unsupported filter definition: {configuration.Key}");
            }

            return filter;
        }
    }
}