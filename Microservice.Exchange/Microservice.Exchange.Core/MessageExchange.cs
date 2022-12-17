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
using System.Reflection;
using LanguageExt;
using Microservice.Exchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Microservice.Exchange.Filter;
using System.IO;
using Microservice.Serialization;

namespace Microservice.Exchange
{
    public class MessageExchange<T, R> : IMessageExchange<T, R>
    {
        private readonly ILogger<MessageExchange<T, R>> _logger;
        private readonly List<IConsumer<T>> _consumers;
        private readonly List<IObservable<Either<Message<T>, ConsumerException>>> _observables;
        private List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool isDisposed;
        private readonly List<IPublisher<R>> _publishers;
        private readonly ITransformer<T, R> _transformer;
        private readonly IDeadletterPublisher<T, R> _deadletterPublisher;
        private readonly IFilter<T, R> _filter;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private readonly IExchangeMetrics _exchangeMetrics;
        private readonly string _exchangeName;

        private string _workDirectoryPath => $".working/{_exchangeName}/";

        public MessageExchange(
            ILogger<MessageExchange<T, R>> logger,
            List<IConsumer<T>> consumers,
            List<IPublisher<R>> publishers,
            ITransformer<T, R> transformer,
            IDeadletterPublisher<T, R> deadletterPublisher,
            IFilter<T, R> filter,
            IJsonConverterProvider jsonConverterProvider, 
            IExchangeMetrics exchangeMetrics,
            string name)
        {
            _logger = logger;
            _consumers = consumers;
            _publishers = publishers;
            _transformer = transformer;
            _observables = _consumers.Select(consumer => consumer.GetObservable()).ToList();
            _deadletterPublisher = deadletterPublisher;
            _filter = filter;
            _jsonConverterProvider = jsonConverterProvider;
            _exchangeMetrics = exchangeMetrics;
            _exchangeName = name;

            if(!Directory.Exists(_workDirectoryPath))
                Directory.CreateDirectory(_workDirectoryPath);
        }

        public TryOptionAsync<Unit> End()
        {
            return async () =>
            {
                _logger.LogInformation($"#### Microservice Exchange: {_exchangeName}: Stopping Exchange");
                Dispose();

                return await Task.FromResult(Unit.Default);
            };
        }

        public TryOptionAsync<Unit> Start()
        {
            return async () =>
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException("MessageExchange");
                }

                _logger.LogInformation($"#### Microservice Exchange: {_exchangeName}: Starting Exchange");

                if(Directory.Exists(_workDirectoryPath) && Directory.GetFiles(_workDirectoryPath).Any())
                {
                    _logger.LogWarning($"#### Microservice Exchange: {_exchangeName}: Processing old unfinished/crashed work");
                    foreach(var file in Directory.GetFiles(_workDirectoryPath))
                    {
                        await ProcessMessage(_jsonConverterProvider.Deserialize<Message<T>>(await File.ReadAllTextAsync(file, IJsonConverterProvider.TextEncoding)));
                    }
                }

                _observables.ForEach(obs =>
                {
                    _subscriptions.Add(Subscribe(obs));
                });

                _consumers.ForEach(async consumer =>
                {
                    await consumer.Start().Match(
                        r => r, 
                        () => throw GetException(consumer.GetType().Name), 
                        e => throw GetException(consumer.GetType().Name, e));
                });

                

