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
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.UserActions;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using LanguageExt;

namespace Crawler.Configuration.Core
{
    public class CrawlerConfigurationService : ICrawlerConfigurationService
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly ISchedulerRepository _schedulerRepository;

        public CrawlerConfigurationService(IConfigurationRepository repo, ISchedulerRepository schedulerRepository)
        {
            _schedulerRepository = schedulerRepository;
            _configurationRepository = repo;
        }

        public TryOptionAsync<Unit> Add(Option<CrawlUriDataModel> crawlUri)
        {
            return _schedulerRepository.Add(crawlUri).Bind<Guid, Unit>(_ => async () => await Task.FromResult(Unit.Default));
        }

        public TryOptionAsync<Unit> Add(Option<UriDataModel> sourceData)
        {
            return _schedulerRepository.Add(sourceData).Bind<Guid, Unit>(_ => async () => await Task.FromResult(Unit.Default));
        }

        public TryOptionAsync<CrawlRequest> CreateRequest(Option<string> uri, Option<Guid> guid, Option<Guid> crawlId)
        {
            var correlationId = guid.Match(g => g, () => Guid.NewGuid());
            var cId = crawlId.Match(g => g, () => Guid.Empty);

            return async () =>
            {
                return await uri.ToTryOptionAsync()
                 .Bind(u => _configurationRepository.GetCrawlRequest(uri)).Match(r => r.Map(uri, correlationId, cId), () => 
                 CreateGenericCrawlRequest(uri, correlationId, cId), ex => throw ex);
            };
        }

        public TryOptionAsync<CrawlRequestModel> GetCollectorCrawlRequest(Option<string> uri)
        {
            return uri.ToTryOptionAsync().Bind((Func<string, TryOptionAsync<CrawlRequestModel>>)(u => async () =>
           {
               return await _configurationRepository.GetCollectorCrawlRequest(uri).Match(r => r,
               CreateDefaultCollectorRequest(u));
           }));
        }

        public TryOptionAsync<List<SourceDataModel>> GetCollectorSourceData()
        {
            return _schedulerRepository.GetCollectorSourceData();
        }

        public TryOptionAsync<DocumentPart> GetExpectedDocumentPart(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId)
        {
            return CreateRequest(uri, correlationId, crawlId)
            .Bind<CrawlRequest, DocumentPart>(r => async () => await Task.FromResult(r.RequestDocument.Bind(d => d.RequestDocumentPart)));
        }

        public TryOptionAsync<List<UiAction>> GetUiActions(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId)
        {
            return CreateRequest(uri, correlationId, crawlId)
            .Bind<CrawlRequest, List<UiAction>>(r => async () => await Task.FromResult(r.LoadPageRequest.Bind(p => p.UserActions)));
        }

        public TryOptionAsync<List<CrawlUriDataModel>> GetUnscheduledCrawlUriData()
        {
            return _schedulerRepository.GetUnscheduledCrawlUriData();
        }

        public TryOptionAsync<UriDataModel> GetUriData(Option<Guid> id)
        {
            return _schedulerRepository.GetUriData(id);
        }

        public TryOptionAsync<List<UriDataModel>> GetPeriodicUri()
        {
            return _schedulerRepository.GetPeriodicUriData();
        }

        public TryOptionAsync<Unit> StoreDetectedUrls(Option<List<DocumentPartLink>> links, Option<Guid> guid)
        {
            return links.ToTryOptionAsync().Bind<List<DocumentPartLink>, Unit>(list => async () =>
           {
               list.ForEach(async link =>
               {
                   await _schedulerRepository
                   .UriLinkExists(link.Uri)
                   .MatchAsync(_ =>
                       Task.CompletedTask,
                       async () =>
                       await _schedulerRepository.Add(new UriDataModel
                       {
                           UriTypeId = UriType.Onetime,
                           Uri = link.Uri.Match(u => u, () => throw new Exception("Uri can't be empty")),


                       })
                       .Match(r => r, () => throw new Exception("Failed to add link")));
               });

               return await Task.FromResult(Unit.Default);
           });
        }

        public TryOptionAsync<Unit> UpdateCompletedTimeUtcNow(Guid id)
        {
            return _schedulerRepository.UpdateCompletedTimeUtcNow(id);
        }

        public TryOptionAsync<Unit> UpdateScheduledTimeUtcNow(Guid id)
        {
            return _schedulerRepository.UpdateScheduledTimeUtcNow(id);
        }



        private CrawlRequest CreateGenericCrawlRequest(Option<string> uri, Guid correlationId, Guid crawlId)
        {
            return new CrawlRequest()
            {
                LoadPageRequest = new LoadPageRequest()
                {
                    CorrelationId = correlationId,
                    Uri = uri,
                    UserActions = new List<UiAction>()
                },
                CorrelationCrawlId = correlationId,
                RequestDocument = new Document
                {
                    RequestDocumentPart = new DocumentPartAutodetect()
                }
            };
        }

        private static CrawlRequestModel CreateDefaultCollectorRequest(string u)
        {
            return new CrawlRequestModel
            {
                Uri = u,
                Host = new Uri(u).Host,
                ContinuationStrategyDefinition = CrawlContinuationStrategy.TrackLinksOnly,
                DocumentPartDefinition = new DocumentPartLink
                {
                    Selector = new DocumentPartSelector
                    {
                        Xpath = "//a"
                    }
                },
                ShouldDownloadContent = false,
                IsUrlCollector = true
            };
        }

    }
}