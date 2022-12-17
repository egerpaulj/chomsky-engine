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
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Core.Metrics;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.Strategy;
using Crawler.Core.UserActions;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.WebDriver.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Crawler.Core.Management
{
    public class CrawlerConfigurationGeneric : ICrawlerConfigurationService
    {
        private readonly IWebDriverService _driver;
        private readonly IMetricRegister _metricRegister;

        private readonly ILogger<CrawlerConfigurationGeneric> _logger;

        public CrawlerConfigurationGeneric(IWebDriverService driver, IMetricRegister metricRegister, ILogger<CrawlerConfigurationGeneric> logger)
        {
            _driver = driver;
            _metricRegister = metricRegister;
            _logger = logger;
        }

        public TryOptionAsync<Unit> Add(Option<CrawlUriDataModel> crawlUri)
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<Unit> Add(Option<UriDataModel> sourceData)
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<Request> CreateRequest(Option<CrawlRequest> crawlRequest, Option<Guid> guid)
        {
            return async () =>
            {
                return await Task.FromResult(new Request(new CrawlerStrategyGeneric(_driver, _metricRegister), null, crawlRequest));
            };
        }

        public TryOptionAsync<CrawlRequest> CreateRequest(Option<string> uri, Option<Guid> guid)
        {
            return async () =>
            {
                var corrId = Guid.NewGuid();
                return await Task.FromResult(new CrawlRequest()
                {
                    LoadPageRequest = new LoadPageRequest()
                    {
                        CorrelationId = corrId,
                        Uri = uri,
                        UserActions = new List<UiAction>(),
                    },
                    CorrelationCrawlId = corrId,
                    RequestDocument = new Document
                    {
                        RequestDocumentPart = new DocumentPartAutodetect(),
                        DownloadContent = true,
                    }
                });
            };
        }

        public TryOptionAsync<CrawlRequest> CreateRequest(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId)
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<CrawlRequestModel> GetCollectorCrawlRequest(Option<string> uri)
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<List<SourceDataModel>> GetCollectorSourceData()
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<DocumentPart> GetExpectedDocumentPart(Option<string> uri, Option<Guid> guid)
        {
            return async () => await Task.FromResult(new DocumentPartAutodetect());
        }

        public TryOptionAsync<DocumentPart> GetExpectedDocumentPart(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId)
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<List<UriDataModel>> GetPeriodicUri()
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<List<UiAction>> GetUiActions(Option<string> uri, Option<Guid> guid)
        {
            return async () => await Task.FromResult(new List<UiAction>());
        }

        public TryOptionAsync<List<UiAction>> GetUiActions(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId)
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<List<CrawlUriDataModel>> GetUnscheduledCrawlUriData()
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<UriDataModel> GetUriData(Option<string> uri)
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<UriDataModel> GetUriData(Option<Guid> id)
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<Unit> StoreDetectedUrls(Option<List<DocumentPartLink>> links, Option<Guid> guid)
        {
            return async () =>
            {
                links.Match(r =>
                {
                    foreach (var link in r)
                    {
                        var uri = link.Uri.Match(l => l, string.Empty);
                        var text = link.Text.Match(l => l, string.Empty);
                        
                        _logger.LogInformation($"Found hyper link on website: {uri} with text: {text}");
                    }
                }, () => { });
                return await Task.FromResult(Unit.Default);
            };
        }

        public TryOptionAsync<Unit> UpdateCompletedTimeUtcNow(Guid uriId)
        {
            throw new NotImplementedException();
        }

        public TryOptionAsync<Unit> UpdateScheduledTimeUtcNow(Guid uriId)
        {
            throw new NotImplementedException();
        }
    }
}