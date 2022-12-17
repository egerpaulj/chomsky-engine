# Microservices Elasticsearch Repo

.NET Repository to Index C#.NET objects in Elastic Search.

**Note:** Contains git submodule 

```
git clone --recurse-submodules https://github.com/egerpaulj/Microservice.Elasticsearch.git
```

OR

```
git clone https://github.com/egerpaulj/Microservice.Elasticsearch.git
git submodule init
git submodule update
```

A generic library provides the following interface

```
public interface IElasticsearchRepository
{
     TryOptionAsync<Unit> IndexDocument<T>(Option<T> document, Option<string> indexKey) where T : IDataModel;
}
```

## IndexDocument<T>(Option<T> document, Option<string> indexKey) where T : IDataModel

Serializes the **document T** to JSON and indexes the document in Elastic Search.

- **IDataModel** ensures each Indexed **document T** has a unique identifier GUID: **Id**.
- **indexKey** specifies the Index in elastic search

See https://www.elastic.co/elasticsearch


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

Start a Elasticsearch container for integration tests; run the startElastic.sh;


Alternatively update the **appSettings.Development** file to direct requests to your Elastic Search server

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
