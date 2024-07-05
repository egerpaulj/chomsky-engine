//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2024  Paul Eger                                                                                                                                                                     

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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Exchange.Core.Polling;
using Microservice.Mongodb.Repo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microservice.Exchange.Endpoints.Mongodb;

public class MongoDbBertrandConsumer<T> : BertrandPollingConsumerBase where T : IDataModel
{
    public MongoDbBertrandConsumer(
        string name,
        IDatabaseConfiguration databaseConfiguration,
        ILoggerFactory loggerFactory,
        Option<FilterDefinition<BsonDocument>> filters,
        IPollingConsumerFactory pollingConsumerFactory,
        IRepositoryFactory repositoryFactory,
        int pollingIntervalMs,
        int limit = 0,
        int skip = 0
        )
        : base(name, loggerFactory.CreateLogger<MongoDbBertrandConsumer<T>>())
    {
        var repository = repositoryFactory.CreateRepository<T>(databaseConfiguration);
        var query = repository.GetMany(filters, limit, skip).Bind(Convert);
        PollingConsumer = pollingConsumerFactory
        .Create(() => query, pollingIntervalMs);
    }

    protected override IPollingConsumer<object> PollingConsumer {get;}

}