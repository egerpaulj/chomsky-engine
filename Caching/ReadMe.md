# Microservices Caching Data

.NET library provides a high performance Generic Caching Solution.

High throughput performance can be gained by caching data in distributed systems:
- Caching of frequently accessed data
- Temporarily caching of, data accessed in bursts, of micro-service activities

**Note:** Contains git submodule 

```
git clone --recurse-submodules https://github.com/egerpaulj/Caching.git
```

OR

```
git clone https://github.com/egerpaulj/Caching.git
git submodule init
git submodule update
```


The following Interface is provided:

```
public interface ICacheProvider
{
     /// <summary>
     /// Store T in Cache with a unique identifier key.
     /// <para>Expiration duration stores an item for that duration; and then is automatically removed.</para>
     /// </summary>
     TryOptionAsync<Unit> StoreInCache<T>(Option<string> key, T value, double expirationDurationInSeconds = 0)

     /// <summary>
     /// Get T from Cache using a unique identifier key.
     /// </summary>
     TryOptionAsync<T> Get<T>(Option<string> key);
}
```

## StoreInCache<T>(Option<string> key, T value, double expirationDurationInSeconds)

Store an object or value in the Cache

- **key** - string identifier of the stored data
- **value** - Any object or value type
- **expirationDurationInSeconds** - once this duration expires, then the item is removed from the Cache

## TryOptionAsync<T> Get<T>(Option<string> key)

Gets an object or value from the Cache

- **key** - string identifier of the stored data

## Redis

A redis implementation of the interface is provided. Redis is a free, fast, in-memory data structure store.

**Note:** Redis has many features; the interface only utilizes the Caching features

See https://redis.io

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

Start a Redis container for integration tests; run the startRedis.sh; or the command:

```
docker run -d --rm --network development_network --name redis -p 6379:6379 redis
```

Alternatively update the **appSettings.Development** file to direct requests to your redis server

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
