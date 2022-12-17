# Microservices MongoDb Repo

.NET Repository to Store/Retrieve/Delete C#.NET objects in MongoDb.

**Note:** Contains git submodule 

```
git clone --recurse-submodules https://github.com/egerpaulj/Microservice.Mongodb.git
```

OR

```
git clone https://github.com/egerpaulj/Microservice.Mongodb.git
git submodule init
git submodule update
```

This minimal and generic library provides the following interface:

```
public interface IElasticsearchRepository<T> where T : IDataModel
{
     TryOptionAsync<Guid> AddOrUpdate(Option<T> document);
         
     TryOptionAsync<T> Get(Option<Guid> id);
     TryOptionAsync<T> Get(Option<FilterDefinition<BsonDocument>> filter);
     TryOptionAsync<List<T>> GetMany(Option<FilterDefinition<BsonDocument>> filter);
     
     TryOptionAsync<Unit> Delete(Option<Guid> id);
     TryOptionAsync<Unit> Delete(Option<FilterDefinition<BsonDocument>> filter);
}
```

**Note:** Each document type **T** will be represented as a repository. This allows simple dependency injection and usage

## T and IDataModel

A repository represents a type **Document T**.

**T** should satisfy the following:

- implement **IDataModel**
- specify JSON serialization to serialization the Id as **_id**

E.g.

```
     [JsonProperty("_id")]
     public Guid Id { get; set; }
```

## Usage

### Dependency Injection

```
services.AddTransient<IMongoDbRepository<SomeDataModel>, MongoDbRepository<SomeDataModel>>();
services.AddTransient<IMongoDbRepository<AnotherModel>, MongoDbRepository<AnotherModel>>();
```

### Business repository

E.g. Business Repository to store and retrieve 2 models (**SomeDataModel**, **AnotherModel**); with Filters:

```
public class BusinessDataRepository : ISomeBusinessRepo
{
     private readonly IMongoDbRepository<SomeDataModel> _someDataRepository;
     private readonly IMongoDbRepository<AnotherModel> _anotherDataRepository;

     public MongoDbConfigurationRepository(IMongoDbRepository<SomeDataModel> someDataRepository, IMongoDbRepository<AnotherModel> anotherDataRepository)
     {
          _someDataRepository = someDataRepository;
          _anotherDataRepository = anotherDataRepository;
     }

     public TryOptionAsync<Guid> AddOrUpdate(Option<SomeDataModel> dataModel)
     {
          return _someDataRepository.AddOrUpdate(dataModel);
     }

     public TryOptionAsync<Guid> AddOrUpdate(Option<AnotherData> dataModel)
     {
          return _anotherDataRepository.AddOrUpdate(dataModel);
     }

     public TryOptionAsync<SomeDataModel> GetSomeData(Option<string> uri)
     {
          return uri
          .ToTryOptionAsync()
          .Bind(u => GetUriFilter(u))
          .Bind(filter => _someDataRepository.Get(filter));
     }

     public TryOptionAsync<AnotherModel> GetAnotherData(Option<Guid> id)
     {
          return id
          .ToTryOptionAsync()
          .Bind(filter => _anotherDataRepository.Get(filter));
     }

     private static TryOptionAsync<FilterDefinition<BsonDocument>> GetUriFilter(string uri, bool isCollector = false)
     {
         return async () =>
         {
             var host = new Uri(uri).Host;
             var filter = Builders<BsonDocument>.Filter.Eq("Host", host);
             filter &= (Builders<BsonDocument>.Filter.Eq("Uri", uri) | Builders<BsonDocument>.Filter.Eq("Uri", "*"));
             if(isCollector)
                 filter &= Builders<BsonDocument>.Filter.Eq("IsUrlCollector", "True");
             return await Task.FromResult(filter);
         };
     }

     // etc.
}

```

## TryOptionAsync<Guid> AddOrUpdate(Option<T> document);

Serializes the **document T** to JSON and stores the Document in MongoDb

If the **Id** is provided, then the existing document is updated; otherwise a new document is stored in MongoDb.

- **IDataModel** ensures each Indexed **document T** has a unique identifier GUID: **Id**.


## TryOptionAsync<List<T>> GetMany(Option<FilterDefinition<BsonDocument>> filter)

Instead of searching for an **Id** Guid, a filter definition can be provided to match several documents; or the first matching filter:

e.g. **Search for a document/object with a property name 'Division' and value 'Head Office'**


```
var filter = Builders<BsonDocument>.Filter.Eq<string>("Division", "Head Office");
```

To buld more complex filters see the documentation:

https://mongodb.com


## TryOptionAsync<T>

The interface uses a Monad **TryOptionAsync<T>** to represent a return type. This allows the following encapsulation:
- An Async operation
- Potential Exceptions
- Successfully Result T is returned (otherwise Exception or Null is returned).

This allows better binding of pure functions. The callee can decide when to propagate the monad back; by calling **.Match**.

Additionally, the callee can also use **.Bind** to another TryOptionAsync; creating a flow of pure functions

E.g.

```
var cacheResult = await _Get<string>("CacheId1")
                  .Match(
                      r => r, // Result found => return result
                      CreateDefault(u), // Result is null => create a default value
                      e => throw e // Exception => throw error or return a value; for error-cases
                      );
```

Note: null checks are not necessary.

see https://github.com/louthy/language-ext

## Integration Tests

Start a MongoDb container for integration tests; run the startMongoDb.sh; or the command:

Alternatively update the **appSettings.Development** file to direct requests to your MongoDb Test server

## License

Copyright (C) 2021  Paul Eger

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.