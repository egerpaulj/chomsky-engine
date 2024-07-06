//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2024  Paul Eger                                                                                                                                                                     

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
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microservice.Exchange.Bertrand;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Core.Bertrand;

public class BertrandExchange(
        string exchangeName,
        List<IBertrandConsumer> consumers,
        List<IBertrandTransformer> transformers,
        List<IBetrandTransformerFilter> transformerFilters,
        List<IBertrandPublisherFilter> publisherFilters,
        List<IPublisher<object>> publishers,
        ILogger<BertrandExchange> logger,
        IBertrandMetrics metrics,
        IBertrandStateStore bertrandStateStore,
        IBertrandExchangeStore bertrandExchangeStore,
        IBertrandExchangeManager bertrandExchangeManager) : IBertrandExchange, IBertrandMessageHandler
{
    private readonly List<IBertrandConsumer> _consumers = consumers ?? [];
    private readonly List<IBertrandTransformer> _transformers = transformers ?? [];
    private readonly List<IBetrandTransformerFilter> transformerFilters = transformerFilters ?? [];
    private readonly List<IBertrandPublisherFilter> _publisherFilters = publisherFilters ?? [];
    private readonly List<IPublisher<object>> _publishers = publishers ?? [];
    private readonly ILogger<BertrandExchange> logger = logger;
    private readonly IBertrandMetrics _metrics = metrics;
    private readonly IBertrandStateStore bertrandStateStore = bertrandStateStore;
    private readonly IBertrandExchangeStore bertrandExchangeStore = bertrandExchangeStore;

    public string ExchangeName { get; } = exchangeName;

    public IReadOnlyList<IBertrandConsumer> GetConsumers() => _consumers;
    public IReadOnlyList<IBertrandTransformer> GetTransformers() => _transformers;

    public IReadOnlyList<IPublisher<object>> GetPublishers() => _publishers;
    public IReadOnlyList<IBertrandPublisherFilter> GetPublisherFilters() => _publisherFilters;
    public IReadOnlyList<IBetrandTransformerFilter> GetTransformerFilters() => transformerFilters;

    public TryOptionAsync<Unit> End()
    {
        return LogInformation("Stopping Exchange")
                .Bind(StopConsumers)
                .Bind(_ => LogInformation("Stopped exchange successfully"));
    }

    public TryOptionAsync<Unit> Start()
    {
        return LogInformation("Starting Exchange")
                .Bind(RegisterExchange)
                .Bind(ProcessOutstandingWork)
                .Bind(StartConsumers)
                .Bind(_ => LogInformation("Started exchange successfully"));
    }

    private TryOptionAsync<Unit> RegisterExchange(Unit _) => bertrandExchangeManager.RegisterExchange(this);

    private TryOptionAsync<Unit> LogInformation(Option<string> message)
    {
        return message.ToTryOptionAsync().Bind<string, Unit>(message => async () =>
        {
            logger.LogInformation("EXCHANGE: {name}: {message}", ExchangeName, message);
            return await Task.FromResult(Unit.Default);
        });
    }

    private TryOptionAsync<Unit> StopConsumers(Unit _)
    {
        return async () =>
        {
            if (_consumers == null)
                return Unit.Default;

            foreach (var consumer in _consumers)
            {
                await consumer.End().Match(r => { }, () => logger.LogWarning("EXCHANGE: {_exchangeName} - Failed to stop consumer", ExchangeName), ex => logger.LogError(ex, "Failed to stop consumer"));
            }

            return await Task.FromResult(Unit.Default);
        };
    }

    private TryOptionAsync<Unit> StartConsumers(Unit _)
    {
        return async () =>
        {
            if (_consumers == null)
            {
                LogWarning("Empty consumers. Starting exchange to process outstanding states?");
                return Unit.Default;
            }

            foreach (var consumer in _consumers)
            {
                await consumer.Start(this).Match(r => r, () => throw new Exception("Failed to start exchange: " + ExchangeName), ex => throw new Exception("Failed to start exchange_ " + ExchangeName, ex));
                logger.LogInformation("EXCHANGE: {name}: started consumer: {consumer}", ExchangeName, consumer.GetType());
            }

            return await Task.FromResult(Unit.Default);
        };
    }

    public TryOptionAsync<object> Handle(Option<Message<object>> message)
    {
        return Handle(message, true);
    }

    private TryOptionAsync<object> Handle(Option<Message<object>> message, bool shouldStoreState)
    {
        var correlationId = Guid.NewGuid();
        var handleTryOption = message.ToTryOptionAsync()
            .Bind(IncrementIncoming)
            .Bind(m => EnrichMessage(m, correlationId));

        if (shouldStoreState)
            handleTryOption = handleTryOption.Bind(StoreMessage);

        return handleTryOption
            .Bind(LogMessageReceived)
            .Bind(TransformMessage)
            .Bind(PublishMessages)
            .Bind(_ => message.Bind(m => m.Payload).ToTryOptionAsync())
            .Bind(p => DeleteSuccessfulMessage(p, correlationId));
    }

    private TryOptionAsync<Unit> ProcessOutstandingWork(Unit _)
    {
        return async () =>
        {
            var outstandingWork = await bertrandStateStore.GetOutstandingMessages().Match(r => r,
                () => { logger.LogWarning("EXCHANGE: {name}: Failed to get outstanding messages: empty", ExchangeName); return Enumerable.Empty<Message<object>>(); },
                ex => { logger.LogError(ex, "EXCHANGE: {name}: Failed to get outstanding messages", ExchangeName); return Enumerable.Empty<Message<object>>(); });

            if (outstandingWork.Any())
            {
                logger.LogInformation("EXCHANGE: {name}: processing outstanding items: {items}", ExchangeName, outstandingWork.Count());

                foreach (var outstanding in outstandingWork)
                {
                    await Handle(outstanding, false).Match(r => { },
                    () => logger.LogWarning("EXCHANGE: {name}: Failed to handle outstanding message: {id}", ExchangeName, outstanding.Id.Match(i => i.ToString(), () => "Unknown")),
                    ex => logger.LogError(ex, "EXCHANGE: {name}: Failed to handle outstanding message: {id}", ExchangeName, outstanding.Id.Match(i => i.ToString(), () => "Unknown")));
                }
            }
            return await Task.FromResult(Unit.Default);
        };
    }

    private TryOptionAsync<object> DeleteSuccessfulMessage(object payload, Guid id)
    {
        return async () =>
        {
            await bertrandStateStore.Delete(id).Match(r => r, () => throw new Exception("Failed to delete successfully processed message: " + id.ToString()), ex => throw ex);
            return await Task.FromResult(payload);
        };
    }

    private TryOptionAsync<Message<object>> StoreMessage(Message<object> message)
    {
        return async () =>
        {
            bertrandStateStore.StoreIncomingMessage(message);
            return await Task.FromResult(message);
        };
    }

    private TryOptionAsync<Message<object>> IncrementIncoming(Message<object> message)
    {
        return async () =>
        {
            _metrics.IncIncoming(GetTypeName(message));
            return await Task.FromResult(message);
        };
    }

    private static TryOptionAsync<Message<object>> EnrichMessage(Message<object> message, Guid correlationId)
    {
        return async () =>
        {
            message.Id = correlationId;
            if (message.CorrelationId.IsNone)
                message.CorrelationId = correlationId;
            return await Task.FromResult(message);
        };
    }

    private TryOptionAsync<Message<object>> LogMessageReceived(Message<object> message)
    {
        return async () =>
        {
            LogInformation(message, "Received message");
            return await Task.FromResult(message);
        };
    }

    private TryOptionAsync<List<Message<object>>> TransformMessage(Message<object> message)
    {

        return async () =>
        {
            var results = new List<Message<object>>();

            if (_transformers.Count == 0)
            {
                LogInformation(message, "No transformers");
                results.Add(message);
                return results;
            }

            LogInformation(message, "Starting transformations");
            foreach (var transformer in _transformers)
            {
                if (transformerFilters.Count == 0)
                {
                    await transformer
                        .Transform(message)
                            .Match(
                        r => { _metrics.IncTransformed(GetTypeName(message)); results.Add(r); },
                        () => LogError(message, "Transformation"),
                        ex => LogError(message, "Transformation", ex));

                    LogInformation(message, "Completed transformation without filters");

                    continue;
                }

                LogInformation(message, "Transformation filter on " + transformer.Name);

                foreach (var filter in transformerFilters)
                {

                    var isMatch = await filter.IsMatch(transformer.ToSome(), message)
                        .Match(
                            r => r,
                        () => false,
                    (Func<Exception, bool>)(ex => { this.LogError(message, $"Transform-filter-match-{filter.Name}", ex); return false; }));

                    isMatch &= await bertrandExchangeStore.IsTransformerActive(ExchangeName, transformer.Name).Match(r => r, () => true);

                    if (isMatch)
                    {
                        LogInformation(message, "Transormer matched filter: " + filter.Name);
                        await transformer
                                .Transform(message)
                                    .MatchAsync(
                                async r =>
                                {
                                    _metrics.IncTransformed(GetTypeName(message)); results.Add(r);
                                    return await Task.FromResult(Unit.Default);

                                },
                                async () => await HandleError(message, "Transformation"),
                                async ex => await HandleError(message, "Transformation", ex));

                        LogInformation(message, "Transformation complete: " + transformer.GetType());
                        break;
                    }
                }
            }

            if (results.Count == 0)
            {
                LogWarning(message, "Transformations returned an empty result");
            }

            LogInformation(message, "Transformations complete");
            return results;
        };
    }

    private TryOptionAsync<Unit> PublishMessages(List<Message<object>> messages)
    {
        if (_publishers.Count == 0)
        {
            logger.LogWarning("EXCHANGE: {_exchangeName} - No publishers. Messages: {messages.Count}", ExchangeName, messages.Count);
            return async () => await Task.FromResult(Unit.Default);
        }

        return async () =>
        {
            logger.LogInformation("EXCHANGE: {_exchangeName} - starting to publish messages", ExchangeName);
            foreach (var publisher in _publishers)
            {
                foreach (var message in messages)
                {
                    if (_publisherFilters.Count == 0)
                    {
                        await PublishMessage(publisher, message);
                        LogInformation(message, "Published message to: " + publisher.Name);

                        continue;
                    }

                    foreach (var filter in _publisherFilters)
                    {
                        var isMatch = await filter.IsMatch<object>(publisher.ToSome(), message)
                        .Match(
                            r => r,
                            () => false,
                            (Func<Exception, bool>)(ex => { this.LogError(message, $"Transform-filter-match-{filter.Name}", ex); return false; }));

                        isMatch &= await bertrandExchangeStore.IsPublisherActive(ExchangeName, publisher.Name).Match(r => r, () => true);

                        if (isMatch)
                        {
                            await PublishMessage(publisher, message);
                            LogInformation(message, "Published message: " + publisher.Name);
                            break;
                        }
                    }
                }
            }

            return Unit.Default;
        };
    }

    private Task<Unit> PublishMessage(IPublisher<object> publisher, Message<object> message)
    {
        return publisher.Publish(message).MatchAsync(
                                    async r =>
                                    {
                                        _metrics.IncPublished(GetTypeName(message));
                                        return await Task.FromResult(Unit.Default);
                                    },
                                    async () => await HandleError(message, $"Publish-failed-{publisher.Name}"),
                                    async ex => await HandleError(message, $"Publish-failed-{publisher.Name}", ex));
    }

    private async Task<Unit> HandleError(Message<object> message, string action, Exception ex = null)
    {
        LogError(message, action, ex);
        await bertrandStateStore.StoreInDeadletter(message).Match(r => { }, () => LogError(message, "Failed to store in deadletter"), ex => LogError(message, "Failed to store in deadletter"));
        return await Task.FromResult(Unit.Default);
    }

    private void LogError(Message<object> message, string action, Exception ex = null)
    {
        _metrics.IncErrors(GetTypeName(message));
        if (ex == null)
            logger.LogError("{action} failed. Id: {message.Id}, RoutingKey: {message.RoutingKey}", action, message.Id, message.RoutingKey);
        else
        {
            logger.LogError(ex, "{action} failed. Id: {message.Id}, RoutingKey: {message.RoutingKey}", action, message.Id, message.RoutingKey);
        }
    }

    private static string GetTypeName(Message<object> message)
    {
        return message.Payload.Match(p => p.GetType().Name, () => "");
    }

    private void LogInformation(Message<object> message, string logMessage)
    {
        logger.LogInformation("EXCHANGE: {_exchangeName} - {logMessage}, Type: {MessageType}, Id: {message.Id}, CorrelationId: {message.CorrelationId}", ExchangeName, logMessage, message.Payload.Match(p => p, () => ""), message.Id, message.CorrelationId);
    }

    private void LogWarning(Message<object> message, string logMessage)
    {
        logger.LogWarning("EXCHANGE: {_exchangeName} - {logMessage}, Type: {MessageType}, Id: {message.Id}, CorrelationId: {message.CorrelationId}", ExchangeName, logMessage, message.Payload.Match(p => p, () => ""), message.Id, message.CorrelationId);
    }

    private void LogWarning(string logMessage)
    {
        logger.LogWarning("EXCHANGE: {_exchangeName} - {message}", ExchangeName, logMessage);
    }
}
