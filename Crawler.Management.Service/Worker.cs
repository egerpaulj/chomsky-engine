using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Core.Management;
using LanguageExt;
using Microservice.Amqp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Crawler.Management.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ICrawlerManager _crawlerManager;
        private readonly IAmqpBootstrapper _amqpBootstrapper;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);

        public Worker(ILogger<Worker> logger, ICrawlerManager crawlerManager, IAmqpBootstrapper amqpBootstrapper)
        {
            _logger = logger;
            _crawlerManager = crawlerManager;
            _amqpBootstrapper = amqpBootstrapper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _amqpBootstrapper.Bootstrap().Match(a => a, () => Unit.Default);
            var promServer = new MetricServer(7777);
            promServer.Start();


            _logger.LogInformation("Crawler Manager Worker started at: {time}", DateTime.Now);
            await _crawlerManager.Start().Match(a => a, () => throw new Exception("Failed to start"), ex => throw ex);
            
            await _semaphore.WaitAsync();
            stoppingToken.Register(() => _semaphore.Release());
            await _semaphore.WaitAsync();
            
            await _crawlerManager.Stop().Match(a => a, () => throw new Exception("Failed to stop"), ex => throw ex);
            _logger.LogInformation("Crawler Manager Worker stopped");
            promServer.Stop();
        }
    }
}
