using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Crawler.DataModel.Scheduler;
using LanguageExt;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Crawler.Scheduler.Repository
{
    public class SchedulerRepository : ISchedulerRepository
    {
        private const string DateStrFormat = "yyyy-MM-dd-HH:mm:ss.fff";
        IMongoDbRepository<CrawlUriDataModel> _crawlUriRepository;
        IMongoDbRepository<UriDataModel> _uriDataRepository;

        public SchedulerRepository(IConfiguration configuration, IJsonConverterProvider jsonConverterProvider)
        {
            _crawlUriRepository = new MongoDbRepository<CrawlUriDataModel>(configuration, new CrawlUriDataConfiguration(), jsonConverterProvider);
            _uriDataRepository = new MongoDbRepository<UriDataModel>(configuration, new UriDataConfiguration(), jsonConverterProvider);
        }

        public TryOptionAsync<List<UriDataModel>> GetPeriodicUriData()
        {
            return GetPeriodicUriDataFilter().Bind(filter => _uriDataRepository.GetMany(filter));
        }

        public TryOptionAsync<UriDataModel> GetUriData(Option<string> uri)
        {
            return uri
            .ToTryOptionAsync()
            .Bind(u => GetUriFilter(u))
            .Bind(filter => _uriDataRepository.Get(filter));
        }

        public TryOptionAsync<Guid> AddOrUpdate(Option<CrawlUriDataModel> model)
        {
            return model
            .ToTryOptionAsync()
            .SelectMany(m => _uriDataRepository.Get(m.UriId), (m, _) => m)
            .Bind(m => _crawlUriRepository.AddOrUpdate(m));
        }

        public TryOptionAsync<Guid> AddOrUpdate(Option<UriDataModel> model)
        {
            return model.ToTryOptionAsync().Bind<UriDataModel, Guid>(m => async () =>
           {
               if (!Uri.TryCreate(m.Uri, UriKind.Absolute, out var uri))
                   throw new Exception($"Failed to add Bad Uri: {m.Uri}");

               return await _uriDataRepository.AddOrUpdate(m).Match(g => g, () => throw new Exception("Failed to add Uri Data model"));
           });
        }

        public TryOptionAsync<List<CrawlUriDataModel>> GetUnscheduledCrawlUriData(int limit = 200)
        {
            return GetUnscheduledCrawlFilter().Bind(filter => _crawlUriRepository.GetMany(filter, limit));
        }

        public TryOptionAsync<List<UriDataModel>> GetCollectorUriData()
        {
            return GetPeriodicCollectorUriFilter().Bind(filter => _uriDataRepository.GetMany(filter));
        }

        public TryOptionAsync<Unit> UpdateCompletedTimeUtcNow(Guid id)
        {
            return _crawlUriRepository.Get(id).Bind(m =>
            {
                m.CompletedTimestamp = DateTime.UtcNow.ToString(DateStrFormat);
                return _crawlUriRepository.AddOrUpdate(m);
            }).Bind<Guid, Unit>(_ => async () => await Task.FromResult(Unit.Default));
        }

        public TryOptionAsync<Unit> UpdateScheduledTimeUtcNow(Guid id)
        {
            return _crawlUriRepository.Get(id).Bind(m =>
            {
                m.ScheduledTimestamp = DateTime.UtcNow.ToString(DateStrFormat);
                return _crawlUriRepository.AddOrUpdate(m);
            }).Bind<Guid, Unit>(_ => async () => await Task.FromResult(Unit.Default));
        }

        public TryOptionAsync<bool> UriLinkExists(Option<string> uri)
        {
            return GetUriData(uri)
            .Bind<UriDataModel, bool>(r => async () => await Task.FromResult(true));
        }

        public TryOptionAsync<UriDataModel> GetUriData(Option<Guid> id)
        {
            return _uriDataRepository.Get(id);
        }

        public TryOptionAsync<List<UriDataModel>> GetUriFoundList(int limit = 100)
        {
            return GetUriFoundFilter().Bind(filter => _uriDataRepository.GetMany(filter, limit));
        }

        public TryOptionAsync<List<UriDataModel>> GetIncompleteOnetimeUris(int limit = 100)
        {
            return GetOnetimeFilter().Bind(filter => _uriDataRepository.GetMany(filter, limit));
        }

        public TryOptionAsync<List<UriDataModel>> GetNewCollectorUris(int limit = 10)
        {
            return _uriDataRepository.GetMany(GetCollectorWithoutCronFilter(), limit);
        }

        private static TryOptionAsync<FilterDefinition<BsonDocument>> GetUriFilter(string uri)
        {
            return async () =>
            {
                var filter = Builders<BsonDocument>.Filter.Eq("Uri", uri);

                return await Task.FromResult(filter);
            };
        }

        private static TryOptionAsync<FilterDefinition<BsonDocument>> GetPeriodicUriDataFilter()
        {
            return async () =>
            {
                var filter = Builders<BsonDocument>.Filter.Eq("UriTypeId", (int)UriType.Periodic);
                filter &= (Builders<BsonDocument>.Filter.Exists("CronPeriod"));

                return await Task.FromResult(filter);
            };
        }

        private static TryOptionAsync<FilterDefinition<BsonDocument>> GetPeriodicCollectorUriFilter()
        {
            return async () =>
            {
                var filter = Builders<BsonDocument>.Filter.Eq("UriTypeId", (int)UriType.Collector);
                filter &= (Builders<BsonDocument>.Filter.Ne("CronPeriod", BsonNull.Value));

                return await Task.FromResult(filter);
            };
        }

        private static TryOptionAsync<FilterDefinition<BsonDocument>> GetUriFoundFilter()
        {
            return async () =>
            {
                var filter = Builders<BsonDocument>.Filter.Eq("UriTypeId", (int)UriType.Found);
                filter &= (Builders<BsonDocument>.Filter.Eq("IsCompleted", false));

                return await Task.FromResult(filter);
            };
        }

        private static TryOptionAsync<FilterDefinition<BsonDocument>> GetOnetimeFilter()
        {
            return async () =>
            {
                var filter = Builders<BsonDocument>.Filter.Eq("UriTypeId", (int)UriType.Onetime);
                filter &= (Builders<BsonDocument>.Filter.Eq("IsCompleted", false));

                return await Task.FromResult(filter);
            };
        }

        private static TryOptionAsync<FilterDefinition<BsonDocument>> GetUnscheduledCrawlFilter()
        {
            return async () =>
            {
                var filter = Builders<BsonDocument>.Filter.Eq("ScheduledTimestamp", BsonNull.Value);

                return await Task.FromResult(filter);
            };
        }

        private static FilterDefinition<BsonDocument> GetSourceFilter(Uri uri)
        {
            return Builders<BsonDocument>.Filter.Eq("Name", uri.Host.ToLowerInvariant());
        }

        private static FilterDefinition<BsonDocument> GetCollectorWithoutCronFilter()
        {
            var filter = Builders<BsonDocument>.Filter.Eq("UriTypeId", (int)UriType.Collector);
            filter &= Builders<BsonDocument>.Filter.Eq("CronPeriod", BsonNull.Value);
            filter &= Builders<BsonDocument>.Filter.Eq("IsCompleted", false);

            return filter;
        }
    }

    internal class CrawlUriDataConfiguration : IDatabaseConfiguration
    {
        public string DatabaseName => "Crawler";

        public string CollectionName => "crawl_uri";
    }

    internal class UriDataConfiguration : IDatabaseConfiguration
    {
        public string DatabaseName => "Crawler";

        public string CollectionName => "uri";
    }

    internal class SourceDataConfiguration : IDatabaseConfiguration
    {
        public string DatabaseName => "Crawler";

        public string CollectionName => "source_data";
    }
}