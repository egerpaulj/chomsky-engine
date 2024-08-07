using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core.Management;
using Crawler.Core.Requests;
using Crawler.Core.Results;
using Crawler.DataModel;
using LanguageExt;
using Microservice.Amqp;
using Microservice.Exchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Crawler.Management.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IExchangeFactory _exchangeFactory;
        private readonly IConfiguration _configuration;
        private readonly IAmqpBootstrapper _amqpBootstrapper;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);

        public Worker(ILogger<Worker> logger, IExchangeFactory exchangeFactory, IConfiguration configuration, IAmqpBootstrapper amqpBootstrapper)
        {
            _logger = logger;
            _exchangeFactory = exchangeFactory;
            _configuration = configuration;
            _amqpBootstrapper = amqpBootstrapper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var promServer = new MetricServer(7777);
            promServer.Start();

            await _amqpBootstrapper.Bootstrap().Match(_ => {}, () => throw new Exception("bootstrap exception"), ex => throw ex);

            _logger.LogInformation("Starting Crawl Exchange at: {time}", DateTime.Now);
            var exchange = await _exchangeFactory
                .CreateMessageExchange<CrawlResponse, CrawlResponse>(
                    Option<IConfiguration>.Some(_configuration),
                    "CrawlerExchange")
                .Match(r => r, () => throw new Exception("Empty result for Exchange"), ex => throw ex);


            await exchange.Start()
                .Match(
                        r => r,
                        () => throw new Exception("Failed to Start Exchange"),
                        ex => throw ex);

            _logger.LogInformation("Starting Uri Exchange at: {time}", DateTime.Now);
            var uriExchange = await _exchangeFactory
                .CreateMessageExchange<CrawlUri, CrawlUri>(
                    Option<IConfiguration>.Some(_configuration),
                    "UriExchange")
                .Match(r => r, () => throw new Exception("Empty result for URI Exchange"), ex => throw ex);


            await uriExchange.Start()
                .Match(
                        r => r,
                        () => throw new Exception("Failed to Start URI Exchange"),
                        ex => throw ex);
            
            await _semaphore.WaitAsync();
            stoppingToken.Register(() => _semaphore.Release());
            await _semaphore.WaitAsync();
            
            await exchange.End()
                .Match(
                        r => r,
                        () => throw new Exception("Failed to Stop Exchange"),
                        ex => throw ex);

            _logger.LogInformation("Crawler Exchange stopped");
            promServer.Stop();
        }
    }
}
