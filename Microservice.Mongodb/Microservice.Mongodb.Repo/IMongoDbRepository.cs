//      Microservice Cache Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2021  Paul Eger  
//                                                                                                                                                                  
//      This program is free software: you can redistribute it and/or modify                                                                                                                                          
//      it under the terms of the GNU General Public License as published by                                                                                                                                          
//      the Free Software Foundation, either version 3 of the License, or                                                                                                                                             
//      (at your option) any later version.   
//                                                                                                                                                                        
//      This program is distributed in the hope that it will be useful,                                                                                                                                               
//      but WITHOUT ANY WARRANTY; without even the implied warranty of                                                                                                                                                
//      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                                                                                                                                                 
//      GNU General Public License for more details.                                                                                                                                                                  
//
//      You should have received a copy of the GNU General Public License                                                                                                                                             
//      along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using LanguageExt;
using Microservice.DataModel.Core;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microservice.Mongodb.Repo
{
    public interface IMongoDbRepository<T> where T : IDataModel
    {
        /// <summary>
        /// Add or update a document in MongoDb. Document T is Serialized to JSON and added/updated in MongoDb.
        /// </summary>
        TryOptionAsync<Guid> AddOrUpdate(Option<T> document);

        /// <summary>
        /// Get a Document T from MongoDb with Guid id.
        /// </summary>
        TryOptionAsync<T> Get(Option<Guid> id);

        /// <summary>
        /// Get the first Document T which matches the Filter.
        /// </summary>
        TryOptionAsync<T> Get(Option<FilterDefinition<BsonDocument>> filter);

        /// <summary>
        /// Get all Documents T that matches the filter.
        /// </summary>
        TryOptionAsync<List<T>> GetMany(Option<FilterDefinition<BsonDocument>> filter);

        /// <summary>
        /// Delete Documents that matches the given Id.
        /// </summary>
        TryOptionAsync<Unit> Delete(Option<Guid> id);

        /// <summary>
        /// Delete documents that matches the filter.
        /// </summary>
        TryOptionAsync<Unit> Delete(Option<FilterDefinition<BsonDocument>> filter);
    }
}