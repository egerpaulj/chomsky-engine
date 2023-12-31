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
using Crawler.DataModel;
using Crawler.DataModel.Scheduler;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Prometheus;
using Quartz;
namespace Crawler.Scheduler.Core
{
    public class UnscheduledUriCrawlJob : IJob
    {
        private ILogger<UnscheduledUriCrawlJob> _logger;
        private readonly ICrawlerConfigurationService _crawlerConfiguration;

        private readonly IRequestPublisher _requestPublisher;
        private readonly ISchedulerRepository _schedulerRepository;

        private static Counter _counter = Prometheus.Metrics.CreateCounter($"job_unscheduled", "unscheduled crawls", "context");

        public UnscheduledUriCrawlJob(ILogger<UnscheduledUriCrawlJob> logger, ICrawlerConfigurationService crawlerConfiguration, IRequestPublisher requestPublisher, ISchedulerRepository schedulerRepository)
        {
            _logger = logger;
            _crawlerConfiguration = crawlerConfiguration;
            _requestPublisher = requestPublisher;
            _schedulerRepository = schedulerRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"Running Unscheduled job in crawl_uri");
            await ScheduleUriCrawls().Match(r => r, () => throw new Exception($"Failed to schedule pending Uris"));
        }

        private TryOptionAsync<Unit> ScheduleUriCrawls()
        {
            return _schedulerRepository.GetUnscheduledCrawlUriData().Bind(list => Schedule(list));
        }

        private TryOptionAsync<Unit> Schedule(List<CrawlUriDataModel> crawlUriDataModel)
        {
            return async () =>
            {
                await Task.WhenAll(
                    crawlUriDataModel.Select(crawlUri =>
                        _schedulerRepository
                            .GetUriData(crawlUri.UriId)
                            .Bind(uriData => _crawlerConfiguration.CreateRequest(uriData.Uri, correlationId: Guid.NewGuid(), crawlUri.Id))
                            .Bind(request => 
                                {
                                    _logger.LogInformation($"Scheduling crawl. RequestId: {request.Id}, Uri: {crawlUri.UriId}");
                                    return _requestPublisher.PublishRequest(request);
                                })
                            .Bind(_ => _crawlerConfiguration.UpdateScheduledTimeUtcNow(crawlUri.Id))
                            .Match(u => {_counter.WithLabels($"published").Inc(); }, () => LogUriError(crawlUri.UriId.ToString()), ex => LogUriError(crawlUri.Id.ToString(), ex))
                    )
                    .ToArray());

                return Unit.Default;
            };

        }

        private void LogUriError(string uri, Exception ex = null)
        {
            _counter.WithLabels($"failed").Inc();
            var message = $"Failed to schedule UriId: {uri}";
            if (ex == null)
                _logger.LogError(message);
            else
                _logger.LogError(ex, message);
        }
    }
}