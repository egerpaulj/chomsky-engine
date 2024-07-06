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
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Threading;
using Microservice.Exchange.Bertrand;
using System.Diagnostics.Contracts;

namespace Microservice.Exchange.Core.Bertrand;



public interface IBertrandExchangeManager
{
    TryOptionAsync<Unit> StartSynch();
    TryOptionAsync<Unit> StopSynch();

    TryOptionAsync<Unit> RegisterExchange(IBertrandExchange bertrandExchange);
    TryOptionAsync<Unit> UnregisterExchange(IBertrandExchange bertrandExchange);

}
public class BertrandExchangeManager(IBertrandExchangeStore bertrandExchangeStore, ILogger<BertrandExchangeManager> logger) : IBertrandExchangeManager
{
    private const string DateStrFormat = "yyyy-MM-dd-HH:mm:ss.fff";

    private readonly System.Timers.Timer timer = new(TimeSpan.FromSeconds(5));

    private readonly List<IBertrandExchange> registeredExchanges = [];
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    public TryOptionAsync<Unit> RegisterExchange(IBertrandExchange bertrandExchange)
    {
        return async () =>
        {
            var model = await bertrandExchangeStore.GetExchange(bertrandExchange.ExchangeName).Match(m => m.BertrandExchangeModel, () => new BertrandExchangeModel() { ExchangeName = bertrandExchange.ExchangeName }, ex => throw ex);

            foreach (var transformer in bertrandExchange.GetTransformers())
            {
                if (!model.Transformers.Any(f => f.Name == transformer.Name))
                {
                    model.Transformers.Add(new BertrandExchangeControlModel
                    {
                        Name = transformer.Name,
                        IsActive = true,
                        RegistrationDate = DateTime.UtcNow.ToString(DateStrFormat)

                    });
                }
            }
            foreach (var consumer in bertrandExchange.GetConsumers())
            {
                if (!model.Consumers.Any(f => f.Name == consumer.Name))
                {
                    model.Consumers.Add(new BertrandExchangeControlModel
                    {
                        Name = consumer.Name,
                        IsActive = true,
                        RegistrationDate = DateTime.UtcNow.ToString(DateStrFormat)

                    });
                }
            }
            foreach (var publisher in bertrandExchange.GetPublishers())
            {
                if (!model.Publishers.Any(f => f.Name == publisher.Name))
                {
                    model.Publishers.Add(new BertrandExchangeControlModel
                    {
                        Name = publisher.Name,
                        IsActive = true,
                        RegistrationDate = DateTime.UtcNow.ToString(DateStrFormat)

                    });
                }
            }

            foreach (var publisherFilter in bertrandExchange.GetPublisherFilters())
            {
                if (!model.PublisherFilters.Any(f => f.Name == publisherFilter.Name))
                {
                    model.PublisherFilters.Add(new BertrandExchangeControlModel
                    {
                        Name = publisherFilter.Name,
                        IsActive = true,
                        RegistrationDate = DateTime.UtcNow.ToString(DateStrFormat)

                    });
                }
            }

            foreach (var transformerFilter in bertrandExchange.GetTransformerFilters())
            {
                if (!model.TransformerFilters.Any(f => f.Name == transformerFilter.Name))
                {
                    model.TransformerFilters.Add(new BertrandExchangeControlModel
                    {
                        Name = transformerFilter.Name,
                        IsActive = true,
                        RegistrationDate = DateTime.UtcNow.ToString(DateStrFormat)

                    });
                }
            }

            await bertrandExchangeStore.SaveExchange(model).Match(r => r, () => throw new Exception("Failed to save exchange"), ex => throw ex);

            await semaphoreSlim.WaitAsync();
            registeredExchanges.Add(bertrandExchange);
            semaphoreSlim.Release();

            return Unit.Default;
        };
    }

    public TryOptionAsync<Unit> StartSynch()
    {
        return async () =>
        {
            timer.Start();
            timer.Elapsed += async (_, _) =>
            {
                if (registeredExchanges.Count == 0)
                    return;

                await semaphoreSlim.WaitAsync();

                foreach (var exchange in registeredExchanges)
                {
                    foreach (var consumer in exchange.GetConsumers())
                    {
                        var shouldStopConsumer = await bertrandExchangeStore.IsConsumerActive(exchange.ExchangeName, consumer.Name).Match(r => r, false);
                        if (shouldStopConsumer)
                            await consumer.End().Match(r => { }, () => logger.LogWarning("{ExchangeName} exchange: failed to stop consumer: {consumer}", exchange.ExchangeName, consumer.Name), ex => logger.LogError(ex, "{ExchangeName} exchange: failed to stop consumer: {consumer}", exchange.ExchangeName, consumer.Name));
                    }
                }

                semaphoreSlim.Release();

            };
            return Unit.Default;
        };
    }

    public TryOptionAsync<Unit> StopSynch()
    {
        return async () =>
        {
            timer.Stop();
            return await Task.FromResult(Unit.Default);
        };
    }

    public TryOptionAsync<Unit> UnregisterExchange(IBertrandExchange bertrandExchange)
    {
        return async () =>
        {
            await semaphoreSlim.WaitAsync();

            registeredExchanges.Remove(bertrandExchange);

            semaphoreSlim.Release();

            return Unit.Default;
        };
    }
}