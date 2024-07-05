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
using System.Threading.Tasks;
using System.Timers;
using LanguageExt;
using Microservice.DataModel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Core.Polling
{
    public class PollingConfiguration
    {
        public const string IntervalInMsKey = "IntervalInMs";
    }

    public interface IPollingConsumer<T>
    {
        TryOptionAsync<Unit> End();
        TryOptionAsync<Unit> Start(IObserver<Either<Message<T>, ConsumerException>> observer);
    }

    public class PollingConsumer<T>(
        ILogger<IConsumer<T>> logger,
        Func<TryOptionAsync<List<T>>> queryDataFunc,
        int pollingIntervalInMs,
        string routingKey = ""
            ) : IPollingConsumer<T>
    {
        private IObserver<Either<Message<T>, ConsumerException>> _observer;
        private readonly ILogger<IConsumer<T>> _logger = logger;

        private Timer _timer;

        private readonly int _IntervalInMs = pollingIntervalInMs;
        private readonly string _routingKey = routingKey;
        readonly Func<TryOptionAsync<List<T>>> _queryDataFunc = queryDataFunc;

        public TryOptionAsync<Unit> Start(IObserver<Either<Message<T>, ConsumerException>> observer)
        {
            return async () =>
            {
                _observer = observer;
                if (_IntervalInMs == 0)
                {
                    await RunQuery();
                }
                else
                {
                    _timer = new Timer(_IntervalInMs);
                    _timer.Elapsed += async (sender, args) => await RunQuery();
                    _timer.Start();
                }


                return await Task.FromResult(Unit.Default);
            };
        }

        public TryOptionAsync<Unit> End()
        {
            return async () =>
            {
                _timer?.Stop();
                _observer?.OnCompleted();
                _timer?.Dispose();

                return await Task.FromResult(Unit.Default);
            };
        }

        private async Task RunQuery()
        {
            await _queryDataFunc().Match(
            resultList =>
            {
                _logger.LogInformation($"Polling timer elapsed. Data Query Successfull. #Items: {resultList.Count}");

                if (resultList.Count == 0)
                    return;

                foreach (var result in resultList)
                {
                    var id = (result as IDataModel)?.Id ?? Guid.NewGuid();
                    var correlationId = id;
                    var routingKey = _routingKey;

                    if (result is IMessage message)
                    {
                        id = message.Id.Match(i => i, () => id);
                        correlationId = message.CorrelationId.Match(i => i, () => correlationId);
                        routingKey = message.RoutingKey.Match(r => r, () => string.Empty);
                    }

                    _observer.OnNext(new Message<T>
                    {
                        Payload = result,
                        Id = id,
                        CorrelationId = correlationId,
                        RoutingKey = string.IsNullOrEmpty(routingKey) ? _routingKey : routingKey
                    });
                }
            },
            // EMPTY
            () =>
            {
                _logger.LogWarning("Data Query Empty result.");
                _observer.OnNext(new ConsumerException(new Exception("Query returned an Empty result")));
            },
            // ERROR
            ex =>
            {
                _logger.LogError(ex, "Data Query Error.");
                _observer.OnNext(new ConsumerException(ex));
            });
        }
    }
}