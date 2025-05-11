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

//      You should have received a copy of the GNU General Public License
//      along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microservice.Mongodb.Repo
{
    public interface IDatabaseConfiguration
    {
        string DatabaseName { get; }
        string CollectionName { get; }
    }

    public class DatabaseConfiguration : IDatabaseConfiguration
    {
        public DatabaseConfiguration(string collectionName, string databaseName)
        {
            CollectionName = collectionName;
            DatabaseName = databaseName;
        }

        public DatabaseConfiguration(string collectionName, IConfiguration configuration)
        {
            DatabaseName = configuration.GetSection("MongoDbDatabaseName").Value;
            CollectionName = collectionName;
        }

        public string DatabaseName { get; }
        public string CollectionName { get; }
    }
}
