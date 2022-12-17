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

using System.Collections.Generic;
using LanguageExt;
using Microservice.DataModel.Core;

namespace Microservice.Elasticsearch.Repo
{
    public interface IElasticsearchRepository
    {
        /// <summary>
        /// Index a document of type T. The Document is serialized into JSON and indexed using the provided the key.
        /// </summary>
        TryOptionAsync<Unit> IndexDocument<T>(Option<T> document, Option<string> indexKey) where T : IDataModel;

        /// <summary>
        /// Search elastic-search for documents of type T; based on the query.
        /// </summary>
        TryOptionAsync<List<T>> Search<T>(Option<string> index, Option<string> queryJson) where T : class, IDataModel;

        /// <summary>
        /// Delete an entire index
        /// </summary>
        TryOptionAsync<Unit> Delete(Option<string> index);
    }
}