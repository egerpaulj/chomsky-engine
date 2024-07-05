using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Core;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.UserActions;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json;
using Crawler.Core.Parser.DocumentParts.Serialilzation;
using Crawler.DataModel;
using Microservice.Mongodb.Repo;
using System.Text.RegularExpressions;

namespace Crawler.Configuration.Repository
{
    public class MongoDbConfigurationRepository : IConfigurationRepository
    {

        private readonly IMongoDbRepository<CrawlRequestModel> _mongoDocumentRepository;


        public MongoDbConfigurationRepository(IMongoDbRepository<CrawlRequestModel> mongoDocumentRepository)
        {
            _mongoDocumentRepository = mongoDocumentRepository;
        }

        public TryOptionAsync<Guid> AddOrUpdate(Option<CrawlRequestModel> crawlRequestModel)
        {
            return _mongoDocumentRepository.AddOrUpdate(crawlRequestModel);
        }


        public TryOptionAsync<CrawlRequestModel> GetCrawlRequest(Option<string> uri)
        {
            return uri
            .ToTryOptionAsync()
            .Bind(u => GetUriFilter(u))
            .Bind(filter => _mongoDocumentRepository.Get(filter));
        }

        public TryOptionAsync<CrawlRequestModel> GetCollectorCrawlRequest(Option<string> uri)
        {
            return uri
            .ToTryOptionAsync()
            .Bind(u => GetUriFilter(u, isCollector: true))
            .Bind(filter => _mongoDocumentRepository.Get(filter));
        }


        public TryOptionAsync<List<UiAction>> GetUserActions(Option<string> uri)
        {
            return 
            GetCrawlRequest(uri)
            .Bind<CrawlRequestModel, List<UiAction>>(doc => async () => await Task.FromResult(doc.UiActions));
        }

        public TryOptionAsync<DocumentPart> GetDocumentPart(Option<string> uri)
        {
            return 
            GetCrawlRequest(uri)
            .Bind<CrawlRequestModel, DocumentPart>(doc => async () => await Task.FromResult(doc.DocumentPartDefinition));
        }

        public TryOptionAsync<Unit> DeleteAll(Option<string> uri)
        {
            return uri
            .ToTryOptionAsync()
            .Bind(u => GetUriFilter(u))
            .Bind(f => _mongoDocumentRepository.Delete(f));
            
        }

        public TryOptionAsync<bool> IsCollectable(Option<string> uri)
        {
            return uri
            .ToTryOptionAsync()
            .SelectMany(u => GetUriFilter(u), async  (u, filter) => 
            {
                return await _mongoDocumentRepository
                            .Get(filter)
                            .Match(model => 
                                new Regex(model.CollectablePattern).IsMatch(u), 
                                () => false, 
                                ex => throw ex);
            });
        }

        public TryOptionAsync<bool> ShouldSkip(Option<string> baseUri, Option<string> uri)
        {
            return baseUri
            .ToTryOptionAsync()
            .SelectMany(buri => GetUriFilter(buri, isCollector: true), async  (buri, filter) => 
            {
                var u = uri.Match(r => r, string.Empty);
                return await _mongoDocumentRepository
                            .Get(filter)
                            .Match(model => 
                                model.UrlSkipList?.Any(item => u.ToLower().Contains(item.ToLower())) ?? true, 
                                () => false, 
                                ex => throw ex);
            });
        }

        private static TryOptionAsync<FilterDefinition<BsonDocument>> GetUriFilter(string uri, bool isCollector = false)
        {
            return async () =>
            {
                var host = new Uri(uri).Host;
                var filter = Builders<BsonDocument>.Filter.Eq("Host", host.ToLowerInvariant());
                filter &= (Builders<BsonDocument>.Filter.Eq("Uri", uri.ToLowerInvariant()) | Builders<BsonDocument>.Filter.Eq("Uri", CrawlRequestModel.AllUriMatch));
                filter &= Builders<BsonDocument>.Filter.Eq<Boolean>("IsUrlCollector", isCollector);
                
                return await Task.FromResult(filter);
            };
        }
    }
}