                return await Task.FromResult(Unit.Default);
            };
        }

        private IDisposable Subscribe(IObservable<Either<Message<T>, ConsumerException>> obs)
        {
            return obs.Subscribe(async dataInEither =>
            {
                await dataInEither.MatchAsync<Unit>(
                            async error => await ProcessError(error), 
                            async dataIn => await ProcessMessage(dataIn)
                );
            },
            ex => _logger.LogError(ex, "Observable Terminated abruptly"),
            () => _logger.LogWarning($"Observable completed"));
        }

        private async Task<Unit> ProcessMessage(Message<T> dataIn)
        {
            _logger.LogInformation($"#### Microservice Exchange: {_exchangeName}: Received Message: {dataIn.Id}. CorrelationId: {dataIn.CorrelationId}");
            _exchangeMetrics.IncInput(typeof(T).Name);

            var matchedPublishers = FilterPublishers(dataIn);
            _logger.LogInformation($"#### Microservice Exchange: {_exchangeName}: Message: {dataIn.Id}. Number of matched publishers: {matchedPublishers.Count()}");

            try
            {
                await File.WriteAllTextAsync(GetWorkingFilePath(dataIn.Id), _jsonConverterProvider.Serialize(dataIn), IJsonConverterProvider.TextEncoding);

                var dataOut = await Transform(dataIn);
                _logger.LogInformation($"#### Microservice Exchange: {_exchangeName}: Message: {dataIn.Id}. Data transformed successfully");

                await Parallel.ForEach(matchedPublishers, async publisher =>
                {

                    _logger.LogInformation($"#### Microservice Exchange: {_exchangeName}: Message: {dataIn.Id}. Publishing to: {publisher.Name}");

                    await publisher.Publish(dataOut).Match(
                        r => _exchangeMetrics.IncOutput(publisher.Name),
                        async () => await ProcessError(dataOut, new Exception($"Failed to publish. Publisher: {publisher.GetType().Name}"), publisher.Name),
                        async ex => await ProcessError(dataOut, new Exception($"Failed to publish. Publisher: {publisher.GetType().Name}. {ex.Message} - {ex.StackTrace}"), publisher.Name));
                }).AsTask();
            }
            catch (Exception ex)
            {
                await ProcessError(dataIn, ex);
            }
            finally
            {
                File.Delete(GetWorkingFilePath(dataIn.Id));
            }

            return await Task.FromResult(Unit.Default);
        }

        private string GetWorkingFilePath(Option<Guid> id)
        {
            return Path.Combine($"{_workDirectoryPath}{id.Match(i => i, () => Guid.NewGuid()).ToString()}");
        }

        private async Task<Unit> ProcessError(Exception exception)
        {
            _logger.LogError(exception, $"#### Microservice Exchange: {_exchangeName}: Error received from consumer");

            return await _deadletterPublisher.PublishError(exception.Message).Match(r => {}, () => 
            {
                _logger.LogError(exception, $"#### Microservice Exchange: {_exchangeName}: Deadletter publish failed");
                StoreFailure($"{exception.Message} - {exception.StackTrace}", Guid.NewGuid().ToString());
            });
        }

        private async Task<Unit> ProcessError(Message<T> message, Exception ex)
        {
            _logger.LogError(ex, $"#### Microservice Exchange: {_exchangeName}: Message: {message.Id}. Error Processing Message");

            return await _deadletterPublisher.PublishError(new ErrorMessage<T>(message, ex)).Match(r => {}, () => 
            {
                 _logger.LogError(ex, $"#### Microservice Exchange: {_exchangeName}: Deadletter publish failed. Id: {message.Id}");
                StoreFailure(_jsonConverterProvider.Serialize(message), message.Id.ToString());
            });
        }

        private async Task<Unit> ProcessError(Message<R> message, Exception ex, string publisherName)
        {
            _logger.LogError(ex, $"#### Microservice Exchange: {_exchangeName}: Message: {message.Id}. Error Publishing");

            _exchangeMetrics.IncError(publisherName);

            return await _deadletterPublisher.PublishError(new ErrorMessage<R>(message, ex)).Match(r => {}, () => 
            {
                 _logger.LogError(ex, $"#### Microservice Exchange: {_exchangeName}: Deadletter publish failed. Id: {message.Id}");
                StoreFailure(_jsonConverterProvider.Serialize(message), message.Id.ToString());
            });
        }

        private List<IPublisher<R>> FilterPublishers(Message<T> dataIn)
        {
            return _publishers
                        .Where(
                            p => _filter.IsMatch(dataIn, Option<IPublisher<R>>.Some(p))
                                .Match(
                                    r => r,
                                    () =>
                                    {
                                        _logger.LogWarning($"#### Microservice Exchange: {_exchangeName}: Filter error. Publishing to all configured publishers");
                                        return true;
                                    }))
                        .ToList();
        }

        private async Task<Message<R>> Transform(Option<Message<T>> input)
        {
            return await input
                            .ToTryOptionAsync()
                            .Bind(dataIn => _transformer.Transform(dataIn))
                            .Match(
                                r => r,
                                () => throw new TransformationException($"Failed to transform data: Empty result. Transformer: {_transformer.GetType().Name}"),
                                ex => throw new TransformationException($"Failed to transform data. Transformer: {_transformer.GetType().Name}. Message: {ex.Message}", ex)); ;
        }

        private ExchangeBootstrapException GetException(string failed, Exception e = null)
        {
            return new ExchangeBootstrapException($"Failed to start: {failed}", e);
        }

        private void StoreFailure(string json, string fileName)
        {
            var criticalFailureDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, ".exchangeFailures"));
            CreateIfNotExist(criticalFailureDirectory);
            File.WriteAllText(Path.Combine(criticalFailureDirectory.FullName, fileName), json, IJsonConverterProvider.TextEncoding);
        }

        private void CreateIfNotExist(DirectoryInfo info)
        {
            if (!info.Exists)
                info.Create();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                   _logger.LogInformation($"#### Microservice Exchange: {_exchangeName}: Disposing Exchange");

                    _subscriptions.ForEach(sub => sub.Dispose());

                    _consumers.ForEach(async cons => await cons.End().Match(r => { },
                        () => _logger.LogWarning($"#### Microservice Exchange: {_exchangeName}: Failed to stop consumer: {cons.GetType().Name}"),
                        ex => _logger.LogWarning(ex, $"#### Microservice Exchange: {_exchangeName}: Failed to stop consumer: {cons.GetType().Name}")));

                    _consumers.Clear();
                    _publishers.Clear();
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}