using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Scheduler.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Crawler.Scheduler.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ICrawlScheduler _scheduler;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);

        public Worker(ILogger<Worker> logger, ICrawlScheduler scheduler)
        {
            _logger = logger;
            _scheduler = scheduler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var promServer = new MetricServer(7778);
            promServer.Start();

            _logger.LogInformation("Crawler Scheduler Worker started at: {time}", DateTime.Now);
            await _scheduler.Start().Match(a => a, () => throw new Exception("Failed to start scheduler"), ex => throw ex);
            
            await _semaphore.WaitAsync();
            stoppingToken.Register(() => _semaphore.Release());
            await _semaphore.WaitAsync();
            

            await _scheduler.Start().Match(a => a, () => throw new Exception("Failed to stop scheduler"), ex => throw ex);
            _logger.LogInformation("Crawler Scheduler Worker stopped");
            promServer.Stop();
        }
    }
}
