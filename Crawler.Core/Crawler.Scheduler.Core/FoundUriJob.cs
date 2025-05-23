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
using System.Linq;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Prometheus;
using Quartz;

namespace Crawler.Scheduler.Core
{
    public class FoundUriJob : IJob
    {
        private ILogger<FoundUriJob> _logger;
        private readonly ISchedulerRepository _schedulerRepository;
        private readonly IConfigurationRepository _configurationRepository;

        private readonly Counter _counter = Prometheus.Metrics.CreateCounter(
            "job_uri_found",
            "Uris found job",
            "context"
        );

        public FoundUriJob(
            ILogger<FoundUriJob> logger,
            ISchedulerRepository schedulerRepository,
            IConfigurationRepository configurationRepository
        )
        {
            _logger = logger;
            _schedulerRepository = schedulerRepository;
            _configurationRepository = configurationRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"Running URI Found processing job");
            await Schedule()
                .Match(_ => { }, () => throw new Exception($"Failed to schedule FoundURIs"));
        }

        private TryOptionAsync<Unit> Schedule()
        {
            return async () =>
            {
                await _schedulerRepository
                    .GetUriFoundList()
                    .Match(
                        async list =>
                        {
                            if (list.Any())
                            {
                                foreach (var uri in list)
                                {
                                    if (
                                        await _configurationRepository
                                            .ShouldSkip(uri.BaseUri, uri.Uri)
                                            .Match(r => r, () => false)
                                    )
                                    {
                                        uri.IsSkipped = true;
                                        uri.IsCompleted = true;
                                        await UpdateUri(uri);

                                        _counter.WithLabels("skipped").Inc();
                                        continue;
                                    }

                                    uri.UriTypeId =
                                        (
                                            await _configurationRepository
                                                .IsCollectable(uri.Uri)
                                                .Match(r => r, () => false, ex => false)
                                        )
                                            ? UriType.Collector
                                            : uri.UriTypeId;

                                    switch (uri.UriTypeId)
                                    {
                                        case UriType.Found:
                                        case UriType.Onetime:
                                            _counter.WithLabels($"schedule_crawl").Inc();
                                            await _schedulerRepository
                                                .AddOrUpdate(
                                                    new CrawlUriDataModel { UriId = uri.Id }
                                                )
                                                .Match(
                                                    _ => { },
                                                    () => LogError(),
                                                    ex => LogError(ex)
                                                );

                                            uri.IsCompleted = true;
                                            break;
                                    }

                                    await _schedulerRepository
                                        .AddOrUpdate(uri)
                                        .Match(_ => { }, () => LogError(), ex => LogError(ex));
                                }
                            }
                        },
                        () => LogError(),
                        ex => LogError(ex)
                    );
                return Unit.Default;
            };
        }

        private async Task UpdateUri(UriDataModel uri)
        {
            await _schedulerRepository
                .AddOrUpdate(uri)
                .Match(r => r, () => throw new Exception("failed to update model"), ex => throw ex);
        }

        private void LogError(Exception ex = null)
        {
            _counter.WithLabels($"failed").Inc();
            var message = $"Job Failed - Found URI list";
            if (ex == null)
                _logger.LogError(message);
            else
                _logger.LogError(ex, message);
        }
    }
}
