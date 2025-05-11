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
using System.Threading;
using System.Threading.Tasks;
using Crawler.RequestHandling.Core;
using Microservice.Amqp;
using Microservice.Exchange.Bertrand;
using Microservice.Mongodb.Repo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using IMongRepositoryFactory = Microservice.Mongodb.Repo.IRepositoryFactory;

namespace Crawler.Management.Service
{
    public class Worker(
        ILogger<Worker> logger,
        IBertrandExchangeFactory bertrandExchangeFactory,
        IAmqpBootstrapper amqpBootstrapper,
        IMongRepositoryFactory mongodbFactory,
        IConfiguration configuration
    ) : BackgroundService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var promServer = new MetricServer(7777);
            promServer.Start();

            await amqpBootstrapper
                .Bootstrap()
                .Match(_ => { }, () => throw new Exception("bootstrap exception"), ex => throw ex);

            logger.LogInformation("Starting Crawl Exchange at: {time}", DateTime.Now);
            var exchange = await bertrandExchangeFactory.CreateExchange(stoppingToken);

            var stateStoreConfiguration = new DatabaseConfiguration(
                "bertrand_exchange_crawler_state",
                configuration
            );

            var bertrandStateRepository = mongodbFactory.CreateRepository<BertrandStateDataModel>(
                stateStoreConfiguration
            );

            await exchange
                .Start()
                .Match(
                    r => r,
                    () => throw new Exception("Failed to Start Exchange"),
                    ex => throw ex
                );

            await _semaphore.WaitAsync();
            stoppingToken.Register(() => _semaphore.Release());
            await _semaphore.WaitAsync();

            await exchange
                .End()
                .Match(
                    r => r,
                    () => throw new Exception("Failed to Stop Exchange"),
                    ex => throw ex
                );

            logger.LogInformation("Crawler Exchange stopped");
            promServer.Stop();
        }
    }
}
