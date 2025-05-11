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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace Microservice.Mongodb.Repo
{
    public class MongoDbRepository<T> : IMongoDbRepository<T>
        where T : IDataModel
    {
        private const string DefaultConnectionString = "mongodb://mongodb:27017";
        private const string ConnectionStringKey = "MongoDbConnectionString";
        private readonly string _databaseName = "default";
        private readonly string _collectionName = "default_document";

        private readonly string _connectionString;

        private readonly Lazy<MongoClient> _client;

        private IMongoDatabase Database => _client.Value.GetDatabase(_databaseName);
        private readonly IJsonConverterProvider _jsonConverterProvider;

        public MongoDbRepository(
            IConfiguration configuration,
            IDatabaseConfiguration databaseConfiguration,
            IJsonConverterProvider jsonConverterProvider
        )
        {
            _connectionString =
                configuration.GetConnectionString(ConnectionStringKey) ?? DefaultConnectionString;
            _client = new Lazy<MongoClient>(() => new MongoClient(_connectionString));
            _databaseName = databaseConfiguration.DatabaseName ?? _databaseName;
            _collectionName = databaseConfiguration.CollectionName ?? _collectionName;
            _jsonConverterProvider = jsonConverterProvider;
        }

        public TryOptionAsync<Guid> AddOrUpdate(Option<T> document)
        {
            return document.ToTryOptionAsync().Bind(model => AddOrUpdate(model));
        }

        public TryOptionAsync<Unit> Delete(Option<Guid> id)
        {
            return id.ToTryOptionAsync().Bind(idGuid => DeleteAll(GetIdFilter(idGuid)));
        }

        public TryOptionAsync<Unit> Delete(Option<FilterDefinition<BsonDocument>> filter)
        {
            return filter.ToTryOptionAsync().Bind(f => DeleteAll(f));
        }

        public TryOptionAsync<T> Get(Option<Guid> id)
        {
            return id.ToTryOptionAsync().Bind(idStr => GetModel(GetIdFilter(idStr)));
        }

        public TryOptionAsync<T> Get(Option<FilterDefinition<BsonDocument>> filter)
        {
            return filter.ToTryOptionAsync().Bind(f => GetModel(f));
        }

        public async IAsyncEnumerable<T> GetBatches(
            FilterDefinition<BsonDocument> filter,
            [EnumeratorCancellation] CancellationToken cancellationToken,
            int batchSize = 100
        )
        {
            var collection = Database.GetCollection<BsonDocument>(_collectionName);
            var cursor = await collection.FindAsync(
                filter,
                options: new FindOptions<BsonDocument, BsonDocument> { BatchSize = batchSize },
                cancellationToken
            );

            await cursor.MoveNextAsync();

            while (cursor.Current != null)
            {
                foreach (var document in cursor.Current)
                {
                    yield return _jsonConverterProvider.Deserialize<T>(document.ToJson());
                }

                await cursor.MoveNextAsync();
            }
        }

        public TryOptionAsync<List<T>> GetMany(
            Option<FilterDefinition<BsonDocument>> filter,
            int limit = 100,
            int skip = 0
        )
        {
            return filter.ToTryOptionAsync().Bind(f => GetModels(f, limit, skip));
        }

        private static FilterDefinition<BsonDocument> GetIdFilter(Guid idGuid)
        {
            return Builders<BsonDocument>.Filter.Eq(IDataModel.IdStr, idGuid.ToString());
        }

        private static FilterDefinition<BsonDocument> GetModelIdFilter(T model)
        {
            return Builders<BsonDocument>.Filter.Eq(IDataModel.IdStr, model.Id.ToString());
        }

        private BsonDocument GetBsonDocument(object model)
        {
            var json = _jsonConverterProvider.Serialize(model);
            var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(json);
            return bsonDocument;
        }

        private TryOptionAsync<Guid> AddOrUpdate(T model)
        {
            return async () =>
            {
                var collection = Database.GetCollection<BsonDocument>(_collectionName);
                if (model.Id == Guid.Empty)
                {
                    model.Id = Guid.NewGuid();
                    model.Created = $"{DateTime.UtcNow:yyyy.MM.dd:HH:mm:ss}";
                    await collection.InsertOneAsync(GetBsonDocument(model));
                    return model.Id;
                }
                else
                {
                    var found = await collection
                        .Find(GetModelIdFilter(model))
                        .FirstOrDefaultAsync();
                    if (found != null)
                    {
                        model.Updated = $"{DateTime.UtcNow:yyyy.MM.dd:HH:mm:ss}";
                        await collection.FindOneAndReplaceAsync(
                            GetModelIdFilter(model),
                            GetBsonDocument(model)
                        );
                    }
                    else
                    {
                        model.Created = $"{DateTime.UtcNow:yyyy.MM.dd:HH:mm:ss}";
                        await collection.InsertOneAsync(GetBsonDocument(model));
                    }
                    return model.Id;
                }
            };
        }

        private TryOptionAsync<T> GetModel(FilterDefinition<BsonDocument> filter)
        {
            return async () =>
            {
                var collection = Database.GetCollection<BsonDocument>(_collectionName);
                //var contents = await collection.FindAsync(d => true);

                var matchedModel = await collection.Find(filter).FirstOrDefaultAsync();
                if (matchedModel == null)
                    return Option<T>.None;
                var model = _jsonConverterProvider.Deserialize<T>(matchedModel.ToJson());

                return model;
            };
        }

        private TryOptionAsync<List<T>> GetModels(
            FilterDefinition<BsonDocument> filter,
            int limit = 100,
            int skip = 0
        )
        {
            return async () =>
            {
                var collection = Database.GetCollection<BsonDocument>(_collectionName);
                //var contents = await collection.FindAsync(d => true);

                var result = await collection.FindAsync(
                    filter,
                    new FindOptions<BsonDocument, BsonDocument> { Limit = limit, Skip = skip }
                );
                var results = await result.ToListAsync();

                return results
                    .Select(m => _jsonConverterProvider.Deserialize<T>(m.ToJson()))
                    .ToList();
            };
        }

        private TryOptionAsync<Unit> DeleteAll(FilterDefinition<BsonDocument> filter)
        {
            return async () =>
            {
                var collection = Database.GetCollection<BsonDocument>(_collectionName);
                //var contents = await collection.FindAsync( d => true);
                //await collection.DeleteManyAsync(d => true);
                var result = await collection.DeleteManyAsync(filter);

                return Unit.Default;
            };
        }
    }
}
