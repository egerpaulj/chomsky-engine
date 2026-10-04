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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crawler.Configuration.Core;
using Crawler.Core.Results;
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Prometheus;
using Quartz;

namespace Crawler.Scheduler.Core
{
    public class OnetimeUriJob : IJob
    {
        private ILogger<OnetimeUriJob> _logger;
        private readonly ICrawlerConfigurationService _crawlerConfiguration;

        private readonly IRequestPublisher _requestPublisher;
        private readonly ISchedulerRepository _schedulerRepository;

        private readonly Counter _counter = Prometheus.Metrics.CreateCounter(
            "job_uri_onetime",
            "One-time uri",
            "context"
        );

        public OnetimeUriJob(
            ILogger<OnetimeUriJob> logger,
            ICrawlerConfigurationService crawlerConfiguration,
            IRequestPublisher requestPublisher,
            ISchedulerRepository schedulerRepository
        )
        {
            _logger = logger;
            _crawlerConfiguration = crawlerConfiguration;
            _requestPublisher = requestPublisher;
            _schedulerRepository = schedulerRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"Running onetime URI processing job");
            await Schedule()
                .Match(r => r, () => throw new Exception($"Failed to schedule pending Uris"));
        }

        private TryOptionAsync<Unit> Schedule()
        {
            return _schedulerRepository.GetIncompleteOnetimeUris().Bind(list => Schedule(list));
        }

        private TryOptionAsync<Unit> Schedule(List<UriDataModel> uriDataModels)
        {
            return async () =>
            {
                var queuePublishCounts = new Dictionary<string, uint>();

                foreach (var model in uriDataModels)
                {
                    if (
                        !CrawlResponseData.WhiteList.Any(w =>
                            model.Uri.ToLower().StartsWith(w)
                        )
                    )
                    {
                        _logger.LogInformation(
                            $"Skip Scheduling. Uri not in whitelist. Uri: {model.Uri}"
                        );

                        model.IsCompleted = true;
                        model.IsSkipped = true;

                        await _schedulerRepository
                            .AddOrUpdate(model)
                            .Match(
                                r => r,
                                () => throw new Exception("Failed to update model"),
                                ex => throw ex
                            );

                        continue;
                    }

                    string host = model.Uri;
                    try
                    {
                        host = new Uri(model.Uri).Host;
                    }
                    catch { }

                    uint currentQueueSize = await _requestPublisher.GetQueueSize(host)
                        .Match(s => s, () => 0u, _ => 0u);

                    queuePublishCounts.TryGetValue(host, out var publishedCount);

                    if (currentQueueSize + publishedCount >= 100)
                    {
                        _logger.LogInformation(
                            $"Skip scheduling Uri: {model.Uri}. Target queue {host} size limit (100) reached (Current: {currentQueueSize}, Published: {publishedCount})."
                        );
                        continue;
                    }

                    await _crawlerConfiguration
                        .CreateRequest(model.Uri, correlationId: Guid.NewGuid(), model.Id)
                        .Bind(request => _requestPublisher.PublishRequest(request))
                        .Bind<Unit, Unit>(_ =>
                            async () =>
                            {
                                model.IsCompleted = true;
                                await _schedulerRepository
                                    .AddOrUpdate(model)
                                    .Match(
                                        _ => { },
                                        () => LogUriError(model.Uri),
                                        ex => LogUriError(model.Uri, ex)
                                    );
                                return Unit.Default;
                            }
                        )
                        .Match(
                            u =>
                            {
                                _counter.WithLabels($"onetime_schedule").Inc();
                                queuePublishCounts[host] = publishedCount + 1;
                            },
                            () => LogUriError(model.Uri.ToString()),
                            ex => LogUriError(model.Uri, ex)
                        );
                }

                return Unit.Default;
            };
        }

        private void LogUriError(string uri, Exception ex = null)
        {
            _counter.WithLabels("onetime_error").Inc();
            var message = $"Failed to schedule Uri: {uri}";
            if (ex == null)
                _logger.LogError(message);
            else
                _logger.LogError(ex, message);
        }
    }
}
