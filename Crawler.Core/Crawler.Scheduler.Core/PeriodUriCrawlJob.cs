//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2022  Paul Eger

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
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Prometheus;
using Quartz;

namespace Crawler.Scheduler.Core
{
    public class PeriodUriCrawlJob : IJob
    {
        private ILogger<PeriodUriCrawlJob> _logger;
        private readonly ICrawlerConfigurationService _crawlerConfiguration;

        private readonly IRequestPublisher _requestPublisher;

        private static Counter _counter = Prometheus.Metrics.CreateCounter(
            $"job_periodic",
            "Runs periodically",
            "context"
        );

        public PeriodUriCrawlJob(
            ILogger<PeriodUriCrawlJob> logger,
            ICrawlerConfigurationService crawlerConfiguration,
            IRequestPublisher requestPublisher = null
        )
        {
            _logger = logger;
            _crawlerConfiguration = crawlerConfiguration;
            _requestPublisher = requestPublisher;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var uri = context.MergedJobDataMap.GetString(UriCollectionJob.JobDataUriKey);
            var id = context.MergedJobDataMap.GetGuid(UriCollectionJob.JobDataIdKey);

            _logger.LogInformation($"Running Periodic job: {uri}. Id: {id}");

            await Schedule(uri, id)
                .Match(
                    r => r,
                    () => throw new Exception($"Failed to schedule Periodic Uri: {uri}")
                );
        }

        private TryOptionAsync<Unit> Schedule(string uri, Guid uriId)
        {
            return async () =>
            {
                if (_requestPublisher != null)
                {
                    string host = uri;
                    try
                    {
                        host = new Uri(uri).Host;
                    }
                    catch { }

                    uint currentQueueSize = await _requestPublisher.GetQueueSize(host)
                        .Match(s => s, () => 0u, _ => 0u);

                    if (currentQueueSize >= 100)
                    {
                        _logger.LogInformation(
                            $"Skip scheduling Periodic Uri for {uri}. Target queue {host} size limit (100) reached (Current: {currentQueueSize})."
                        );
                        return Unit.Default;
                    }
                }

                await _crawlerConfiguration
                    .Add(new CrawlUriDataModel { UriId = uriId })
                    .Match(
                        r =>
                        {
                            _counter?.WithLabels($"success").Inc();
                        },
                        () => LogCollectionError(uri),
                        ex => LogCollectionError(uri, ex)
                    );

                return Unit.Default;
            };
        }

        private void LogCollectionError(string uri, Exception ex = null)
        {
            _counter?.WithLabels($"error").Inc();
            var message = $"Failed to schedule Uri: {uri}";
            if (ex == null)
                _logger.LogError(message);
            else
                _logger.LogError(ex, message);
        }
    }
}
