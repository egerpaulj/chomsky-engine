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

    public class PollingConsumer<T>
    {
        private IObserver<Either<Message<T>, ConsumerException>> _observer;
        private readonly ILogger<IConsumer<T>> _logger;

        private Timer _timer;

        private int _IntervalInMs;

        Func<TryOptionAsync<List<T>>> _queryDataFunc;

        public PollingConsumer(
            ILogger<IConsumer<T>> logger,
            IConfiguration configuration,
            Func<TryOptionAsync<List<T>>> queryDataFunc
            )

        {
            _logger = logger;
            _IntervalInMs = configuration.GetValue<int>(PollingConfiguration.IntervalInMsKey);
            _queryDataFunc = queryDataFunc;
        }

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
                    var message = result as IMessage;

                    if(message != null)
                    {
                        id = message.Id.Match(i => i, () => id);
                        correlationId = message.CorrelationId.Match(i => i, () => correlationId);
                    }

                    _observer.OnNext(new Message<T>
                    {
                        Payload = result,
                        Id = id,
                        CorrelationId = correlationId
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