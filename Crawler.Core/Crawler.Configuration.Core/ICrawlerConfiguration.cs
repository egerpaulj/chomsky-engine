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
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.UserActions;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using LanguageExt;

namespace Crawler.Configuration.Core
{
    public interface ICrawlerConfigurationService
    {
        public const string CrawlRequestsUriKey = "ConfigCrawlRequestUri";
        public const string UiActionsUriKey = "ConfigUiActionsUri";
        public const string DocumentPartUriKey = "ConfigDocumentPartUri";
        public const string GetCollectorCrawlRequestUri = "GetCollectorCrawlRequestUri";
        public const string ConfigUiActionsUri = "ConfigUiActionsUri";
        public const string ConfigDocumentPartUri = "ConfigDocumentPartUri";
        public const string GetUnscheduledCrawlUriUri = "GetUnscheduledCrawlUriUri";
        public const string GetCollectorSourceDataUri = "GetCollectorSourceDataUri";
        public const string GetPeriodicUriDataUri = "GetPeriodicUriDataUri";
        public const string GetUriDataUri = "GetUriDataUri";
        public const string SaveCrawlUriDataUri = "SaveCrawlUriDataUri";
        public const string SaveUriDataUri = "SaveUriDataUri";
        public const string UpdateCompletedTimeUri = "UpdateCompletedTimeUri";
        public const string UpdateScheduledTimeUri = "UpdateScheduledTimeUri";
        public const string StoreLinksUri = "StoreLinksUri";

        TryOptionAsync<List<UiAction>> GetUiActions(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId);
        TryOptionAsync<DocumentPart> GetExpectedDocumentPart(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId);
        TryOptionAsync<CrawlRequest> CreateRequest(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId);
        TryOptionAsync<CrawlRequestModel> GetCollectorCrawlRequest(Option<string> uri);
        
        TryOptionAsync<UriDataModel> GetUriData(Option<Guid> id);

        TryOptionAsync<Unit> StoreDetectedUrls(Option<List<DocumentPartLink>> links, Option<Guid> correlationId);

        TryOptionAsync<List<SourceDataModel>> GetCollectorSourceData();
        TryOptionAsync<List<CrawlUriDataModel>> GetUnscheduledCrawlUriData();
        TryOptionAsync<List<UriDataModel>> GetPeriodicUri();
        TryOptionAsync<Unit> Add(Option<CrawlUriDataModel> crawlUri);
        TryOptionAsync<Unit> Add(Option<UriDataModel> sourceData);
        TryOptionAsync<Unit> UpdateScheduledTimeUtcNow(Guid id);
        TryOptionAsync<Unit> UpdateCompletedTimeUtcNow(Guid id);
    }

}