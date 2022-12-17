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

using Crawler.DataModel.Scheduler;
using Crawler.RequestHandling.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;

namespace Crawler.Scheduler.Core
{
    public class CrawlerScheduler : ICrawlScheduler
    {
        private readonly ILogger<CrawlerScheduler> _logger;

        private readonly ICrawlerConfigurationService _crawlerConfiguration;

        private readonly IRequestPublisher _requestPublisher;

        private readonly StdSchedulerFactory _factory;
        private IScheduler _scheduler;

        public CrawlerScheduler(ILogger<CrawlerScheduler> logger, ICrawlerConfigurationService crawlerConfiguration, IRequestPublisher requestPublisher)
        {
            _logger = logger;
            _crawlerConfiguration = crawlerConfiguration;
            _requestPublisher = requestPublisher;
            _factory = new StdSchedulerFactory();
        }

        public TryOptionAsync<Unit> Start()
        {
            return async () =>
            {
                _logger.LogInformation("starting scheduler");   
                _scheduler = await _factory.GetScheduler();

                await _scheduler.Start();

                await _scheduler.ScheduleJob(
                    JobBuilder.Create()
                        .WithIdentity("Schedule pending Uris")
                        .Build(),
                    TriggerBuilder.Create()
                        .StartNow()
                        .WithSimpleSchedule(x => x
                            .WithIntervalInMinutes(UnscheduledUriCrawlJob.IntervalInMinutes)
                            .RepeatForever())
                        .Build()
                );

                var scheduleUriCollectorJobs = await ScheduleUriCollectors();
                await Task.WhenAll(scheduleUriCollectorJobs);

                var schedulePeriodicUriJobs = await ScheduleUriCollectors();
                await Task.WhenAll(schedulePeriodicUriJobs);

                _logger.LogInformation("Scheduler Initialized");
                return Unit.Default;
            };
        }

        public TryOptionAsync<Unit> Stop()
        {
            return async () =>
            {
                await _scheduler.Shutdown();
                _logger.LogInformation("Scheduler Shutdown");
                return Unit.Default;
            };
        }

        private async Task<IEnumerable<Task<DateTimeOffset>>> SchedulePeriodicUri()
        {
            var sourceData = await _crawlerConfiguration
                            .GetPeriodicUri()
                            .Match(r => r, () => throw new Exception("Failed to get Periodic Uri data"), ex => throw ex);

            var periodicUriJobs = sourceData.Select(s =>
                new Tuple<IJobDetail, ITrigger>(
                    JobBuilder.Create<PeriodUriCrawlJob>()
                        .UsingJobData(UriCollectionJob.JobDataIdKey, s.Id)
                        .UsingJobData(UriCollectionJob.JobDataUriKey, s.Uri)
                        .Build(),
                    TriggerBuilder.Create()
                        .WithIdentity($"Uri Schedule job: {s.Uri}", "PeriodicUri")
                        .WithCronSchedule(s.CronPeriod, x => x.WithMisfireHandlingInstructionDoNothing())
                        .Build()));

            var scheduleUriCollectorJobs = periodicUriJobs.Select(jobTuple => _scheduler.ScheduleJob(jobDetail: jobTuple.Item1, trigger: jobTuple.Item2));
            return scheduleUriCollectorJobs;
        }

        private async Task<IEnumerable<Task<DateTimeOffset>>> ScheduleUriCollectors()
        {
            var sourceData = await _crawlerConfiguration
                            .GetCollectorSourceData()
                            .Match(r => r, () => throw new Exception("Failed to get Url collector data"), ex => throw ex);

            var uriCollectorJobs = sourceData.Select(s =>
                new Tuple<IJobDetail, ITrigger>(
                    JobBuilder.Create<UriCollectionJob>()
                        .UsingJobData(UriCollectionJob.JobDataIdKey, s.Id)
                        .UsingJobData(UriCollectionJob.JobDataUriKey, s.Uri)
                        .Build(),
                    TriggerBuilder.Create()
                        .WithIdentity($"Collector job: {s.Uri}", "UriCollector")
                        .WithCronSchedule(s.CronPeriod, x => x.WithMisfireHandlingInstructionDoNothing())
                        .Build()));

            var scheduleUriCollectorJobs = uriCollectorJobs.Select(jobTuple => _scheduler.ScheduleJob(jobDetail: jobTuple.Item1, trigger: jobTuple.Item2));
            return scheduleUriCollectorJobs;
        }
    }
}