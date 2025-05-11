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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Crawler.Core;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.UserActions;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Core.Exceptions;
using Crawler.WebDriver.Selenium.Firefox.UserActions;
using Crawler.WebDriver.Selenium.UserActions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace Crawler.WebDriver.Selenium.Firefox
{
    public class WebDriverFirefoxRobust : IWebDriverService
    {
        private const int PageLoadWaitTimeoutInSeconds = 10;
        private readonly IWebDriverMetrics _webDriverMetrics;
        private readonly ILogger<WebDriverServiceFirefox> _logger;
        private readonly Dictionary<string, FirefoxContainer> _activeDrivers =
            new Dictionary<string, FirefoxContainer>();

        private readonly SemaphoreSlim _firefoxContainerSemaphoreSlim = new SemaphoreSlim(1, 1);

        private const int WAIT_FOR_PAGE_LOAD_IN_SECONDS = 60;
        private readonly bool _isHeadless;
        private bool disposedValue;

        public WebDriverFirefoxRobust(
            IWebDriverMetrics metrics,
            ILogger<WebDriverServiceFirefox> logger
        )
            : this(metrics, logger, true) { }

        public WebDriverFirefoxRobust(
            IWebDriverMetrics metrics,
            ILogger<WebDriverServiceFirefox> logger,
            bool isHeadless
        )
        {
            _logger = logger;
            _webDriverMetrics = metrics;
            _isHeadless = isHeadless;

            var options = new FirefoxOptions();
            if (_isHeadless)
                options.AddArgument("-headless");

            options.AddArgument("--no-sandbox");
            options.Profile = new FirefoxProfile();
        }

        public TryOptionAsync<FileData> Download(Option<DownloadRequest> downloadRequest)
        {
            var uri = downloadRequest
                .Bind(r => r.Uri)
                .Match(r => r, () => throw new DownloadException("Uri is empty"));
            var correlationId = downloadRequest
                .Bind(r => r.CorrelationId)
                .Match(c => c, () => Guid.NewGuid());

            return DownloadBytes(uri, correlationId).Bind(result => CreateFileData(result, uri));
        }

        public TryOptionAsync<string> LoadPage(Option<LoadPageRequest> request)
        {
            var correlationId = request
                .Bind(r => r.CorrelationId)
                .Match(c => c, () => Guid.NewGuid());

            var userActions = request
                .Bind(r => r.UserActions)
                .Match(s => s, () => new List<UiAction>())
                .Select(WebDriverServiceFirefox.Map);

            var uri = request
                .Bind(r => r.Uri)
                .Match(u => u, () => throw new Exception("Firefox dirver: Uri empty"));

            return async () =>
            {
                var container = await GetFirefoxContainer(new Uri(uri));

                try
                {
                    await Task.Delay(300);
                    container.Driver.Navigate().GoToUrl(uri);

                    var wait = new WebDriverWait(
                        container.Driver,
                        TimeSpan.FromSeconds(WAIT_FOR_PAGE_LOAD_IN_SECONDS)
                    );
                    wait.PollingInterval = TimeSpan.FromMilliseconds(300);
                    if (!wait.Until(WaitForDocumentToLoad(container)))
                    {
                        _logger.LogWarning(
                            "Document state is not ready. Attempt to parse source anyway"
                        );
                        //throw new Exception($"Failed to load page within {WAIT_FOR_PAGE_LOAD_IN_SECONDS}s - document not ready");
                    }

                    await WebDriverServiceFirefox
                        .ExecuteUserActions(_logger, container, userActions, correlationId)
                        .Match(
                            r => r,
                            () => throw new Exception("User Actions failed"),
                            ex => throw new Exception("User Actions Failed", ex)
                        );

                    _webDriverMetrics.IncPageLoad(uri);
                    return container.Driver.PageSource;
                }
                catch (WebDriverException ex)
                {
                    _logger.LogError(
                        ex,
                        "Error connecting to Driver Management. Attempt to get source anyway: "
                            + uri
                    );

                    await WebDriverServiceFirefox
                        .ExecuteUserActions(_logger, container, userActions, correlationId)
                        .Match(
                            r => r,
                            () => throw new Exception("User Actions failed"),
                            ex => throw new Exception("User Actions Failed", ex)
                        );
                    _webDriverMetrics.IncPageLoad(uri);

                    return container.Driver.PageSource;
                }
                catch (Exception ex)
                {
                    _webDriverMetrics.IncPageLoadFail();
                    _logger.LogError(ex, $"Failed to load page: {uri}");
                    CleanUpWebDrivers(uri);
                    throw;
                }
            };

            static Func<IWebDriver, bool> WaitForDocumentToLoad(FirefoxContainer container)
            {
                return d =>
                    container.Driver.ExecuteScript("return document.readyState").Equals("complete");
            }
        }

        private static TryOptionAsync<FileData> CreateFileData(
            string binaryData,
            Option<string> uri
        )
        {
            return async () =>
            {
                var uriLocalPath = new Uri(
                    uri.Match(s => s, () => throw new DownloadException("Uri is empty"))
                ).LocalPath;
                var fileName = System.IO.Path.GetFileName(uriLocalPath);

                return await Task.FromResult(
                    new FileData()
                    {
                        Name = fileName,
                        Uri = uri,
                        Data = binaryData,
                    }
                );
            };
        }

        private TryOptionAsync<string> DownloadBytes(string uri, Guid correlationId)
        {
            return async () =>
            {
                var client = new WebClient();
                client.Headers.Add(
                    "user-agent",
                    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)"
                );

                _logger.LogInformation($"Downloading: {uri}. CorrelationId: {correlationId}");
                var data = client.DownloadData(uri);
                _logger.LogInformation(
                    $"Successfully Downloaded: {uri}. CorrelationId: {correlationId}"
                );
                _webDriverMetrics.IncDownload(new Uri(uri).Host);

                return await Task.FromResult(Encoding.GetEncoding("iso-8859-1").GetString(data));
            };
        }

        private async Task<FirefoxContainer> GetFirefoxContainer(Uri indicator)
        {
            await _firefoxContainerSemaphoreSlim.WaitAsync();

            try
            {
                if (_activeDrivers.ContainsKey(indicator.Host))
                    return _activeDrivers[indicator.Host];
                var options = new FirefoxOptions();

                if (_isHeadless)
                    options.AddArgument("-headless");

                options.AddArgument("--no-sandbox");

                var container = new FirefoxContainer(
                    new FirefoxDriver("/bin", options, TimeSpan.FromSeconds(120)),
                    indicator.Host
                );
                var driver = (OpenQA.Selenium.WebDriver)container.Driver;
                driver.Url = indicator.AbsoluteUri;
                driver.ResetInputState();
                _activeDrivers.Add(indicator.Host, container);
                return container;
            }
            finally
            {
                _firefoxContainerSemaphoreSlim.Release();
            }
        }

        private void CleanUpWebDrivers(string uri)
        {
            _logger.LogInformation($"Firefox Cleanup requested due to: {uri}");
            _firefoxContainerSemaphoreSlim.Wait();
            try
            {
                if (!_activeDrivers.Any())
                    return;

                foreach (var driverKey in _activeDrivers.Keys)
                {
                    var driver = _activeDrivers[driverKey];

                    try
                    {
                        driver.Driver.Quit();
                        _activeDrivers.Remove(driverKey);
                        _logger.LogWarning($"Removed expired web driver: {driver.HostUri}");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Failed to Stop Web Driver: {driver.HostUri}");
                    }
                }
            }
            finally
            {
                _firefoxContainerSemaphoreSlim.Release();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_activeDrivers.Values.Any())
                    {
                        foreach (var driver in _activeDrivers.Values)
                        {
                            driver.Driver.Quit();
                            driver.Driver.Dispose();
                        }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
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
