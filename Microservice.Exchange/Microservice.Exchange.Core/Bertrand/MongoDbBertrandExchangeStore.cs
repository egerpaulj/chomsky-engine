using System;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Mongodb.Repo;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace Microservice.Exchange.Bertrand;

public class BertrandExchangeDataModel : IDataModel
{
    [JsonProperty("_id")]
    public Guid Id { get; set; }
    public string Created { get; set; }
    public string Updated { get; set; }
    public BertrandExchangeModel BertrandExchangeModel { get; set; }

    public string ExchangeName { get; set; }

    public static BertrandExchangeDataModel Map(BertrandExchangeModel model, Guid? id = null)
    {
        return new BertrandExchangeDataModel
        {
            BertrandExchangeModel = model,
            ExchangeName = model.ExchangeName,
            Id = id ?? Guid.Empty,
        };
    }
}

public class MongoDbBertrandExchangeStore(IMongoDbRepository<BertrandExchangeDataModel> repository)
    : IBertrandExchangeStore
{
    public TryOptionAsync<BertrandExchangeDataModel> GetExchange(string name)
    {
        return repository.Get(Builders<BsonDocument>.Filter.Eq("ExchangeName", name));
    }

    public TryOptionAsync<bool> IsConsumerActive(string exchangeName, string consumerName)
    {
        return GetExchange(exchangeName)
            .Bind<BertrandExchangeDataModel, bool>(exchangeModel =>
                async () =>
                {
                    var consumer = exchangeModel.BertrandExchangeModel.Consumers.FirstOrDefault(c =>
                        c.Name == consumerName
                    );
                    if (consumer == null)
                        return true;

                    return await Task.FromResult(consumer.IsActive);
                }
            );
    }

    public TryOptionAsync<Unit> SaveExchange(BertrandExchangeModel model)
    {
        return async () =>
        {
            var bertrandExchangeDataModel = await GetExchange(model.ExchangeName)
                .Match(
                    exchangeModel => BertrandExchangeDataModel.Map(model, exchangeModel.Id),
                    () => BertrandExchangeDataModel.Map(model),
                    ex => throw ex
                );

            return await repository
                .AddOrUpdate(bertrandExchangeDataModel)
                .Match(
                    _ => Unit.Default,
                    () => throw new Exception("Failed to save exchange"),
                    ex => throw ex
                );
        };
    }

    public TryOptionAsync<bool> IsPublisherActive(string exchangeName, string publisherName)
    {
        return GetExchange(exchangeName)
            .Bind<BertrandExchangeDataModel, bool>(exchangeModel =>
                async () =>
                {
                    var publisher = exchangeModel.BertrandExchangeModel.Publishers.FirstOrDefault(
                        c => c.Name == publisherName
                    );
                    if (publisher == null)
                        return true;

                    return await Task.FromResult(publisher.IsActive);
                }
            );
    }

    public TryOptionAsync<bool> IsTransformerActive(string exchangeName, string transformerName)
    {
        return GetExchange(exchangeName)
            .Bind<BertrandExchangeDataModel, bool>(exchangeModel =>
                async () =>
                {
                    var transformer =
                        exchangeModel.BertrandExchangeModel.Transformers.FirstOrDefault(c =>
                            c.Name == transformerName
                        );
                    if (transformer == null)
                        return true;

                    return await Task.FromResult(transformer.IsActive);
                }
            );
    }
}
