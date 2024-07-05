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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Exchange.Core.Bertrand;
using Microservice.Exchange.Core.Polling;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Core.Bertrand;

public abstract class BertrandPollingConsumerBase : IBertrandConsumer
{
    public string Name { get; }
    protected abstract IPollingConsumer<object> PollingConsumer { get; }
    private IObserver<Either<Message<object>, ConsumerException>> _observer;
    private readonly IObservable<Either<Message<object>, ConsumerException>> _observable;
    private readonly ILogger<IBertrandConsumer> _logger;
    private IDisposable _subscription;

    public BertrandPollingConsumerBase(string name, ILogger<IBertrandConsumer> logger)
    {
        _observable = Observable.Create<Either<Message<object>, ConsumerException>>(observer =>
            {
                _observer = observer;
                return Disposable.Empty;
            });

        _logger = logger;
        Name = name;
    }

    public TryOptionAsync<Unit> End()
    {
        _subscription?.Dispose();
        return PollingConsumer?.End();
    }

    public TryOptionAsync<Unit> Start(IBertrandMessageHandler messageHandler)
    {
        _subscription = _observable.Subscribe(reqEither =>
                {
                    reqEither.MatchAsync<Unit>(async ex =>
                       {
                           _logger.LogError(ex, "Polling failed");
                           await Task.CompletedTask;
                           throw ex;

                       }, async req =>
                       {
                           await messageHandler.Handle(req).Match(r => r, () => throw new Exception("Failed to process message"), ex => throw ex);
                           return Unit.Default;
                       });
                });

        return PollingConsumer?.Start(_observer);
    }

    protected static TryOptionAsync<List<object>> Convert<T>(List<T> list) => async () => await Task.FromResult(list.ToBertrandList());
}