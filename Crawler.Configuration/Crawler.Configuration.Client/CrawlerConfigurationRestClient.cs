using System;
using System.Collections.Generic;
using System.Net.Http;
using Crawler.Configuration.Core;
using Crawler.Configuration.Core.Requests;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using Crawler.Core.UserActions;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using LanguageExt;
using Microservice.Core.Http;
using Microsoft.Extensions.Configuration;

namespace Crawler.Configuration.Client
{
    public class CrawlerConfigurationRestClient : ICrawlerConfigurationService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly string _crawlRequestUri;
        private readonly string _uiActionsUri;
        private readonly string _documentPartUri;
        private readonly string _getCollectorCrawlRequestUri;
        private readonly string _configUiActionsUri;
        private readonly string _configDocumentPartUri;
        private readonly string _getUnscheduledCrawlUriUri;
        private readonly string _getCollectorSourceDataUri;
        private readonly string _getPeriodicUriDataUri;
        private readonly string _getUriDataUri;
        private readonly string _saveCrawlUriDataUri;
        private readonly string _saveUriDataUri;
        private readonly string _updateCompletedTimeUri;
        private readonly string _updateScheduledTimeUri;
        private readonly string _storeLinksUri;

        public CrawlerConfigurationRestClient(IHttpClientService httpClientService, IConfiguration configuration)
        {
            _httpClientService = httpClientService;
            _crawlRequestUri = configuration.GetValue<string>(ICrawlerConfigurationService.CrawlRequestsUriKey);
            _uiActionsUri = configuration.GetValue<string>(ICrawlerConfigurationService.UiActionsUriKey);
            _documentPartUri = configuration.GetValue<string>(ICrawlerConfigurationService.DocumentPartUriKey);
            _getCollectorCrawlRequestUri = configuration.GetValue<string>(ICrawlerConfigurationService.GetCollectorCrawlRequestUri);
            _configUiActionsUri = configuration.GetValue<string>(ICrawlerConfigurationService.ConfigUiActionsUri);
            _configDocumentPartUri = configuration.GetValue<string>(ICrawlerConfigurationService.ConfigDocumentPartUri);
            _getUnscheduledCrawlUriUri = configuration.GetValue<string>(ICrawlerConfigurationService.GetUnscheduledCrawlUriUri);
            _getCollectorSourceDataUri = configuration.GetValue<string>(ICrawlerConfigurationService.GetCollectorSourceDataUri);
            _getPeriodicUriDataUri = configuration.GetValue<string>(ICrawlerConfigurationService.GetPeriodicUriDataUri);
            _getUriDataUri = configuration.GetValue<string>(ICrawlerConfigurationService.GetUriDataUri);
            _saveCrawlUriDataUri = configuration.GetValue<string>(ICrawlerConfigurationService.SaveCrawlUriDataUri);
            _saveUriDataUri = configuration.GetValue<string>(ICrawlerConfigurationService.SaveUriDataUri);
            _updateCompletedTimeUri = configuration.GetValue<string>(ICrawlerConfigurationService.UpdateCompletedTimeUri);
            _updateScheduledTimeUri = configuration.GetValue<string>(ICrawlerConfigurationService.UpdateScheduledTimeUri);
            _storeLinksUri = configuration.GetValue<string>(ICrawlerConfigurationService.StoreLinksUri);
        }

        public TryOptionAsync<Unit> Add(Option<CrawlUriDataModel> crawlUri)
        {
            return crawlUri
            .ToTryOptionAsync()
            .Bind(r => 
                _httpClientService.Send<CrawlUriDataModel>(Guid.NewGuid(), r, _saveCrawlUriDataUri, HttpMethod.Get));
        }

        public TryOptionAsync<Unit> Add(Option<UriDataModel> sourceData)
        {
            return sourceData
            .ToTryOptionAsync()
            .Bind(r => 
                _httpClientService.Send<UriDataModel>(Guid.NewGuid(), r, _saveUriDataUri, HttpMethod.Get));
        }

        public TryOptionAsync<CrawlRequest> CreateRequest(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId)
        {
            return _httpClientService.Send<ConfigurationCrawlRequest, CrawlRequest>( correlationId, new ConfigurationCrawlRequest{Uri = uri, CrawlId = crawlId}, _crawlRequestUri, HttpMethod.Get);
        }

        public TryOptionAsync<CrawlRequestModel> GetCollectorCrawlRequest(Option<string> uri)
        {
            return _httpClientService.Send<SingleValue, CrawlRequestModel>( Guid.NewGuid(), new SingleValue{Value = uri.Match(u => u, () => throw new Exception("Uri is empty"))}, _getCollectorCrawlRequestUri, HttpMethod.Get);
        }

        public TryOptionAsync<List<UriDataModel>> GetCollectorUri()
        {
            return _httpClientService.Send<SingleValue, List<UriDataModel>>( Guid.NewGuid(), new SingleValue(), _getCollectorSourceDataUri, HttpMethod.Get);
        }


        public TryOptionAsync<DocumentPart> GetExpectedDocumentPart(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId)
        {
            return _httpClientService.Send<ConfigurationCrawlRequest, DocumentPart>( correlationId, new ConfigurationCrawlRequest{CrawlId = crawlId, Uri = uri}, _documentPartUri, HttpMethod.Get);
        }

        public TryOptionAsync<List<UriDataModel>> GetPeriodicUri()
        {
            return _httpClientService.Send<SingleValue, List<UriDataModel>>( Guid.NewGuid(), new SingleValue(), _getPeriodicUriDataUri, HttpMethod.Get);
        }

        public TryOptionAsync<List<UiAction>> GetUiActions(Option<string> uri, Option<Guid> correlationId, Option<Guid> crawlId)
        {
            return _httpClientService.Send<ConfigurationCrawlRequest, List<UiAction>>( correlationId, new ConfigurationCrawlRequest{CrawlId = crawlId, Uri = uri}, _uiActionsUri, HttpMethod.Get);
        }

        public TryOptionAsync<List<CrawlUriDataModel>> GetUnscheduledCrawlUriData()
        {
            return _httpClientService.Send<SingleValue, List<CrawlUriDataModel>>( Guid.NewGuid(), new SingleValue(), _getUnscheduledCrawlUriUri, HttpMethod.Get);
        }

        public TryOptionAsync<UriDataModel> GetUriData(Option<Guid> id)
        {
            return _httpClientService.Send<SingleValue, UriDataModel>( Guid.NewGuid(), new SingleValue{Value = id.ToString()}, _getUnscheduledCrawlUriUri, HttpMethod.Get);
        }

        public TryOptionAsync<Unit> StoreDetectedUrls(Option<List<DocumentPartLink>> links, Option<Guid> correlationId)
        {
            return _httpClientService.Send<ConfigurationRestStoreLinks>( Guid.NewGuid(), new ConfigurationRestStoreLinks{Links = links}, _storeLinksUri, HttpMethod.Get);
        }

        public TryOptionAsync<Unit> UpdateCompletedTimeUtcNow(Guid id)
        {
            return _httpClientService.Send<SingleValue>( Guid.NewGuid(), new SingleValue{Value = id.ToString()}, _updateCompletedTimeUri, HttpMethod.Get);
        }

        public TryOptionAsync<Unit> UpdateScheduledTimeUtcNow(Guid id)
        {
            return _httpClientService.Send<SingleValue>( Guid.NewGuid(), new SingleValue{Value = id.ToString()}, _updateScheduledTimeUri, HttpMethod.Get);
        }
    }
}