using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Scheduler.Core;
using Microservice.Amqp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Crawler.Scheduler.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ICrawlScheduler _scheduler;
        private readonly IAmqpBootstrapper _amqpBootstrapper;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);

        public Worker(ILogger<Worker> logger, ICrawlScheduler scheduler, IAmqpBootstrapper amqpBootstrapper)
        {
            _logger = logger;
            _scheduler = scheduler;
            _amqpBootstrapper = amqpBootstrapper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Prometheus Metrics");
            var promServer = new MetricServer(7779);
            promServer.Start();

            _logger.LogInformation("Boostrapping Amqp environment");
            await _amqpBootstrapper.Bootstrap().Match(r => r, () => throw new Exception("Failed to bootstrap Rabbitmq"), ex => throw ex);

            _logger.LogInformation("Crawler Scheduler Worker started at: {time}", DateTime.Now);
            
            await _scheduler.Start().Match(a => a, () => throw new Exception("Failed to start scheduler"), ex => throw ex);
            
            await _semaphore.WaitAsync();
            stoppingToken.Register(() => _semaphore.Release());
            await _semaphore.WaitAsync();
            

            await _scheduler.Stop().Match(a => a, () => throw new Exception("Failed to stop scheduler"), ex => throw ex);
            _logger.LogInformation("Crawler Scheduler Worker stopped");
            promServer.Stop();
        }
    }
}
