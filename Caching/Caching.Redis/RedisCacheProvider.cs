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

using System;
using System.Threading.Tasks;
using Caching.Core;
using LanguageExt;
using StackExchange.Redis;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Microservice.Serialization;
using Microsoft.Extensions.Logging;

namespace Caching.Redis
{
    public class RedisCacheProvider : ICacheProvider, IDisposable
    {
        private readonly string _uri;
        private readonly int _dbNumber;
        private readonly ILogger<RedisCacheProvider> _logger;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private readonly Lazy<ConnectionMultiplexer> _redisMultiplexer;
        private readonly Lazy<IDatabaseAsync> _database;
        private readonly IRedisConfiguration _configuration;
        private IDatabaseAsync Database => _database.Value;
        private bool disposedValue;

        public RedisCacheProvider(ILogger<RedisCacheProvider> logger, IRedisConfiguration configuration, IJsonConverterProvider jsonConverterProvider)
        {
            _configuration = configuration;
            _uri = configuration.Uri;
            _dbNumber = configuration.RedisDatabaseNumber;

            _redisMultiplexer = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(_uri));
            _database = new Lazy<IDatabaseAsync>(() => _redisMultiplexer.Value.GetDatabase(_dbNumber, _uri));
            _logger = logger;
            _jsonConverterProvider = jsonConverterProvider;

        }

        public TryOptionAsync<T> Get<T>(Option<string> key)
        {
            return async () =>
            {
                var redisKey = GetRedisKey(key);
                var result = await ExecuteResiliently(Database.StringGetAsync(redisKey), redisKey.ToString());
                return await Task.FromResult(_jsonConverterProvider.Deserialize<T>(result.HasValue ? result.ToString().Trim('"') : string.Empty));
            };
        }

        public TryOptionAsync<Unit> StoreInCache<T>(Option<string> key, T data, double expirationDurationInSeconds = 0)
        {
            return async () =>
            {
                var redisKey = GetRedisKey(key);

                Option<T> value = data;

                var resilientExecutor = new Func<Task<bool>, Task<bool>>(work => ExecuteResiliently(work, redisKey.ToString()));

                bool isSuccessful = await value.MatchAsync(
                    // store in cache
                    v =>
                        {
                            if (expirationDurationInSeconds <= 0)
                                return resilientExecutor(Database.StringSetAsync(redisKey, _jsonConverterProvider.Serialize(v)));
                            else
                                return
                                    resilientExecutor(Database.StringSetAsync(redisKey, _jsonConverterProvider.Serialize(v), TimeSpan.FromSeconds(expirationDurationInSeconds)));
                        },
                    // No value => remove item from cache
                    () => resilientExecutor(Database.KeyDeleteAsync(redisKey)));

                if (!isSuccessful)
                    throw new Exception($"Failed to store in Cache. Key: {redisKey}");

                return await Task.FromResult(Unit.Default);
            };
        }

        private async Task<T> ExecuteResiliently<T>(Task<T> task, string context)
        {
            if (!_configuration.UseResiliency)
            {
                return await task;
            }

            var retryPolicy = Policy
            .Handle<TaskCanceledException>()
            .Or<RedisTimeoutException>()
            .Or<RedisConnectionException>()
            .Or<RedisServerException>()
            .Or<System.AggregateException>()
            .Or<TimeoutException>()
            .Or<OperationCanceledException>()
            .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 5),
                    (exception, _) =>
                    {
                        _logger.LogError(exception, "Cache Server/Timeout/Cancelation Error");
                        _logger.LogWarning($"Timeout or cancellation exception. Retrying context: {context}");
                    });

            return await retryPolicy.ExecuteAsync<T>(() => task);
        }

        private static RedisKey GetRedisKey(Option<string> key) => key.Match(k => new RedisKey(k), () => throw new CacheException("Cache Key is empty", string.Empty));

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_redisMultiplexer.IsValueCreated)
                        _redisMultiplexer.Value.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}