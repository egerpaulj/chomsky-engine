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
using Microservice.DataModel.Core;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Mongodb.Repo;

public interface IRepositoryFactory
{
    IMongoDbRepository<T> CreateRepository<T>(IDatabaseConfiguration databaseConfiguration) where T : IDataModel;
}

public class RepositoryFactory : IRepositoryFactory
{
    IConfiguration configuration;
    IJsonConverterProvider jsonConverterProvider;
    ILoggerFactory loggerFactory;


    public RepositoryFactory(IConfiguration configuration, IJsonConverterProvider jsonConverterProvider, ILoggerFactory loggerFactory)
    {
        this.configuration = configuration;
        this.jsonConverterProvider = jsonConverterProvider;
        this.loggerFactory = loggerFactory;
    }

    public IMongoDbRepository<T> CreateRepository<T>(IDatabaseConfiguration databaseConfiguration) where T : IDataModel
    {
        return new MongoDbRepository<T>(configuration, databaseConfiguration, jsonConverterProvider);
    }
}