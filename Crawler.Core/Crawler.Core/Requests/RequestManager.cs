//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2022  Paul Eger

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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core.Cache;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Crawler.Core.Requests
{
    public class RequestManager : IRequestManager
    {
        private bool _jitterTime = false;
        private const int JitterWaitInMinutes = 10;

        private Dictionary<string, int> _recursionControl = new Dictionary<string, int>();
        private const int DownloadRecursionThreshold = 2000;

        // ToDo add to database configuration
        private readonly Dictionary<string, int> minThrottleValues = new Dictionary<string, int>
        {
            { "www.londonstockexchange.com", 1 },
            { "www.theguardian.com", 4 },
        };

        private readonly Dictionary<string, int> maxThrottleValues = new Dictionary<string, int>
        {
            { "www.londonstockexchange.com", 3 },
            { "www.theguardian.com", 10 },
        };

        private const int MinThrottleValueDefault = 5;
        private const int MaxThrottleValueDefault = 15;
        private readonly int minThrottleValue;
        private readonly int maxThrottleValue;
        private readonly ICache _cache;
        private readonly Random _random;

        private readonly ILogger<RequestManager> _logger;

        private SemaphoreSlim _requestSemaphore = new SemaphoreSlim(1, 1);
        private SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(1, 1);

        private string _host;

        public RequestManager(ILogger<RequestManager> logger, ICache cache, string host)
        {
            _random = new Random();
            _cache = cache;
            _logger = logger;
            _host = host;

            if (minThrottleValues.ContainsKey(host))
            {
                minThrottleValue = minThrottleValues[host];
            }
            else
                minThrottleValue = MinThrottleValueDefault;

            if (maxThrottleValues.ContainsKey(host))
            {
                maxThrottleValue = maxThrottleValues[host];
            }
            else
                maxThrottleValue = MaxThrottleValueDefault;
        }

        private async Task ThrottleRequest()
        {
            if (_jitterTime)
            {
                // ToDo Skip jitter for now - fail fast
                // _logger.LogInformation($"Jittering request for {JitterWaitInMinutes}minutes: {_host}");
                // await Task.Delay(TimeSpan.FromMinutes(JitterWaitInMinutes));
                _jitterTime = false;
            }

            var lastRequest = await _cache.GetLastRequestTime(_host).Match(d => d, DateTime.UtcNow);
            var now = DateTime.UtcNow;

            var waitSeconds = _random.Next(minThrottleValue, maxThrottleValue);

            var timeSinceLastRequest = now - lastRequest;

            if (timeSinceLastRequest.TotalSeconds > waitSeconds)
                return;

            waitSeconds = waitSeconds - (int)timeSinceLastRequest.TotalSeconds;

            if (waitSeconds <= 0)
                return;

            if (waitSeconds > maxThrottleValue)
                waitSeconds = maxThrottleValue;

            _logger.LogInformation($"Throtting request for {waitSeconds}s: {_host}");

            await Task.Delay(waitSeconds * 1000);
        }

        public async Task ThrottleDownload()
        {
            var isDownloadActive = await _cache.IsActiveDownload(_host).Match(d => d, false);
            while (isDownloadActive)
            {
                var valueExists = _recursionControl.TryGetValue(_host, out var recursionCount);
                if (valueExists && recursionCount > DownloadRecursionThreshold)
                    throw new CrawlException(
                        "Download throttle, waited too long",
                        ErrorType.ThrottleError
                    );

                // ToDo Potential Expoential backoff with a threshold would be better.....
                _recursionControl.Add(_host, recursionCount + 1);

                var waitSeconds = _random.Next(minThrottleValue, maxThrottleValue);
                _logger.LogInformation($"Throtting download request for {waitSeconds}s: {_host}");
                await Task.Delay(waitSeconds);

                isDownloadActive = await _cache.IsActiveDownload(_host).Match(d => d, false);
            }
        }

        public TryOptionAsync<T> ThrottleRequest<T>(Func<TryOptionAsync<T>> action)
        {
            return async () =>
            {
                await _requestSemaphore.WaitAsync();
                try
                {
                    await ThrottleRequest();
                    try
                    {
                        return await action()
                            .Match(
                                res => res,
                                () =>
                                    throw new CrawlException(
                                        "Request action failed (empty result)",
                                        ErrorType.PageLoadError
                                    ),
                                e =>
                                    throw new CrawlException(
                                        "Request action failed",
                                        ErrorType.PageLoadError,
                                        e
                                    )
                            );
                    }
                    catch (Exception)
                    {
                        _jitterTime = true;
                        throw;
                    }
                    finally
                    {
                        await _cache
                            .StoreLastRequest(_host)
                            .Match(
                                u => u,
                                () =>
                                    throw new CrawlException(
                                        $"Failed to store Uri in cache(empty result): {_host}",
                                        ErrorType.ThrottleError
                                    ),
                                e =>
                                    throw new CrawlException(
                                        $"Failed to store Uri in cache: {_host}",
                                        ErrorType.ThrottleError,
                                        e
                                    )
                            );
                    }
                }
                finally
                {
                    _requestSemaphore.Release();
                }
            };
        }

        public TryOptionAsync<T> ThrottleDownload<T>(Func<TryOptionAsync<T>> action)
        {
            return async () =>
            {
                await _downloadSemaphore.WaitAsync();
                try
                {
                    await ThrottleDownload();
                    try
                    {
                        return await action()
                            .Match(
                                res => res,
                                () =>
                                    throw new CrawlException(
                                        "Download action failed (empty result)",
                                        ErrorType.FileDownloadError
                                    ),
                                e =>
                                    throw new CrawlException(
                                        "Request action failed",
                                        ErrorType.FileDownloadError,
                                        e
                                    )
                            );
                    }
                    finally
                    {
                        await _cache
                            .SetActiveDownload(_host, false)
                            .Match(
                                u => u,
                                () =>
                                    throw new CrawlException(
                                        $"Failed to Download Uri in cache (empty result): {_host}",
                                        ErrorType.ThrottleError
                                    ),
                                e =>
                                    throw new CrawlException(
                                        "Failed to store Download Uri in cache: {_host}",
                                        ErrorType.ThrottleError,
                                        e
                                    )
                            );
                    }
                }
                finally
                {
                    _downloadSemaphore.Release();
                }
            };
        }
    }
}
