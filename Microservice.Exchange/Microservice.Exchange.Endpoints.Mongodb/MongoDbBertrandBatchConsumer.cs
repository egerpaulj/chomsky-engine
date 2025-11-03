//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2025  Paul Eger

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
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.DataModel.Core;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Mongodb.Repo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microservice.Exchange.Endpoints.Mongodb;

public class MongoDbBertrandBatchConsumer<T>(
    string name,
    ILogger<MongoDbBertrandBatchConsumer<T>> logger,
    IMongoDbRepository<T> repository,
    FilterDefinition<BsonDocument> queryFilterDefinition,
    string routingKey,
    int batchSize = 0
) : IBertrandConsumer
    where T : IDataModel
{
    private CancellationTokenSource cancellationTokenSource;

    public string Name => name;

    public TryOptionAsync<Unit> End()
    {
        return async () =>
        {
            await cancellationTokenSource.CancelAsync();

            return Unit.Default;
        };
    }

    public TryOptionAsync<Unit> Start(IBertrandMessageHandler messageHandler)
    {
#pragma warning disable CS4014
#pragma warning disable CS1998

        return async () =>
        {
            RunBatchProcessing(messageHandler);
            return Unit.Default;
        };
    }

    private async Task RunBatchProcessing(IBertrandMessageHandler messageHandler)
    {
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = new CancellationTokenSource();

        await foreach (
            var result in repository.GetBatches(
                queryFilterDefinition,
                cancellationTokenSource.Token,
                batchSize
            )
        )
        {
            var id = (result as IDataModel)?.Id ?? Guid.NewGuid();
            var correlationId = id;

            if (result is IMessage message)
            {
                id = message.Id.Match(i => i, () => id);
                correlationId = message.CorrelationId.Match(i => i, () => correlationId);
                routingKey = message.RoutingKey.Match(r => r, () => string.Empty);
            }

            await messageHandler
                .Handle(
                    new Message<object>
                    {
                        Payload = result,
                        Id = id,
                        CorrelationId = correlationId,
                        RoutingKey = routingKey,
                    }
                )
                .Match(r => { }, () => { }, ex => { });
        }
    }
}
