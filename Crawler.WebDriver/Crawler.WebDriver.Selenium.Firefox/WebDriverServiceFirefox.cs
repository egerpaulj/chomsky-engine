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
using System.Linq;
using System.Xml.Linq;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Core.Exceptions;
using LanguageExt;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System.Threading.Tasks;
using OpenQA.Selenium;
using System.Text;
using Crawler.WebDriver.Selenium.UserActions;
using System.Net;
using Crawler.Core.Requests;
using Crawler.Core.UserActions;
using Crawler.Core.Parser.DocumentParts;
using Microsoft.Extensions.Logging;
using System.Xml;
using System.IO;
using System.Threading;
using Crawler.WebDriver.Selenium.Firefox.UserActions;
using Crawler.Core;

namespace Crawler.WebDriver.Selenium.Firefox
{
    public class WebDriverServiceFirefox : IWebDriverService
    {
        private const int PageLoadWaitTimeoutInSeconds = 10;
        private readonly IWebDriverMetrics _webDriverMetrics;
        private readonly ILogger<WebDriverServiceFirefox> _logger;
        private readonly Dictionary<string, FirefoxContainer> _activeDrivers = new Dictionary<string, FirefoxContainer>();

        private readonly SemaphoreSlim _firefoxContainerSemaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly Timer _cleanupTimer;
        private const int _cleanupPeriodInSeconds = 300;
        private const int _maxDriverLifetimeInMinutes = 30;
        private readonly bool _isHeadless;
        private bool disposedValue;

        public WebDriverServiceFirefox(IWebDriverMetrics metrics, ILogger<WebDriverServiceFirefox> logger)
        : this(metrics, logger, true)
        {
        }

        public WebDriverServiceFirefox(IWebDriverMetrics metrics, ILogger<WebDriverServiceFirefox> logger, bool isHeadless)
        {
            _logger = logger;
            _webDriverMetrics = metrics;
            _cleanupTimer = new Timer(CleanUpWebDrivers, this, 0, _cleanupPeriodInSeconds * 1000);
            _isHeadless = isHeadless;
        }

        public TryOptionAsync<FileData> Download(Option<DownloadRequest> downloadRequest)
        {
            var uri = downloadRequest.Bind(r => r.Uri).Match(r => r, () => throw new DownloadException("Uri is empty"));
            var correlationId = downloadRequest.Bind(r => r.CorrelationId).Match(c => c, () => Guid.NewGuid());

            return DownloadBytes(uri, correlationId)
                    .Bind(result =>
                        CreateFileData(result, uri));
        }

        public TryOptionAsync<string> LoadPage(Option<LoadPageRequest> request)
        {

            var requestEither = request.ToEitherAsync(new PageLoadException(null, "Request is empty"));
            var uriEither = requestEither.Bind<string>(req => req.Uri.ToEitherAsync<PageLoadException, string>(new PageLoadException(null, "Url is empty")));
            var correlationId = request.Bind(r => r.CorrelationId).Match(c => c, () => Guid.NewGuid());

            var userActions = request
                .Bind(r => r.UserActions)
                .Match(s => s, () => new List<UiAction>())
                .Select(Map);

            return uriEither
                .ToTryOption()
                .SelectMany(uri =>
                    CreateDriver(uri)
                        .SelectMany(driver =>
                            LoadPage(driver, uri, correlationId), (d, d2) => d2), (u, d) => d)
                .SelectMany(driver => ExecuteUserActions(driver, userActions, correlationId), (d, _) => d)
                .Bind<FirefoxContainer, string>(driver => async () => await Task.FromResult(driver.Driver.PageSource));
        }

        private static TryOptionAsync<FileData> CreateFileData(string binaryData, Option<string> uri)
        {
            return async () =>
            {
                var uriLocalPath = new Uri(uri.Match(s => s, () => throw new DownloadException("Uri is empty"))).LocalPath;
                var fileName = System.IO.Path.GetFileName(uriLocalPath);

                return await Task.FromResult(new FileData()
                {
                    Name = fileName,
                    Uri = uri,
                    Data = binaryData
                });
            };
        }

        private TryOptionAsync<string> DownloadBytes(string uri, Guid correlationId)
        {
            return async () =>
            {

                var client = new WebClient();
                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

                _logger.LogInformation($"Downloading: {uri}. CorrelationId: {correlationId}");
                var data = client.DownloadData(uri);
                _logger.LogInformation($"Successfully Downloaded: {uri}. CorrelationId: {correlationId}");
                _webDriverMetrics.IncDownload(new Uri(uri).Host);

                return await Task.FromResult(Encoding.GetEncoding("iso-8859-1").GetString(data));
            };
        }

