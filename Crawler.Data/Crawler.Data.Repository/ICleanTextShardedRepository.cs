using Microservice.Mongodb.Repo;

namespace Crawler.Data.Repository;

public interface ICleanTextShardedRepository : IMongoDbAddOrUpdate<DataPipelineCleanText> { }
