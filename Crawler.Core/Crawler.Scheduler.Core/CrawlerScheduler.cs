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
        private readonly IJobFactory _jobFactory;
        private readonly Quartz.Spi.IJobFactory _quartzJobFactory;
        private readonly IRequestPublisher _requestPublisher;
        private readonly StdSchedulerFactory _factory;
        private IScheduler _scheduler;

        public CrawlerScheduler(ILogger<CrawlerScheduler> logger, IJobFactory jobFactory, Quartz.Spi.IJobFactory quartzJobFactory, IRequestPublisher requestPublisher)
        {
            _logger = logger;
            _jobFactory = jobFactory;
            _quartzJobFactory = quartzJobFactory;
            _requestPublisher = requestPublisher;
            _factory = new StdSchedulerFactory();
        }

        public TryOptionAsync<Unit> Start()
        {
            return async () =>
            {
                _logger.LogInformation("starting scheduler");   
                _scheduler = await _factory.GetScheduler();

                if(_quartzJobFactory != null)
                    _scheduler.JobFactory = _quartzJobFactory;

                await Schedule(_jobFactory.GetUnscheduledCrawlsJob());
                await Schedule(await _jobFactory.GetPeriodicUriJobs());
                await Schedule(await _jobFactory.GetUriCollectorJobs());
                await Schedule(_jobFactory.GetOnetimeUriProcessingJob());
                await Schedule(_jobFactory.GetFoundUriProcessingJob());

                await _scheduler.Start();

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

        private async Task<DateTimeOffset> Schedule(Tuple<IJobDetail, ITrigger> jobDefinition)
        {
            _logger.LogInformation($"Scheduling: {jobDefinition.Item1.Description}. Next Fire Time: {jobDefinition.Item2.GetNextFireTimeUtc().ToString()}");
            return await _scheduler.ScheduleJob(jobDefinition.Item1, jobDefinition.Item2);
        }

        private async Task Schedule(IEnumerable<Tuple<IJobDetail, ITrigger>> jobDefinitions)
        {
            if(jobDefinitions == null)
                return;
                
            foreach(var job in jobDefinitions)
            {
                await Schedule(job);
            }
        }
    }
}