        private static UserAction Map(UiAction uiAction)
        {
            switch (uiAction.Type)
            {
                case UiAction.ActionType.Click:
                    return new UserActionClick()
                    {
                        XPath = uiAction.XPath
                    };

                case UiAction.ActionType.Checkbox:
                    return new UserActionSelected()
                    {
                        Target = bool.Parse(uiAction.ActionData.Match(v => v, () => "false")),
                        XPath = uiAction.XPath
                    };
                case UiAction.ActionType.Dropdown:
                    return new UserActionSelectDropdown()
                    {
                        Target = uiAction.ActionData,
                        XPath = uiAction.XPath
                    };
                case UiAction.ActionType.Input:
                    return new UserActionInput()
                    {
                        Target = uiAction.ActionData,
                        XPath = uiAction.XPath
                    };
                case UiAction.ActionType.List:
                    return new UserActionSelectDropdown()
                    {
                        Target = uiAction.ActionData,
                        XPath = uiAction.XPath
                    };
                case UiAction.ActionType.Radio:
                    return new UserActionSelected()
                    {
                        Target = bool.Parse(uiAction.ActionData.Match(v => v, () => "false")),
                        XPath = uiAction.XPath
                    };
                case UiAction.ActionType.Wait:
                    return new UserActionWait()
                    {
                        XPath = uiAction.XPath
                    };

                default:
                    throw new PageLoadException(null, "Unknown Ui Action");
            }
        }

        private TryOptionAsync<FirefoxContainer> CreateDriver(string uri)
        {
            return async () =>
            {
                try
                {
                    await _firefoxContainerSemaphoreSlim.WaitAsync();

                    var indicator = new Uri(uri);

                    if (_activeDrivers.ContainsKey(indicator.Host))
                        return _activeDrivers[indicator.Host];
                    FirefoxContainer container = CreateFirefoxContainer(indicator);
                    _activeDrivers.Add(indicator.Host, container);

                    return container;
                }
                finally
                {
                    _firefoxContainerSemaphoreSlim.Release();
                }
            };
        }

        private FirefoxContainer CreateFirefoxContainer(Uri indicator)
        {
            var options = new FirefoxOptions();
            if (_isHeadless)
                options.AddArgument("-headless");
            var container = new FirefoxContainer(new FirefoxDriver(options), indicator.Host);
            return container;
        }

        private TryOptionAsync<FirefoxContainer> LoadPage(FirefoxContainer container, string uri, Guid correlationId)
        {
            return async () =>
            {
                _logger.LogInformation($"Loading Page: {uri}. CorrelationId: {correlationId}");
                var c = LoadPage(container, uri);
                _logger.LogInformation($"Loaded Page successfully: {uri}. CorrelationId: {correlationId}");
                _webDriverMetrics.IncPageLoad(new Uri(uri).Host);

                return await c;
            };
        }

        private TryOptionAsync<Unit> ExecuteUserActions(FirefoxContainer container, IEnumerable<UserAction> userActions, Guid correlationId)
        {
            return async () =>
            {

                foreach (var userAction in userActions)
                {
                    _logger.LogInformation($"Executing Page User Action: {userAction}. CorrelationId: {correlationId}");
                    await userAction.Execute(container.Driver).Match(r => r, () => throw new CrawlException("Failed to run user actions", ErrorType.PageLoadError), ex => throw new CrawlException("Failed to run user actions", ErrorType.PageLoadError, ex));
                    _logger.LogInformation($"Successfully executed Page User Action: {userAction}. CorrelationId: {correlationId}");
                }

                return await Task.FromResult(Unit.Default);
            };
        }

        private async Task<FirefoxContainer> LoadPage(FirefoxContainer container, string uri)
        {
            try
            {
                container.Driver.Navigate().GoToUrl(uri);

                var wait = new WebDriverWait(container.Driver, TimeSpan.FromSeconds(10));
                wait.PollingInterval = TimeSpan.FromMilliseconds(300);
                await Task.Delay(3);
                wait.Until(d => container.Driver.ExecuteScript("return document.readyState").Equals("complete"));

                return container;
            }
            catch (Exception e)
            {
                container.Discard();
                _logger.LogWarning($"Webdriver Error. Replacing driver and trying again. {e.Message}");
                var tempContainer = CreateFirefoxContainer(new Uri(uri));
                tempContainer.Driver.Navigate().GoToUrl(uri);
                return tempContainer;

            }

        }

        private void CleanUpWebDrivers(object state)
        {
            _firefoxContainerSemaphoreSlim.Wait();

            try
            {
                _logger.LogInformation("Started Web Driver Cleanup");

                var timeNow = DateTime.UtcNow;

                foreach (var driverKey in _activeDrivers.Keys)
                {
                    var driver = _activeDrivers[driverKey];

                    if ((timeNow - driver.CreatedTimeUtc).TotalMinutes >= _maxDriverLifetimeInMinutes || driver.MarkedForRemoval)
                    {
                        try
                        {
                            driver.Driver.Close();
                            driver.Driver.Dispose();
                            _activeDrivers.Remove(driverKey);
                            _logger.LogWarning($"Removing expired web driver: {driver.HostUri}");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"Failed to Stop Web Driver: {driver.HostUri}");
                        }
                    }
                }
            }
            finally
            {
                _firefoxContainerSemaphoreSlim.Release();
                _logger.LogInformation("Finished Web Driver Cleanup");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cleanupTimer.Dispose();

                    if (_activeDrivers.Values.Any())
                    {
                        foreach (var driver in _activeDrivers.Values)
                        {
                            driver.Driver.Close();
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