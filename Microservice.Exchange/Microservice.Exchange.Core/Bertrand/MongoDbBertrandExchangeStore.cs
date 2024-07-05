using System;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Mongodb.Repo;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Linq;

namespace Microservice.Exchange.Bertrand;

public class BertrandExchangeDataModel : IDataModel
{
    
    public Guid Id { get; set; }
    public string Created { get; set; }
    public string Updated { get; set; }
    public BertrandExchangeModel BertrandExchangeModel { get; set; }
    
    [JsonProperty("_id")]
    public string ExchangeName { get; set; }
}

public class MongoDbBertrandExchangeStore(IMongoDbRepository<BertrandExchangeDataModel> repository) : IBertrandExchangeStore
{
    public TryOptionAsync<BertrandExchangeModel> GetExchange(string name)
    {
        return repository.Get(Builders<BsonDocument>.Filter.Eq("ExchangeName", name))
        .Bind<BertrandExchangeDataModel, BertrandExchangeModel>(
            dataModel => async () => await Task.FromResult(dataModel.BertrandExchangeModel));
    }

    public TryOptionAsync<bool> IsConsumerActive(string exchangeName, string consumerName)
    {
        return GetExchange(exchangeName).Bind<BertrandExchangeModel, bool>(
            exchangeModel => async () => 
            {
                var consumer = exchangeModel.Consumers.FirstOrDefault(c => c.Name == consumerName );
                if(consumer == null)
                    return true;

                return await Task.FromResult(consumer.IsActive);
            });
    }

    public TryOptionAsync<Unit> SaveExchange(BertrandExchangeModel model)
    {
        throw new NotImplementedException("ToDo");
    }
}