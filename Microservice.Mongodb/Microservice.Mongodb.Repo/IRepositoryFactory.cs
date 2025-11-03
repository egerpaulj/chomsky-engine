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
using System;
using System.Threading.Tasks;
using Microservice.DataModel.Core;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Mongodb.Repo;

public interface IRepositoryFactory
{
    Task<IMongoDbRepository<T>> CreateRepositoryAsync<T>(
        IDatabaseConfiguration databaseConfiguration
    )
        where T : IDataModel;
}

public class RepositoryFactory(
    IConfiguration configuration,
    IJsonConverterProvider jsonConverterProvider
) : IRepositoryFactory
{
    readonly IConfiguration configuration = configuration;
    readonly IJsonConverterProvider jsonConverterProvider = jsonConverterProvider;

    public async Task<IMongoDbRepository<T>> CreateRepositoryAsync<T>(
        IDatabaseConfiguration databaseConfiguration
    )
        where T : IDataModel
    {
        var respository = new MongoDbRepository<T>(
            configuration,
            databaseConfiguration,
            jsonConverterProvider
        );

        await respository
            .EnsureCollectionExists()
            .Match(r => r, () => throw new Exception("Failed to collection for repository"));

        return respository;
    }
}
