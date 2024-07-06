using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace Microservice.Exchange.Endpoints.Mongodb;

public class MongoDbBertrandStateStore(IJsonConverterProvider jsonConverterProvider, IMongoDbRepository<BertrandStateDataModel> mongoDbRepository, IMongoDbRepository<BertrandStateDataModel> deadletterRepository) : IBertrandStateStore
{
    private readonly IJsonConverterProvider jsonConverterProvider = jsonConverterProvider;
    private readonly IMongoDbRepository<BertrandStateDataModel> stateRepository = mongoDbRepository;
    private readonly IMongoDbRepository<BertrandStateDataModel> deadletterRepository = deadletterRepository;

    public TryOptionAsync<Unit> Delete(Option<Guid> id)
    {
        return stateRepository.Delete(id);
    }

    public TryOptionAsync<IEnumerable<Message<object>>> GetOutstandingMessages()
    {
        return stateRepository
            .GetMany(Builders<BsonDocument>.Filter.Empty)
            .Bind<List<BertrandStateDataModel>, IEnumerable<Message<object>>>(models => async () =>
            {
                return await Task.FromResult(
                    models
                                .Select(model =>
                                    new Message<object>
                                    {
                                        CorrelationId = model.CorrelationId,
                                        Id = model.Id,
                                        Properties = model.Properties,
                                        RoutingKey = model.RoutingKey,
                                        Payload = jsonConverterProvider.Deserialize(model.Payload, Type.GetType(model.AssemblyQualifiedTypeName))
                                    })
                                .ToList()
                );
            });
    }

    public TryOptionAsync<Unit> StoreIncomingMessage(Option<Message<object>> message)
    {
        return message.ToTryOptionAsync().Bind<Message<object>, Unit>(m => async () =>
        {
            await SaveMessageInRepo(m, stateRepository, jsonConverterProvider);

            return await Task.FromResult(Unit.Default);
        });
    }

    public TryOptionAsync<Unit> StoreInDeadletter(Option<Message<object>> message)
    {
        return message.ToTryOptionAsync().Bind<Message<object>, Unit>(m => async () =>
        {
            await SaveMessageInRepo(m, deadletterRepository, jsonConverterProvider);

            return await Task.FromResult(Unit.Default);
        });
    }

    private static async Task SaveMessageInRepo(Message<object> m, IMongoDbRepository<BertrandStateDataModel> repository, IJsonConverterProvider jsonConverterProvider)
    {
        var payload = m.Payload.Match(m => m, () => string.Empty);
        var dataModel = new BertrandStateDataModel
        {
            RoutingKey = m.RoutingKey,
            CorrelationId = m.CorrelationId,
            Id = m.Id.Match(i => i, () => Guid.NewGuid()),
            Properties = m.Properties,
            AssemblyQualifiedTypeName = payload.GetType().AssemblyQualifiedName,
            Payload = jsonConverterProvider.Serialize(payload)
        };

        await repository.AddOrUpdate(dataModel).Match(r => r, () => throw new Exception("Failed to store"), ex => throw new Exception("Failed to store", ex));
    }
}

public class BertrandStateDataModel : IDataModel
{
    [JsonProperty("_id")]
    public Guid Id { get; set; }
    public string Created { get; set; }
    public string Updated { get; set; }
    public string AssemblyQualifiedTypeName { get; set; }
    public string Payload { get; set; }
    public Option<string> RoutingKey { get; set; }
    public Option<Guid> CorrelationId { get; set; }
    public Option<List<KeyValuePair<string, string>>> Properties { get; set; }
}