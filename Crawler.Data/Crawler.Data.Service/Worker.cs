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
using System.Threading;
using System.Threading.Tasks;
using Microservice.Mongodb.Repo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using IMongRepositoryFactory = Microservice.Mongodb.Repo.IRepositoryFactory;

namespace Crawler.Data.Service
{
    public class Worker(
        ILogger<Worker> logger,
        [FromKeyedServices(nameof(BertrandCleanerExchangeFactory))]
            IBertrandExchangeFactory bertrandCleanerExchangeFactory,
        [FromKeyedServices(nameof(BertrandAdhocExchangeFactory))]
            IBertrandExchangeFactory bertrandAdhocExchangeFactory,
        IConfiguration configuration
    ) : BackgroundService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var promServer = new MetricServer(7777);
            promServer.Start();

            logger.LogInformation("Starting Data Exchange at: {time}", DateTime.Now);
            var exchange = await bertrandCleanerExchangeFactory.CreateExchange(stoppingToken);

            var adhocExchange = await bertrandAdhocExchangeFactory.CreateExchange(stoppingToken);

            var stateStoreConfiguration = new DatabaseConfiguration(
                "bertrand_exchange_datapipeline_state",
                configuration
            );

            await exchange
                .Start()
                .Match(
                    r => r,
                    () => throw new Exception("Failed to Start Data Exchange"),
                    ex => throw ex
                );

            // await adhocExchange
            //     .Start()
            //     .Match(
            //         r => r,
            //         () => throw new Exception("Failed to Start Data Exchange"),
            //         ex => throw ex
            //     );

            await _semaphore.WaitAsync();
            stoppingToken.Register(() => _semaphore.Release());
            await _semaphore.WaitAsync();

            await exchange
                .End()
                .Match(
                    r => r,
                    () => throw new Exception("Failed to Stop Data Exchange"),
                    ex => throw ex
                );

            // await adhocExchange
            //     .End()
            //     .Match(
            //         r => r,
            //         () => throw new Exception("Failed to Stop Data Exchange"),
            //         ex => throw ex
            //     );

            logger.LogInformation("Data Exchange stopped");
            promServer.Stop();
        }
    }
}
